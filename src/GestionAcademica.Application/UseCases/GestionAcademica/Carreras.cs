using GestionAcademica.Application.Interfaces;
using GestionAcademica.Domain.Entities;
using GestionAcademica.Domain.Exceptions;
using GestionAcademica.Shared.Contracts.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestionAcademica.Application.UseCases.GestionAcademica;

// ---------- Queries ----------

public sealed record GetCarrerasQuery : IRequest<List<CarreraDto>>;

public sealed class GetCarrerasQueryHandler : IRequestHandler<GetCarrerasQuery, List<CarreraDto>>
{
    private readonly IApplicationDbContext _db;
    public GetCarrerasQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<CarreraDto>> Handle(GetCarrerasQuery request, CancellationToken ct)
    {
        var carreras = await _db.Carreras.IgnoreQueryFilters()
            .AsNoTracking()
            .OrderBy(c => c.Nombre)
            .ToListAsync(ct);
        return carreras.Select(ToDto).ToList();
    }

    internal static CarreraDto ToDto(Carrera c) =>
        new(c.IdCarrera, c.Nombre, c.Tipo, c.TituloOtorgado, c.DuracionAnios, c.Activa);
}

public sealed record GetCarreraByIdQuery(int Id) : IRequest<CarreraDto>;

public sealed class GetCarreraByIdQueryHandler : IRequestHandler<GetCarreraByIdQuery, CarreraDto>
{
    private readonly IApplicationDbContext _db;
    public GetCarreraByIdQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<CarreraDto> Handle(GetCarreraByIdQuery request, CancellationToken ct)
    {
        var carrera = await _db.Carreras.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(c => c.IdCarrera == request.Id, ct)
            ?? throw new NotFoundException($"carrera {request.Id}");
        return GetCarrerasQueryHandler.ToDto(carrera);
    }
}

// ---------- Commands ----------

public sealed record CreateCarreraCommand(CrearCarreraRequest Request) : IRequest<CarreraDto>;

public sealed class CreateCarreraCommandHandler : IRequestHandler<CreateCarreraCommand, CarreraDto>
{
    private readonly IApplicationDbContext _db;
    public CreateCarreraCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<CarreraDto> Handle(CreateCarreraCommand command, CancellationToken ct)
    {
        var req = command.Request;
        if (await _db.Carreras.IgnoreQueryFilters().AnyAsync(c => c.Nombre == req.Nombre, ct))
            throw new ReglaDeNegocioException($"Ya existe una carrera con nombre '{req.Nombre}'.");

        var carrera = new Carrera
        {
            Nombre = req.Nombre,
            Tipo = req.Tipo,
            TituloOtorgado = req.TituloOtorgado,
            DuracionAnios = req.DuracionAnios,
            Activa = true,
        };
        _db.Carreras.Add(carrera);
        await _db.SaveChangesAsync(ct);
        return GetCarrerasQueryHandler.ToDto(carrera);
    }
}

public sealed record UpdateCarreraCommand(int Id, ActualizarCarreraRequest Request) : IRequest<CarreraDto>;

public sealed class UpdateCarreraCommandHandler : IRequestHandler<UpdateCarreraCommand, CarreraDto>
{
    private readonly IApplicationDbContext _db;
    public UpdateCarreraCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<CarreraDto> Handle(UpdateCarreraCommand command, CancellationToken ct)
    {
        var req = command.Request;
        var carrera = await _db.Carreras.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.IdCarrera == command.Id, ct)
            ?? throw new NotFoundException($"carrera {command.Id}");

        if (await _db.Carreras.IgnoreQueryFilters().AnyAsync(c => c.Nombre == req.Nombre && c.IdCarrera != command.Id, ct))
            throw new ReglaDeNegocioException($"Ya existe una carrera con nombre '{req.Nombre}'.");

        carrera.Nombre = req.Nombre;
        carrera.Tipo = req.Tipo;
        carrera.TituloOtorgado = req.TituloOtorgado;
        carrera.DuracionAnios = req.DuracionAnios;
        carrera.Activa = req.Activa;

        await _db.SaveChangesAsync(ct);
        return GetCarrerasQueryHandler.ToDto(carrera);
    }
}

public sealed record DeleteCarreraCommand(int Id) : IRequest<Unit>;

public sealed class DeleteCarreraCommandHandler : IRequestHandler<DeleteCarreraCommand, Unit>
{
    private readonly IApplicationDbContext _db;
    public DeleteCarreraCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Unit> Handle(DeleteCarreraCommand command, CancellationToken ct)
    {
        var carrera = await _db.Carreras.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.IdCarrera == command.Id, ct)
            ?? throw new NotFoundException($"carrera {command.Id}");

        carrera.Activa = false;
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
