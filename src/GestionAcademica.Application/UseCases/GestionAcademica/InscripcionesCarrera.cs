using GestionAcademica.Application.Common;
using GestionAcademica.Application.Interfaces;
using GestionAcademica.Domain.Entities;
using GestionAcademica.Domain.Exceptions;
using GestionAcademica.Shared.Contracts.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestionAcademica.Application.UseCases.GestionAcademica;

public sealed record GetInscripcionesCarreraQuery(
    int? IdPersona,
    int? IdCarrera,
    Solicitante Solicitante) : IRequest<List<InscripcionCarreraDto>>;

public sealed class GetInscripcionesCarreraQueryHandler : IRequestHandler<GetInscripcionesCarreraQuery, List<InscripcionCarreraDto>>
{
    private readonly IApplicationDbContext _db;
    public GetInscripcionesCarreraQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<InscripcionCarreraDto>> Handle(GetInscripcionesCarreraQuery request, CancellationToken ct)
    {
        var query = _db.InscripcionesCarrera.AsNoTracking()
            .Include(ic => ic.Persona)
            .Include(ic => ic.Carrera)
            .AsQueryable();

        if (request.Solicitante.EsAlumno)
        {
            if (request.IdPersona.HasValue && request.IdPersona.Value != request.Solicitante.IdPersona)
                throw new AccesoDenegadoException("No puede consultar inscripciones de otra persona.");

            query = query.Where(ic => ic.IdPersona == request.Solicitante.IdPersona);
        }
        else if (!request.Solicitante.EsAdministrativo)
        {
            throw new AccesoDenegadoException("El rol activo no tiene acceso a las inscripciones a carrera.");
        }

        if (request.IdPersona.HasValue)
            query = query.Where(ic => ic.IdPersona == request.IdPersona.Value);
        if (request.IdCarrera.HasValue)
            query = query.Where(ic => ic.IdCarrera == request.IdCarrera.Value);

        var items = await query
            .OrderBy(ic => ic.Persona.Apellido).ThenBy(ic => ic.Persona.Nombre)
            .ToListAsync(ct);

        return items.Select(ToDto).ToList();
    }

    internal static InscripcionCarreraDto ToDto(InscripcionCarrera ic) => new(
        ic.IdInscripcionCarrera,
        ic.IdPersona,
        ic.Persona is null ? string.Empty : $"{ic.Persona.Apellido}, {ic.Persona.Nombre}",
        ic.IdCarrera,
        ic.Carrera?.Nombre ?? string.Empty,
        ic.FechaInscripcion,
        ic.AnioUltimaReinscripcion,
        ic.FechaReinscripcion,
        ic.Estado);
}

public sealed record CreateInscripcionCarreraCommand(CrearInscripcionCarreraRequest Request) : IRequest<InscripcionCarreraDto>;

public sealed class CreateInscripcionCarreraCommandHandler : IRequestHandler<CreateInscripcionCarreraCommand, InscripcionCarreraDto>
{
    private readonly IApplicationDbContext _db;
    public CreateInscripcionCarreraCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<InscripcionCarreraDto> Handle(CreateInscripcionCarreraCommand command, CancellationToken ct)
    {
        var req = command.Request;
        var persona = await _db.Personas.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.IdPersona == req.IdPersona, ct)
            ?? throw new NotFoundException($"persona {req.IdPersona}");
        var carrera = await _db.Carreras.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.IdCarrera == req.IdCarrera, ct)
            ?? throw new NotFoundException($"carrera {req.IdCarrera}");

        if (await _db.InscripcionesCarrera.AnyAsync(
                ic => ic.IdPersona == req.IdPersona && ic.IdCarrera == req.IdCarrera, ct))
            throw new ReglaDeNegocioException("La persona ya está inscripta en esa carrera.");

        var inscripcion = new InscripcionCarrera
        {
            IdPersona = req.IdPersona,
            IdCarrera = req.IdCarrera,
            FechaInscripcion = DateTime.UtcNow,
            Estado = Shared.Contracts.Enums.EstadoInscripcionCarrera.Activa,
        };
        _db.InscripcionesCarrera.Add(inscripcion);
        await _db.SaveChangesAsync(ct);

        inscripcion.Persona = persona;
        inscripcion.Carrera = carrera;
        return GetInscripcionesCarreraQueryHandler.ToDto(inscripcion);
    }
}

public sealed record ReinscribirCommand(int Id, ReinscribirRequest Request, Solicitante Solicitante)
    : IRequest<InscripcionCarreraDto>;

public sealed class ReinscribirCommandHandler : IRequestHandler<ReinscribirCommand, InscripcionCarreraDto>
{
    private readonly IApplicationDbContext _db;
    public ReinscribirCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<InscripcionCarreraDto> Handle(ReinscribirCommand command, CancellationToken ct)
    {
        var inscripcion = await _db.InscripcionesCarrera
            .Include(ic => ic.Persona)
            .Include(ic => ic.Carrera)
            .FirstOrDefaultAsync(ic => ic.IdInscripcionCarrera == command.Id, ct)
            ?? throw new NotFoundException($"inscripción {command.Id}");

        if (inscripcion.IdPersona != command.Solicitante.IdPersona)
            throw new AccesoDenegadoException("No puede reinscribirse en el expediente de otra persona.");

        inscripcion.AnioUltimaReinscripcion = command.Request.Anio;
        inscripcion.FechaReinscripcion = DateTime.UtcNow;
        inscripcion.Estado = Shared.Contracts.Enums.EstadoInscripcionCarrera.Activa;

        await _db.SaveChangesAsync(ct);
        return GetInscripcionesCarreraQueryHandler.ToDto(inscripcion);
    }
}

public sealed record UpdateEstadoInscripcionCarreraCommand(int Id, ActualizarEstadoInscripcionCarreraRequest Request)
    : IRequest<InscripcionCarreraDto>;

public sealed class UpdateEstadoInscripcionCarreraCommandHandler
    : IRequestHandler<UpdateEstadoInscripcionCarreraCommand, InscripcionCarreraDto>
{
    private readonly IApplicationDbContext _db;
    public UpdateEstadoInscripcionCarreraCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<InscripcionCarreraDto> Handle(UpdateEstadoInscripcionCarreraCommand command, CancellationToken ct)
    {
        var inscripcion = await _db.InscripcionesCarrera
            .Include(ic => ic.Persona)
            .Include(ic => ic.Carrera)
            .FirstOrDefaultAsync(ic => ic.IdInscripcionCarrera == command.Id, ct)
            ?? throw new NotFoundException($"inscripción {command.Id}");

        inscripcion.Estado = command.Request.Estado;
        await _db.SaveChangesAsync(ct);
        return GetInscripcionesCarreraQueryHandler.ToDto(inscripcion);
    }
}
