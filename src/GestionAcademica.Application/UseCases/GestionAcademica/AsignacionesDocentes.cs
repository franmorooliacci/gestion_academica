using GestionAcademica.Application.Common;
using GestionAcademica.Application.Interfaces;
using GestionAcademica.Domain.Entities;
using GestionAcademica.Domain.Exceptions;
using GestionAcademica.Shared.Contracts.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestionAcademica.Application.UseCases.GestionAcademica;

public sealed record GetAsignacionesDocentesQuery(
    int? IdMateria,
    int? IdPersona,
    Solicitante Solicitante) : IRequest<List<AsignacionDocenteDto>>;

public sealed class GetAsignacionesDocentesQueryHandler : IRequestHandler<GetAsignacionesDocentesQuery, List<AsignacionDocenteDto>>
{
    private readonly IApplicationDbContext _db;
    public GetAsignacionesDocentesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<AsignacionDocenteDto>> Handle(GetAsignacionesDocentesQuery request, CancellationToken ct)
    {
        var query = _db.AsignacionesDocentes.IgnoreQueryFilters()
            .AsNoTracking()
            .Include(a => a.Materia)
            .Include(a => a.Persona)
            .Include(a => a.Comision)
            .AsQueryable();

        if (request.Solicitante.EsProfesor)
        {
            if (request.IdPersona.HasValue && request.IdPersona.Value != request.Solicitante.IdPersona)
                throw new AccesoDenegadoException("No puede consultar cátedras de otro docente.");

            var hoy = DateOnly.FromDateTime(DateTime.Today);
            query = query.Where(a =>
                a.IdPersona == request.Solicitante.IdPersona
                && a.Activo
                && a.FechaDesde <= hoy
                && (a.FechaHasta == null || a.FechaHasta >= hoy));
        }
        else if (!request.Solicitante.EsAdministrativo)
        {
            throw new AccesoDenegadoException("El rol activo no tiene acceso a las asignaciones docentes.");
        }

        if (request.IdMateria.HasValue)
            query = query.Where(a => a.IdMateria == request.IdMateria.Value);
        if (request.IdPersona.HasValue)
            query = query.Where(a => a.IdPersona == request.IdPersona.Value);

        var items = await query
            .OrderBy(a => a.Persona.Apellido).ThenBy(a => a.Persona.Nombre)
            .ToListAsync(ct);

        return items.Select(ToDto).ToList();
    }

    internal static AsignacionDocenteDto ToDto(AsignacionDocente a) =>
        new(a.IdAsignacion, a.IdMateria, a.Materia?.Nombre ?? string.Empty,
            a.IdPersona, a.Persona is null ? string.Empty : $"{a.Persona.Apellido}, {a.Persona.Nombre}",
            a.IdComision, a.Comision?.Codigo, a.Cargo, a.FechaDesde, a.FechaHasta, a.Activo);
}

public sealed record CreateAsignacionDocenteCommand(CrearAsignacionDocenteRequest Request) : IRequest<AsignacionDocenteDto>;

public sealed class CreateAsignacionDocenteCommandHandler : IRequestHandler<CreateAsignacionDocenteCommand, AsignacionDocenteDto>
{
    private readonly IApplicationDbContext _db;
    public CreateAsignacionDocenteCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<AsignacionDocenteDto> Handle(CreateAsignacionDocenteCommand command, CancellationToken ct)
    {
        var req = command.Request;
        await ValidateAsync(req.IdMateria, req.IdPersona, req.IdComision, null, ct);

        var asignacion = new AsignacionDocente
        {
            IdMateria = req.IdMateria,
            IdPersona = req.IdPersona,
            IdComision = req.IdComision,
            Cargo = req.Cargo,
            FechaDesde = req.FechaDesde,
            FechaHasta = req.FechaHasta,
            Activo = true,
        };
        _db.AsignacionesDocentes.Add(asignacion);
        await _db.SaveChangesAsync(ct);

        await LoadNavigationsAsync(asignacion, ct);
        return GetAsignacionesDocentesQueryHandler.ToDto(asignacion);
    }

    private async Task ValidateAsync(int idMateria, int idPersona, int? idComision, int? idAsignacion, CancellationToken ct)
    {
        if (!await _db.Materias.IgnoreQueryFilters().AnyAsync(m => m.IdMateria == idMateria, ct))
            throw new NotFoundException($"materia {idMateria}");
        if (!await _db.Personas.IgnoreQueryFilters().AnyAsync(p => p.IdPersona == idPersona, ct))
            throw new NotFoundException($"persona {idPersona}");

        if (idComision.HasValue)
        {
            var comision = await _db.Comisiones.IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.IdComision == idComision.Value, ct)
                ?? throw new NotFoundException($"comisión {idComision.Value}");
            if (comision.IdMateria != idMateria)
                throw new ReglaDeNegocioException("La comisión no pertenece a la materia indicada.");
        }

        var duplicada = await _db.AsignacionesDocentes.IgnoreQueryFilters().AnyAsync(
            a => a.IdMateria == idMateria && a.IdPersona == idPersona && a.IdComision == idComision
                 && (idAsignacion == null || a.IdAsignacion != idAsignacion), ct);
        if (duplicada)
            throw new ReglaDeNegocioException("Ya existe una asignación para ese docente y comisión.");
    }

    private async Task LoadNavigationsAsync(AsignacionDocente asignacion, CancellationToken ct)
    {
        asignacion.Materia = await _db.Materias.IgnoreQueryFilters()
            .FirstAsync(m => m.IdMateria == asignacion.IdMateria, ct);
        asignacion.Persona = await _db.Personas.IgnoreQueryFilters()
            .FirstAsync(p => p.IdPersona == asignacion.IdPersona, ct);
        if (asignacion.IdComision.HasValue)
        {
            asignacion.Comision = await _db.Comisiones.IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.IdComision == asignacion.IdComision.Value, ct);
        }
    }
}

public sealed record UpdateAsignacionDocenteCommand(int Id, ActualizarAsignacionDocenteRequest Request) : IRequest<AsignacionDocenteDto>;

public sealed class UpdateAsignacionDocenteCommandHandler : IRequestHandler<UpdateAsignacionDocenteCommand, AsignacionDocenteDto>
{
    private readonly IApplicationDbContext _db;
    public UpdateAsignacionDocenteCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<AsignacionDocenteDto> Handle(UpdateAsignacionDocenteCommand command, CancellationToken ct)
    {
        var req = command.Request;
        var asignacion = await _db.AsignacionesDocentes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.IdAsignacion == command.Id, ct)
            ?? throw new NotFoundException($"asignación {command.Id}");

        if (!await _db.Materias.IgnoreQueryFilters().AnyAsync(m => m.IdMateria == req.IdMateria, ct))
            throw new NotFoundException($"materia {req.IdMateria}");
        if (!await _db.Personas.IgnoreQueryFilters().AnyAsync(p => p.IdPersona == req.IdPersona, ct))
            throw new NotFoundException($"persona {req.IdPersona}");

        if (req.IdComision.HasValue)
        {
            var comision = await _db.Comisiones.IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.IdComision == req.IdComision.Value, ct)
                ?? throw new NotFoundException($"comisión {req.IdComision.Value}");
            if (comision.IdMateria != req.IdMateria)
                throw new ReglaDeNegocioException("La comisión no pertenece a la materia indicada.");
        }

        var duplicada = await _db.AsignacionesDocentes.IgnoreQueryFilters().AnyAsync(
            a => a.IdMateria == req.IdMateria && a.IdPersona == req.IdPersona && a.IdComision == req.IdComision
                 && a.IdAsignacion != command.Id, ct);
        if (duplicada)
            throw new ReglaDeNegocioException("Ya existe una asignación para ese docente y comisión.");

        asignacion.IdMateria = req.IdMateria;
        asignacion.IdPersona = req.IdPersona;
        asignacion.IdComision = req.IdComision;
        asignacion.Cargo = req.Cargo;
        asignacion.FechaDesde = req.FechaDesde;
        asignacion.FechaHasta = req.FechaHasta;
        asignacion.Activo = req.Activo;

        await _db.SaveChangesAsync(ct);

        asignacion.Materia = await _db.Materias.IgnoreQueryFilters().FirstAsync(m => m.IdMateria == asignacion.IdMateria, ct);
        asignacion.Persona = await _db.Personas.IgnoreQueryFilters().FirstAsync(p => p.IdPersona == asignacion.IdPersona, ct);
        asignacion.Comision = asignacion.IdComision.HasValue
            ? await _db.Comisiones.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.IdComision == asignacion.IdComision.Value, ct)
            : null;

        return GetAsignacionesDocentesQueryHandler.ToDto(asignacion);
    }
}

public sealed record DeleteAsignacionDocenteCommand(int Id) : IRequest<Unit>;

public sealed class DeleteAsignacionDocenteCommandHandler : IRequestHandler<DeleteAsignacionDocenteCommand, Unit>
{
    private readonly IApplicationDbContext _db;
    public DeleteAsignacionDocenteCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Unit> Handle(DeleteAsignacionDocenteCommand command, CancellationToken ct)
    {
        var asignacion = await _db.AsignacionesDocentes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.IdAsignacion == command.Id, ct)
            ?? throw new NotFoundException($"asignación {command.Id}");

        asignacion.Activo = false;
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
