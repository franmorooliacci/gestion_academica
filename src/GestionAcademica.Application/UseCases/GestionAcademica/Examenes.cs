using GestionAcademica.Application.Interfaces;
using GestionAcademica.Domain.Entities;
using GestionAcademica.Domain.Exceptions;
using GestionAcademica.Shared.Contracts.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestionAcademica.Application.UseCases.GestionAcademica;

// ---------- Queries ----------

public sealed record GetExamenesQuery(int? IdMateria, int? IdPeriodo) : IRequest<List<ExamenDto>>;

public sealed class GetExamenesQueryHandler : IRequestHandler<GetExamenesQuery, List<ExamenDto>>
{
    private readonly IApplicationDbContext _db;
    public GetExamenesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<ExamenDto>> Handle(GetExamenesQuery request, CancellationToken ct)
    {
        var query = _db.Examenes.AsNoTracking()
            .Include(e => e.Materia)
            .Include(e => e.Periodo)
            .AsQueryable();

        if (request.IdMateria.HasValue)
            query = query.Where(e => e.IdMateria == request.IdMateria.Value);
        if (request.IdPeriodo.HasValue)
            query = query.Where(e => e.IdPeriodo == request.IdPeriodo.Value);

        var examenes = await query
            .OrderByDescending(e => e.Fecha)
            .ToListAsync(ct);

        return examenes.Select(ToDto).ToList();
    }

    internal static ExamenDto ToDto(Examen e) => new(
        e.IdExamen,
        e.IdMateria,
        e.Materia?.Nombre ?? string.Empty,
        e.IdPeriodo,
        e.Periodo?.Anio,
        e.Periodo?.Tipo,
        e.Periodo?.InscripcionFinalesAbierta ?? false,
        e.Fecha,
        e.Aula);
}

public sealed record GetExamenByIdQuery(int Id) : IRequest<ExamenDto>;

public sealed class GetExamenByIdQueryHandler : IRequestHandler<GetExamenByIdQuery, ExamenDto>
{
    private readonly IApplicationDbContext _db;
    public GetExamenByIdQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<ExamenDto> Handle(GetExamenByIdQuery request, CancellationToken ct)
    {
        var examen = await _db.Examenes.AsNoTracking()
            .Include(e => e.Materia)
            .Include(e => e.Periodo)
            .FirstOrDefaultAsync(e => e.IdExamen == request.Id, ct)
            ?? throw new NotFoundException($"examen {request.Id}");
        return GetExamenesQueryHandler.ToDto(examen);
    }
}

// ---------- Commands ----------

public sealed record CreateExamenCommand(CrearExamenRequest Request) : IRequest<ExamenDto>;

public sealed class CreateExamenCommandHandler : IRequestHandler<CreateExamenCommand, ExamenDto>
{
    private readonly IApplicationDbContext _db;
    public CreateExamenCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<ExamenDto> Handle(CreateExamenCommand command, CancellationToken ct)
    {
        var req = command.Request;
        var materia = await _db.Materias.IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.IdMateria == req.IdMateria, ct)
            ?? throw new NotFoundException($"materia {req.IdMateria}");

        Periodo? periodo = null;
        if (req.IdPeriodo.HasValue)
        {
            periodo = await _db.Periodos.IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.IdPeriodo == req.IdPeriodo.Value, ct)
                ?? throw new NotFoundException($"período {req.IdPeriodo.Value}");

            if (req.Fecha.Date < periodo.FechaInicio.ToDateTime(TimeOnly.MinValue)
                || req.Fecha.Date > periodo.FechaFin.ToDateTime(TimeOnly.MinValue))
                throw new ReglaDeNegocioException("La fecha del examen no está dentro del período indicado.");
        }

        var examen = new Examen
        {
            IdMateria = req.IdMateria,
            IdPeriodo = req.IdPeriodo,
            Fecha = req.Fecha,
            Aula = req.Aula,
        };
        _db.Examenes.Add(examen);
        await _db.SaveChangesAsync(ct);

        examen.Materia = materia;
        examen.Periodo = periodo;
        return GetExamenesQueryHandler.ToDto(examen);
    }
}

public sealed record UpdateExamenCommand(int Id, ActualizarExamenRequest Request) : IRequest<ExamenDto>;

public sealed class UpdateExamenCommandHandler : IRequestHandler<UpdateExamenCommand, ExamenDto>
{
    private readonly IApplicationDbContext _db;
    public UpdateExamenCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<ExamenDto> Handle(UpdateExamenCommand command, CancellationToken ct)
    {
        var req = command.Request;
        var examen = await _db.Examenes
            .FirstOrDefaultAsync(e => e.IdExamen == command.Id, ct)
            ?? throw new NotFoundException($"examen {command.Id}");

        var materia = await _db.Materias.IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.IdMateria == req.IdMateria, ct)
            ?? throw new NotFoundException($"materia {req.IdMateria}");

        Periodo? periodo = null;
        if (req.IdPeriodo.HasValue)
        {
            periodo = await _db.Periodos.IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.IdPeriodo == req.IdPeriodo.Value, ct)
                ?? throw new NotFoundException($"período {req.IdPeriodo.Value}");

            if (req.Fecha.Date < periodo.FechaInicio.ToDateTime(TimeOnly.MinValue)
                || req.Fecha.Date > periodo.FechaFin.ToDateTime(TimeOnly.MinValue))
                throw new ReglaDeNegocioException("La fecha del examen no está dentro del período indicado.");
        }

        examen.IdMateria = req.IdMateria;
        examen.IdPeriodo = req.IdPeriodo;
        examen.Fecha = req.Fecha;
        examen.Aula = req.Aula;

        await _db.SaveChangesAsync(ct);

        examen.Materia = materia;
        examen.Periodo = periodo;
        return GetExamenesQueryHandler.ToDto(examen);
    }
}

public sealed record DeleteExamenCommand(int Id) : IRequest<Unit>;

public sealed class DeleteExamenCommandHandler : IRequestHandler<DeleteExamenCommand, Unit>
{
    private readonly IApplicationDbContext _db;
    public DeleteExamenCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Unit> Handle(DeleteExamenCommand command, CancellationToken ct)
    {
        var examen = await _db.Examenes
            .FirstOrDefaultAsync(e => e.IdExamen == command.Id, ct)
            ?? throw new NotFoundException($"examen {command.Id}");

        _db.Examenes.Remove(examen);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
