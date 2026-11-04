using GestionAcademica.Application.Interfaces;
using GestionAcademica.Domain.Entities;
using GestionAcademica.Domain.Exceptions;
using GestionAcademica.Shared.Contracts.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestionAcademica.Application.UseCases.GestionAcademica;

public sealed record GetCorrelatividadesQuery(int IdMateria) : IRequest<List<CorrelatividadDto>>;

public sealed class GetCorrelatividadesQueryHandler : IRequestHandler<GetCorrelatividadesQuery, List<CorrelatividadDto>>
{
    private readonly IApplicationDbContext _db;
    public GetCorrelatividadesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<CorrelatividadDto>> Handle(GetCorrelatividadesQuery request, CancellationToken ct)
    {
        var items = await _db.Correlatividades.IgnoreQueryFilters()
            .AsNoTracking()
            .Include(c => c.MateriaRequisito)
            .Where(c => c.IdMateria == request.IdMateria)
            .OrderBy(c => c.IdCorrelatividad)
            .ToListAsync(ct);

        return items.Select(ToDto).ToList();
    }

    internal static CorrelatividadDto ToDto(Correlatividad c) =>
        new(c.IdCorrelatividad, c.IdMateria, c.IdMateriaRequisito,
            c.MateriaRequisito?.Nombre ?? string.Empty, c.TipoRequisito);
}

public sealed record CreateCorrelatividadCommand(int IdMateria, CrearCorrelatividadRequest Request) : IRequest<CorrelatividadDto>;

public sealed class CreateCorrelatividadCommandHandler : IRequestHandler<CreateCorrelatividadCommand, CorrelatividadDto>
{
    private readonly IApplicationDbContext _db;
    public CreateCorrelatividadCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<CorrelatividadDto> Handle(CreateCorrelatividadCommand command, CancellationToken ct)
    {
        var req = command.Request;
        var materia = await _db.Materias.IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.IdMateria == command.IdMateria, ct)
            ?? throw new NotFoundException($"materia {command.IdMateria}");

        var requisito = await _db.Materias.IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.IdMateria == req.IdMateriaRequisito, ct)
            ?? throw new NotFoundException($"materia requisito {req.IdMateriaRequisito}");

        if (materia.IdMateria == requisito.IdMateria)
            throw new ReglaDeNegocioException("Una materia no puede ser correlativa de sí misma.");

        if (await _db.Correlatividades.IgnoreQueryFilters().AnyAsync(
                c => c.IdMateria == command.IdMateria && c.IdMateriaRequisito == req.IdMateriaRequisito, ct))
            throw new ReglaDeNegocioException("La correlatividad ya existe.");

        if (await _db.Correlatividades.IgnoreQueryFilters().AnyAsync(
                c => c.IdMateria == req.IdMateriaRequisito && c.IdMateriaRequisito == command.IdMateria, ct))
            throw new ReglaDeNegocioException("La correlatividad genera un ciclo directo.");

        var correlatividad = new Correlatividad
        {
            IdMateria = command.IdMateria,
            IdMateriaRequisito = req.IdMateriaRequisito,
            TipoRequisito = req.TipoRequisito,
        };
        _db.Correlatividades.Add(correlatividad);
        await _db.SaveChangesAsync(ct);

        correlatividad.MateriaRequisito = requisito;
        return GetCorrelatividadesQueryHandler.ToDto(correlatividad);
    }
}

public sealed record DeleteCorrelatividadCommand(int IdMateria, int IdCorrelatividad) : IRequest<Unit>;

public sealed class DeleteCorrelatividadCommandHandler : IRequestHandler<DeleteCorrelatividadCommand, Unit>
{
    private readonly IApplicationDbContext _db;
    public DeleteCorrelatividadCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Unit> Handle(DeleteCorrelatividadCommand command, CancellationToken ct)
    {
        var correlatividad = await _db.Correlatividades.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.IdCorrelatividad == command.IdCorrelatividad && c.IdMateria == command.IdMateria, ct)
            ?? throw new NotFoundException($"correlatividad {command.IdCorrelatividad}");

        _db.Correlatividades.Remove(correlatividad);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
