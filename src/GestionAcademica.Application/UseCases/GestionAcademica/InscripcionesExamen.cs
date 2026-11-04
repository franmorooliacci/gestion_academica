using GestionAcademica.Application.Common;
using GestionAcademica.Application.Interfaces;
using GestionAcademica.Domain.Entities;
using GestionAcademica.Domain.Exceptions;
using GestionAcademica.Shared.Contracts.DTOs;
using GestionAcademica.Shared.Contracts.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestionAcademica.Application.UseCases.GestionAcademica;

public sealed record GetInscripcionesExamenQuery(
    int? IdExamen,
    int? IdInscripcionCarrera,
    Solicitante Solicitante) : IRequest<List<InscripcionExamenDto>>;

public sealed class GetInscripcionesExamenQueryHandler : IRequestHandler<GetInscripcionesExamenQuery, List<InscripcionExamenDto>>
{
    private readonly IApplicationDbContext _db;
    public GetInscripcionesExamenQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<InscripcionExamenDto>> Handle(GetInscripcionesExamenQuery request, CancellationToken ct)
    {
        var query = _db.InscripcionesExamen.AsNoTracking()
            .Include(i => i.Examen).ThenInclude(e => e.Materia)
            .Include(i => i.InscripcionCarrera).ThenInclude(ic => ic.Persona)
            .AsQueryable();

        if (request.Solicitante.EsAlumno)
        {
            var propias = await _db.InscripcionesCarrera.AsNoTracking()
                .Where(ic => ic.IdPersona == request.Solicitante.IdPersona)
                .Select(ic => ic.IdInscripcionCarrera)
                .ToListAsync(ct);

            if (request.IdInscripcionCarrera.HasValue && !propias.Contains(request.IdInscripcionCarrera.Value))
                throw new AccesoDenegadoException("No puede consultar inscripciones de otra persona.");

            query = query.Where(i => propias.Contains(i.IdInscripcionCarrera));
        }
        else if (request.Solicitante.EsProfesor)
        {
            var materias = await _db.AsignacionesDocentes
                .VigentesDe(request.Solicitante.IdPersona)
                .Select(a => a.IdMateria)
                .Distinct()
                .ToListAsync(ct);

            if (request.IdExamen.HasValue)
            {
                var examen = await _db.Examenes.AsNoTracking()
                    .FirstOrDefaultAsync(e => e.IdExamen == request.IdExamen.Value, ct)
                    ?? throw new NotFoundException($"examen {request.IdExamen.Value}");

                if (!materias.Contains(examen.IdMateria))
                    throw new AccesoDenegadoException("No está asignado a la materia del examen.");
            }

            query = query.Where(i => materias.Contains(i.Examen.IdMateria));
        }
        else if (!request.Solicitante.EsAdministrativo)
        {
            throw new AccesoDenegadoException("El rol activo no tiene acceso a las inscripciones a examen.");
        }

        if (request.IdExamen.HasValue)
            query = query.Where(i => i.IdExamen == request.IdExamen.Value);
        if (request.IdInscripcionCarrera.HasValue)
            query = query.Where(i => i.IdInscripcionCarrera == request.IdInscripcionCarrera.Value);

        var items = await query
            .OrderBy(i => i.InscripcionCarrera.Persona.Apellido).ThenBy(i => i.InscripcionCarrera.Persona.Nombre)
            .ToListAsync(ct);

        return items.Select(ToDto).ToList();
    }

    internal static InscripcionExamenDto ToDto(InscripcionExamen i) => new(
        i.IdInscripcionExamen,
        i.IdInscripcionCarrera,
        i.InscripcionCarrera?.Persona is null ? string.Empty : $"{i.InscripcionCarrera.Persona.Apellido}, {i.InscripcionCarrera.Persona.Nombre}",
        i.IdExamen,
        i.Estado,
        i.Nota,
        i.FechaInscripcion);
}

public sealed record CreateInscripcionExamenCommand(CrearInscripcionExamenRequest Request, Solicitante Solicitante)
    : IRequest<InscripcionExamenDto>;

public sealed class CreateInscripcionExamenCommandHandler
    : IRequestHandler<CreateInscripcionExamenCommand, InscripcionExamenDto>
{
    /// <summary>Nota mínima de final que se considera aprobatoria.</summary>
    private const decimal NotaAprobatoria = 4m;

    private readonly IApplicationDbContext _db;
    public CreateInscripcionExamenCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<InscripcionExamenDto> Handle(CreateInscripcionExamenCommand command, CancellationToken ct)
    {
        var req = command.Request;

        var inscripcionCarrera = await _db.InscripcionesCarrera.AsNoTracking()
            .Include(ic => ic.Persona)
            .FirstOrDefaultAsync(ic => ic.IdInscripcionCarrera == req.IdInscripcionCarrera, ct)
            ?? throw new NotFoundException($"inscripción a carrera {req.IdInscripcionCarrera}");

        if (inscripcionCarrera.IdPersona != command.Solicitante.IdPersona)
            throw new AccesoDenegadoException("No puede inscribir a otra persona en una mesa de examen.");

        var examen = await _db.Examenes.AsNoTracking()
            .Include(e => e.Materia)
            .Include(e => e.Periodo)
            .FirstOrDefaultAsync(e => e.IdExamen == req.IdExamen, ct)
            ?? throw new NotFoundException($"examen {req.IdExamen}");

        // La materia del examen debe pertenecer a la carrera del alumno.
        if (examen.Materia.IdCarrera != inscripcionCarrera.IdCarrera)
            throw new ReglaDeNegocioException("El examen no pertenece a la carrera del alumno.");

        // Ventana de inscripción a finales del período asociado.
        if (examen.IdPeriodo.HasValue && !examen.Periodo!.InscripcionFinalesAbierta)
            throw new VentanaInscripcionCerradaException("la inscripción a finales del período está cerrada.");

        // No duplicar inscripción a la misma mesa.
        var duplicada = await _db.InscripcionesExamen.AnyAsync(
            i => i.IdInscripcionCarrera == req.IdInscripcionCarrera && i.IdExamen == req.IdExamen, ct);
        if (duplicada)
            throw new ReglaDeNegocioException("El alumno ya está inscripto en esta mesa de examen.");

        // Para rendir final la materia debe estar regularizada (cursada aprobada) o ya aprobada.
        var habilitado = await EstaHabilitadoAsync(req.IdInscripcionCarrera, examen.IdMateria, ct);
        if (!habilitado)
            throw new ReglaDeNegocioException("El alumno no tiene la materia regularizada ni aprobada.");

        var inscripcion = new InscripcionExamen
        {
            IdInscripcionCarrera = req.IdInscripcionCarrera,
            IdExamen = req.IdExamen,
            Estado = EstadoInscripcionExamen.Inscripto,
            Nota = null,
            FechaInscripcion = DateTime.UtcNow,
        };
        _db.InscripcionesExamen.Add(inscripcion);
        await _db.SaveChangesAsync(ct);

        inscripcion.Examen = examen;
        inscripcion.InscripcionCarrera = inscripcionCarrera;
        return GetInscripcionesExamenQueryHandler.ToDto(inscripcion);
    }

    private async Task<bool> EstaHabilitadoAsync(int idInscripcionCarrera, int idMateria, CancellationToken ct)
    {
        // Ya aprobó la materia con un final anterior.
        var aprobada = await _db.InscripcionesExamen.AsNoTracking().AnyAsync(
            i => i.IdInscripcionCarrera == idInscripcionCarrera
                 && i.Examen.IdMateria == idMateria
                 && i.Nota != null && i.Nota >= NotaAprobatoria, ct);
        if (aprobada)
            return true;

        // Cursada regularizada: existe una inscripción a comisión activa en condición 'regular'.
        var regularizada = await _db.InscripcionesComision.AsNoTracking().AnyAsync(
            i => i.IdInscripcionCarrera == idInscripcionCarrera
                 && i.Estado == EstadoInscripcionComision.Inscripto
                 && i.Condicion == CondicionCursada.Regular
                 && i.Comision.IdMateria == idMateria, ct);

        return regularizada;
    }
}

public sealed record CerrarActaExamenCommand(
    int Id,
    CerrarActaExamenRequest Request,
    Solicitante Solicitante) : IRequest<List<InscripcionExamenDto>>;

public sealed class CerrarActaExamenCommandHandler : IRequestHandler<CerrarActaExamenCommand, List<InscripcionExamenDto>>
{
    private readonly IApplicationDbContext _db;
    public CerrarActaExamenCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<InscripcionExamenDto>> Handle(CerrarActaExamenCommand command, CancellationToken ct)
    {
        var examen = await _db.Examenes.AsNoTracking()
            .FirstOrDefaultAsync(e => e.IdExamen == command.Id, ct)
            ?? throw new NotFoundException($"examen {command.Id}");

        var asignado = await _db.AsignacionesDocentes
            .VigentesDe(command.Solicitante.IdPersona)
            .AnyAsync(a => a.IdMateria == examen.IdMateria, ct);

        if (!asignado)
            throw new AccesoDenegadoException("No está asignado a la materia del examen.");

        var items = command.Request.Items;
        if (items.Count == 0)
            return new List<InscripcionExamenDto>();

        // Evita procesar dos veces la misma inscripción en la misma petición.
        var ids = items.Select(x => x.IdInscripcionExamen).ToList();
        if (ids.Distinct().Count() != ids.Count)
            throw new ReglaDeNegocioException("La misma inscripción aparece más de una vez en el acta.");

        var inscripciones = await _db.InscripcionesExamen
            .Include(i => i.InscripcionCarrera).ThenInclude(ic => ic.Persona)
            .Include(i => i.Examen).ThenInclude(e => e.Materia)
            .Where(i => i.IdExamen == command.Id && ids.Contains(i.IdInscripcionExamen))
            .ToListAsync(ct);

        var porId = inscripciones.ToDictionary(i => i.IdInscripcionExamen);

        foreach (var item in items)
        {
            if (!porId.TryGetValue(item.IdInscripcionExamen, out var inscripcion))
                throw new NotFoundException($"inscripción a examen {item.IdInscripcionExamen}");

            switch (item.Estado)
            {
                case EstadoInscripcionExamen.Presente:
                    if (item.Nota is null || item.Nota < 1m || item.Nota > 10m)
                        throw new ReglaDeNegocioException(
                            "Un alumno presente debe tener una nota entre 1 y 10.");
                    break;
                case EstadoInscripcionExamen.Ausente:
                    if (item.Nota is not null)
                        throw new ReglaDeNegocioException("Un alumno ausente no puede tener nota.");
                    break;
                default:
                    throw new ReglaDeNegocioException(
                        "El cierre del acta solo admite los estados 'presente' o 'ausente'.");
            }

            inscripcion.Estado = item.Estado;
            inscripcion.Nota = item.Nota;
        }

        await _db.SaveChangesAsync(ct);

        // Devuelve el acta procesada (con las navegaciones ya cargadas), ordenada por alumno.
        return inscripciones
            .OrderBy(i => i.InscripcionCarrera.Persona.Apellido)
            .ThenBy(i => i.InscripcionCarrera.Persona.Nombre)
            .Select(GetInscripcionesExamenQueryHandler.ToDto)
            .ToList();
    }
}
