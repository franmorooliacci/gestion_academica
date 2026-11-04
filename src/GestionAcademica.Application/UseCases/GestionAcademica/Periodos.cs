using GestionAcademica.Application.Interfaces;
using GestionAcademica.Domain.Entities;
using GestionAcademica.Domain.Exceptions;
using GestionAcademica.Shared.Contracts.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestionAcademica.Application.UseCases.GestionAcademica;

public sealed record GetPeriodosQuery : IRequest<List<PeriodoDto>>;

public sealed class GetPeriodosQueryHandler : IRequestHandler<GetPeriodosQuery, List<PeriodoDto>>
{
    private readonly IApplicationDbContext _db;
    public GetPeriodosQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<PeriodoDto>> Handle(GetPeriodosQuery request, CancellationToken ct)
    {
        var periodos = await _db.Periodos.IgnoreQueryFilters()
            .AsNoTracking()
            .OrderByDescending(p => p.Anio).ThenBy(p => p.Tipo)
            .ToListAsync(ct);
        return periodos.Select(ToDto).ToList();
    }

    internal static PeriodoDto ToDto(Periodo p) =>
        new(p.IdPeriodo, p.Anio, p.Tipo, p.FechaInicio, p.FechaFin,
            p.InscripcionComisionesAbierta, p.InscripcionFinalesAbierta, p.Activo);
}

public sealed record GetPeriodoByIdQuery(int Id) : IRequest<PeriodoDto>;

public sealed class GetPeriodoByIdQueryHandler : IRequestHandler<GetPeriodoByIdQuery, PeriodoDto>
{
    private readonly IApplicationDbContext _db;
    public GetPeriodoByIdQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PeriodoDto> Handle(GetPeriodoByIdQuery request, CancellationToken ct)
    {
        var periodo = await _db.Periodos.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(p => p.IdPeriodo == request.Id, ct)
            ?? throw new NotFoundException($"período {request.Id}");
        return GetPeriodosQueryHandler.ToDto(periodo);
    }
}

public sealed record CreatePeriodoCommand(CrearPeriodoRequest Request) : IRequest<PeriodoDto>;

public sealed class CreatePeriodoCommandHandler : IRequestHandler<CreatePeriodoCommand, PeriodoDto>
{
    private readonly IApplicationDbContext _db;
    public CreatePeriodoCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<PeriodoDto> Handle(CreatePeriodoCommand command, CancellationToken ct)
    {
        var req = command.Request;
        if (req.FechaFin < req.FechaInicio)
            throw new ReglaDeNegocioException("La fecha de fin no puede ser anterior a la de inicio.");

        if (await _db.Periodos.IgnoreQueryFilters().AnyAsync(p => p.Anio == req.Anio && p.Tipo == req.Tipo, ct))
            throw new ReglaDeNegocioException($"Ya existe un período {req.Tipo} para el año {req.Anio}.");

        var periodo = new Periodo
        {
            Anio = req.Anio,
            Tipo = req.Tipo,
            FechaInicio = req.FechaInicio,
            FechaFin = req.FechaFin,
            InscripcionComisionesAbierta = false,
            InscripcionFinalesAbierta = false,
            Activo = true,
        };
        _db.Periodos.Add(periodo);
        await _db.SaveChangesAsync(ct);
        return GetPeriodosQueryHandler.ToDto(periodo);
    }
}

public sealed record UpdatePeriodoCommand(int Id, ActualizarPeriodoRequest Request) : IRequest<PeriodoDto>;

public sealed class UpdatePeriodoCommandHandler : IRequestHandler<UpdatePeriodoCommand, PeriodoDto>
{
    private readonly IApplicationDbContext _db;
    public UpdatePeriodoCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<PeriodoDto> Handle(UpdatePeriodoCommand command, CancellationToken ct)
    {
        var req = command.Request;
        var periodo = await _db.Periodos.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.IdPeriodo == command.Id, ct)
            ?? throw new NotFoundException($"período {command.Id}");

        if (req.FechaFin < req.FechaInicio)
            throw new ReglaDeNegocioException("La fecha de fin no puede ser anterior a la de inicio.");

        if (await _db.Periodos.IgnoreQueryFilters().AnyAsync(
                p => p.Anio == req.Anio && p.Tipo == req.Tipo && p.IdPeriodo != command.Id, ct))
            throw new ReglaDeNegocioException($"Ya existe un período {req.Tipo} para el año {req.Anio}.");

        periodo.Anio = req.Anio;
        periodo.Tipo = req.Tipo;
        periodo.FechaInicio = req.FechaInicio;
        periodo.FechaFin = req.FechaFin;
        periodo.Activo = req.Activo;

        await _db.SaveChangesAsync(ct);
        return GetPeriodosQueryHandler.ToDto(periodo);
    }
}

public sealed record UpdateVentanasCommand(int Id, ActualizarVentanasRequest Request) : IRequest<PeriodoDto>;

public sealed class UpdateVentanasCommandHandler : IRequestHandler<UpdateVentanasCommand, PeriodoDto>
{
    private readonly IApplicationDbContext _db;
    public UpdateVentanasCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<PeriodoDto> Handle(UpdateVentanasCommand command, CancellationToken ct)
    {
        var periodo = await _db.Periodos.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.IdPeriodo == command.Id, ct)
            ?? throw new NotFoundException($"período {command.Id}");

        periodo.InscripcionComisionesAbierta = command.Request.InscripcionComisionesAbierta;
        periodo.InscripcionFinalesAbierta = command.Request.InscripcionFinalesAbierta;

        await _db.SaveChangesAsync(ct);
        return GetPeriodosQueryHandler.ToDto(periodo);
    }
}

public sealed record DeletePeriodoCommand(int Id) : IRequest<Unit>;

public sealed class DeletePeriodoCommandHandler : IRequestHandler<DeletePeriodoCommand, Unit>
{
    private readonly IApplicationDbContext _db;
    public DeletePeriodoCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Unit> Handle(DeletePeriodoCommand command, CancellationToken ct)
    {
        var periodo = await _db.Periodos.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.IdPeriodo == command.Id, ct)
            ?? throw new NotFoundException($"período {command.Id}");

        periodo.Activo = false;
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
