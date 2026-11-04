using GestionAcademica.Application.Common;
using GestionAcademica.Application.Interfaces;
using GestionAcademica.Domain.Entities;
using GestionAcademica.Domain.Exceptions;
using GestionAcademica.Domain.Services;
using GestionAcademica.Shared.Contracts.DTOs;
using GestionAcademica.Shared.Contracts.Enums;
using GestionAcademica.Shared.Contracts.Serialization;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestionAcademica.Application.UseCases.GestionAcademica;

public sealed record GetInscripcionesComisionQuery(
    int? IdComision,
    int? IdInscripcionCarrera,
    Solicitante Solicitante) : IRequest<List<InscripcionComisionDto>>;

public sealed class GetInscripcionesComisionQueryHandler : IRequestHandler<GetInscripcionesComisionQuery, List<InscripcionComisionDto>>
{
    private readonly IApplicationDbContext _db;
    public GetInscripcionesComisionQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<InscripcionComisionDto>> Handle(GetInscripcionesComisionQuery request, CancellationToken ct)
    {
        var query = _db.InscripcionesComision.AsNoTracking()
            .Include(i => i.Comision).ThenInclude(c => c.Materia)
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
            var asignaciones = await _db.AsignacionesDocentes
                .VigentesDe(request.Solicitante.IdPersona)
                .Select(a => new { a.IdMateria, a.IdComision })
                .ToListAsync(ct);

            var materias = asignaciones.Where(a => a.IdComision == null)
                .Select(a => a.IdMateria).Distinct().ToList();
            var comisiones = asignaciones.Where(a => a.IdComision != null)
                .Select(a => a.IdComision!.Value).Distinct().ToList();

            if (request.IdComision.HasValue)
            {
                var comision = await _db.Comisiones.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.IdComision == request.IdComision.Value, ct)
                    ?? throw new NotFoundException($"comisión {request.IdComision.Value}");

                if (!materias.Contains(comision.IdMateria) && !comisiones.Contains(comision.IdComision))
                    throw new AccesoDenegadoException("No está asignado a la comisión solicitada.");
            }

            query = query.Where(i => materias.Contains(i.Comision.IdMateria) || comisiones.Contains(i.IdComision));
        }
        else if (!request.Solicitante.EsAdministrativo)
        {
            throw new AccesoDenegadoException("El rol activo no tiene acceso a las inscripciones a comisión.");
        }

        if (request.IdComision.HasValue)
            query = query.Where(i => i.IdComision == request.IdComision.Value);
        if (request.IdInscripcionCarrera.HasValue)
            query = query.Where(i => i.IdInscripcionCarrera == request.IdInscripcionCarrera.Value);

        var items = await query
            .OrderBy(i => i.FechaInscripcion)
            .ToListAsync(ct);

        return items.Select(ToDto).ToList();
    }

    internal static InscripcionComisionDto ToDto(InscripcionComision i) => new(
        i.IdInscripcionComision,
        i.IdInscripcionCarrera,
        i.IdComision,
        i.Comision?.Codigo ?? string.Empty,
        i.Comision?.Materia?.Nombre ?? string.Empty,
        i.InscripcionCarrera?.Persona is null ? string.Empty : $"{i.InscripcionCarrera.Persona.Apellido}, {i.InscripcionCarrera.Persona.Nombre}",
        i.Estado,
        i.Condicion,
        i.FechaInscripcion);
}

public sealed record CreateInscripcionComisionCommand(CrearInscripcionComisionRequest Request, Solicitante Solicitante)
    : IRequest<InscripcionComisionDto>;

public sealed class CreateInscripcionComisionCommandHandler : IRequestHandler<CreateInscripcionComisionCommand, InscripcionComisionDto>
{
    /// <summary>Nota mínima (final) que se considera aprobatoria para satisfacer requisitos 'aprobada'.</summary>
    private const decimal NotaAprobatoria = 4m;

    private readonly IApplicationDbContext _db;
    private readonly IInscripcionComisionService _service;

    public CreateInscripcionComisionCommandHandler(IApplicationDbContext db, IInscripcionComisionService service)
    {
        _db = db;
        _service = service;
    }

    public async Task<InscripcionComisionDto> Handle(CreateInscripcionComisionCommand command, CancellationToken ct)
    {
        var req = command.Request;

        var inscripcionCarrera = await _db.InscripcionesCarrera.AsNoTracking()
            .Include(ic => ic.Persona)
            .Include(ic => ic.Carrera)
            .FirstOrDefaultAsync(ic => ic.IdInscripcionCarrera == req.IdInscripcionCarrera, ct)
            ?? throw new NotFoundException($"inscripción a carrera {req.IdInscripcionCarrera}");

        if (inscripcionCarrera.IdPersona != command.Solicitante.IdPersona)
            throw new AccesoDenegadoException("No puede inscribir a otra persona en una comisión.");

        var comision = await _db.Comisiones.AsNoTracking()
            .Include(c => c.Materia)
            .Include(c => c.Periodo)
            .FirstOrDefaultAsync(c => c.IdComision == req.IdComision, ct)
            ?? throw new NotFoundException($"comisión {req.IdComision}");

        // La materia de la comisión debe pertenecer a la carrera del alumno.
        if (comision.Materia.IdCarrera != inscripcionCarrera.IdCarrera)
            throw new ReglaDeNegocioException("La comisión no pertenece a la carrera del alumno.");

        // Ventana de inscripción a comisiones.
        if (!comision.Periodo.InscripcionComisionesAbierta)
            throw new VentanaInscripcionCerradaException("la inscripción a comisiones del período está cerrada.");

        // Correlatividades de la materia (según tipo_requisito).
        await ValidarCorrelatividadesAsync(comision.IdMateria, inscripcionCarrera, ct);

        // Alta transaccional con bloqueo de cupo (SELECT ... FOR UPDATE).
        var inscripcion = await _service.InscribirAsync(req.IdInscripcionCarrera, req.IdComision, ct);

        return new InscripcionComisionDto(
            inscripcion.IdInscripcionComision,
            req.IdInscripcionCarrera,
            req.IdComision,
            comision.Codigo,
            comision.Materia.Nombre,
            $"{inscripcionCarrera.Persona.Apellido}, {inscripcionCarrera.Persona.Nombre}",
            inscripcion.Estado,
            inscripcion.Condicion,
            inscripcion.FechaInscripcion);
    }

    private async Task ValidarCorrelatividadesAsync(int idMateria, InscripcionCarrera inscripcionCarrera, CancellationToken ct)
    {
        var requisitos = await _db.Correlatividades.AsNoTracking()
            .Include(c => c.MateriaRequisito)
            .Where(c => c.IdMateria == idMateria)
            .ToListAsync(ct);

        if (requisitos.Count == 0)
            return;

        // Materias regularizadas por el alumno. Se resuelven las comisiones ignorando el
        // soft-delete para no perder regularizaciones de comisiones ya desactivadas.
        var comisionesRegularizadas = await _db.InscripcionesComision.AsNoTracking()
            .Where(i => i.IdInscripcionCarrera == inscripcionCarrera.IdInscripcionCarrera
                        && i.Condicion == CondicionCursada.Regular)
            .Select(i => i.IdComision)
            .Distinct()
            .ToListAsync(ct);

        var regularizadas = await _db.Comisiones.IgnoreQueryFilters().AsNoTracking()
            .Where(c => comisionesRegularizadas.Contains(c.IdComision))
            .Select(c => c.IdMateria)
            .ToHashSetAsync(ct);

        // Materias aprobadas (nota de final aprobatoria) por el alumno.
        var aprobadas = await _db.InscripcionesExamen.AsNoTracking()
            .Where(ie => ie.IdInscripcionCarrera == inscripcionCarrera.IdInscripcionCarrera
                         && ie.Nota != null && ie.Nota >= NotaAprobatoria)
            .Select(ie => ie.Examen.IdMateria)
            .ToHashSetAsync(ct);

        var incumplidas = CorrelatividadService.RequisitosIncumplidos(requisitos, regularizadas, aprobadas);
        if (incumplidas.Count > 0)
        {
            var primera = incumplidas[0];
            var nombre = primera.MateriaRequisito?.Nombre ?? primera.IdMateriaRequisito.ToString();
            throw new CorrelativaIncumplidaException(nombre, primera.TipoRequisito.ToValue());
        }
    }
}

public sealed record BajaInscripcionComisionCommand(int Id, Solicitante Solicitante) : IRequest<InscripcionComisionDto>;

public sealed class BajaInscripcionComisionCommandHandler : IRequestHandler<BajaInscripcionComisionCommand, InscripcionComisionDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IInscripcionComisionService _service;

    public BajaInscripcionComisionCommandHandler(IApplicationDbContext db, IInscripcionComisionService service)
    {
        _db = db;
        _service = service;
    }

    public async Task<InscripcionComisionDto> Handle(BajaInscripcionComisionCommand command, CancellationToken ct)
    {
        var actual = await _db.InscripcionesComision.AsNoTracking()
            .Include(i => i.InscripcionCarrera)
            .FirstOrDefaultAsync(i => i.IdInscripcionComision == command.Id, ct)
            ?? throw new NotFoundException($"inscripción a comisión {command.Id}");

        if (actual.InscripcionCarrera.IdPersona != command.Solicitante.IdPersona)
            throw new AccesoDenegadoException("No puede darse de baja de una inscripción ajena.");

        await _service.DarDeBajaAsync(command.Id, ct);

        var inscripcion = await _db.InscripcionesComision.AsNoTracking()
            .Include(i => i.Comision).ThenInclude(c => c.Materia)
            .Include(i => i.InscripcionCarrera).ThenInclude(ic => ic.Persona)
            .FirstOrDefaultAsync(i => i.IdInscripcionComision == command.Id, ct)
            ?? throw new NotFoundException($"inscripción a comisión {command.Id}");

        return GetInscripcionesComisionQueryHandler.ToDto(inscripcion);
    }
}

public sealed record UpdateCondicionInscripcionComisionCommand(
    int Id,
    ActualizarCondicionRequest Request,
    Solicitante Solicitante) : IRequest<InscripcionComisionDto>;

public sealed class UpdateCondicionInscripcionComisionCommandHandler
    : IRequestHandler<UpdateCondicionInscripcionComisionCommand, InscripcionComisionDto>
{
    private readonly IApplicationDbContext _db;
    public UpdateCondicionInscripcionComisionCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<InscripcionComisionDto> Handle(UpdateCondicionInscripcionComisionCommand command, CancellationToken ct)
    {
        var inscripcion = await _db.InscripcionesComision
            .Include(i => i.Comision).ThenInclude(c => c.Materia)
            .Include(i => i.InscripcionCarrera).ThenInclude(ic => ic.Persona)
            .FirstOrDefaultAsync(i => i.IdInscripcionComision == command.Id, ct)
            ?? throw new NotFoundException($"inscripción a comisión {command.Id}");

        if (inscripcion.Comision is null)
            throw new NotFoundException($"comisión de la inscripción {command.Id}");

        if (inscripcion.Estado != EstadoInscripcionComision.Inscripto)
            throw new ReglaDeNegocioException("Solo se puede cargar la condición de alumnos inscriptos.");

        var asignado = await _db.AsignacionesDocentes
            .VigentesDe(command.Solicitante.IdPersona)
            .AnyAsync(a => a.IdMateria == inscripcion.Comision.IdMateria
                           && (a.IdComision == null || a.IdComision == inscripcion.IdComision), ct);

        if (!asignado)
            throw new AccesoDenegadoException("No está asignado a la comisión.");

        inscripcion.Condicion = command.Request.Condicion;
        await _db.SaveChangesAsync(ct);

        return GetInscripcionesComisionQueryHandler.ToDto(inscripcion);
    }
}
