using GestionAcademica.Application.Interfaces;
using GestionAcademica.Domain.Entities;
using GestionAcademica.Domain.Exceptions;
using GestionAcademica.Shared.Contracts.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestionAcademica.Application.UseCases.GestionAcademica;

public sealed record GetMateriasQuery(int? IdCarrera) : IRequest<List<MateriaDto>>;

public sealed class GetMateriasQueryHandler : IRequestHandler<GetMateriasQuery, List<MateriaDto>>
{
    private readonly IApplicationDbContext _db;
    public GetMateriasQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<MateriaDto>> Handle(GetMateriasQuery request, CancellationToken ct)
    {
        var query = _db.Materias.IgnoreQueryFilters().AsNoTracking();
        if (request.IdCarrera.HasValue)
            query = query.Where(m => m.IdCarrera == request.IdCarrera.Value);

        var materias = await query
            .OrderBy(m => m.AnioEnCarrera).ThenBy(m => m.Codigo)
            .ToListAsync(ct);
        return materias.Select(ToDto).ToList();
    }

    internal static MateriaDto ToDto(Materia m) =>
        new(m.IdMateria, m.IdCarrera, m.Codigo, m.Nombre, m.AnioEnCarrera, m.RegimenCursada, m.CargaHorariaTotal, m.Activa);
}

public sealed record GetMateriaByIdQuery(int Id) : IRequest<MateriaDto>;

public sealed class GetMateriaByIdQueryHandler : IRequestHandler<GetMateriaByIdQuery, MateriaDto>
{
    private readonly IApplicationDbContext _db;
    public GetMateriaByIdQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<MateriaDto> Handle(GetMateriaByIdQuery request, CancellationToken ct)
    {
        var materia = await _db.Materias.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(m => m.IdMateria == request.Id, ct)
            ?? throw new NotFoundException($"materia {request.Id}");
        return GetMateriasQueryHandler.ToDto(materia);
    }
}

public sealed record CreateMateriaCommand(CrearMateriaRequest Request) : IRequest<MateriaDto>;

public sealed class CreateMateriaCommandHandler : IRequestHandler<CreateMateriaCommand, MateriaDto>
{
    private readonly IApplicationDbContext _db;
    public CreateMateriaCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<MateriaDto> Handle(CreateMateriaCommand command, CancellationToken ct)
    {
        var req = command.Request;
        if (!await _db.Carreras.IgnoreQueryFilters().AnyAsync(c => c.IdCarrera == req.IdCarrera, ct))
            throw new NotFoundException($"carrera {req.IdCarrera}");

        if (await _db.Materias.IgnoreQueryFilters().AnyAsync(m => m.IdCarrera == req.IdCarrera && m.Codigo == req.Codigo, ct))
            throw new ReglaDeNegocioException($"Ya existe la materia con código '{req.Codigo}' en esa carrera.");

        var materia = new Materia
        {
            IdCarrera = req.IdCarrera,
            Codigo = req.Codigo,
            Nombre = req.Nombre,
            AnioEnCarrera = req.AnioEnCarrera,
            RegimenCursada = req.RegimenCursada,
            CargaHorariaTotal = req.CargaHorariaTotal,
            Activa = true,
        };
        _db.Materias.Add(materia);
        await _db.SaveChangesAsync(ct);
        return GetMateriasQueryHandler.ToDto(materia);
    }
}

public sealed record UpdateMateriaCommand(int Id, ActualizarMateriaRequest Request) : IRequest<MateriaDto>;

public sealed class UpdateMateriaCommandHandler : IRequestHandler<UpdateMateriaCommand, MateriaDto>
{
    private readonly IApplicationDbContext _db;
    public UpdateMateriaCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<MateriaDto> Handle(UpdateMateriaCommand command, CancellationToken ct)
    {
        var req = command.Request;
        var materia = await _db.Materias.IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.IdMateria == command.Id, ct)
            ?? throw new NotFoundException($"materia {command.Id}");

        if (await _db.Materias.IgnoreQueryFilters().AnyAsync(
                m => m.IdCarrera == req.IdCarrera && m.Codigo == req.Codigo && m.IdMateria != command.Id, ct))
            throw new ReglaDeNegocioException($"Ya existe la materia con código '{req.Codigo}' en esa carrera.");

        materia.IdCarrera = req.IdCarrera;
        materia.Codigo = req.Codigo;
        materia.Nombre = req.Nombre;
        materia.AnioEnCarrera = req.AnioEnCarrera;
        materia.RegimenCursada = req.RegimenCursada;
        materia.CargaHorariaTotal = req.CargaHorariaTotal;
        materia.Activa = req.Activa;

        await _db.SaveChangesAsync(ct);
        return GetMateriasQueryHandler.ToDto(materia);
    }
}

public sealed record DeleteMateriaCommand(int Id) : IRequest<Unit>;

public sealed class DeleteMateriaCommandHandler : IRequestHandler<DeleteMateriaCommand, Unit>
{
    private readonly IApplicationDbContext _db;
    public DeleteMateriaCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Unit> Handle(DeleteMateriaCommand command, CancellationToken ct)
    {
        var materia = await _db.Materias.IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.IdMateria == command.Id, ct)
            ?? throw new NotFoundException($"materia {command.Id}");

        materia.Activa = false;
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
