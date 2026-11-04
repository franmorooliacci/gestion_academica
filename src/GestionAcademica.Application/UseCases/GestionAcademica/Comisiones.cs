using GestionAcademica.Application.Interfaces;
using GestionAcademica.Domain.Entities;
using GestionAcademica.Domain.Exceptions;
using GestionAcademica.Shared.Contracts.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestionAcademica.Application.UseCases.GestionAcademica;

public sealed record GetComisionesQuery(int? IdMateria, int? IdPeriodo) : IRequest<List<ComisionDto>>;

public sealed class GetComisionesQueryHandler : IRequestHandler<GetComisionesQuery, List<ComisionDto>>
{
    private readonly IApplicationDbContext _db;
    public GetComisionesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<ComisionDto>> Handle(GetComisionesQuery request, CancellationToken ct)
    {
        var query = _db.Comisiones.IgnoreQueryFilters()
            .AsNoTracking()
            .Include(c => c.Materia)
            .AsQueryable();

        if (request.IdMateria.HasValue)
            query = query.Where(c => c.IdMateria == request.IdMateria.Value);
        if (request.IdPeriodo.HasValue)
            query = query.Where(c => c.IdPeriodo == request.IdPeriodo.Value);

        var comisiones = await query
            .OrderBy(c => c.Codigo)
            .ToListAsync(ct);

        return comisiones.Select(ToDto).ToList();
    }

    internal static ComisionDto ToDto(Comision c) =>
        new(c.IdComision, c.IdMateria, c.Materia?.Nombre ?? string.Empty, c.IdPeriodo, c.Codigo,
            c.Turno, c.ModalidadDictado, c.CupoMaximo, c.ListaEsperaMaxima, c.Activo);
}

public sealed record GetComisionByIdQuery(int Id) : IRequest<ComisionDto>;

public sealed class GetComisionByIdQueryHandler : IRequestHandler<GetComisionByIdQuery, ComisionDto>
{
    private readonly IApplicationDbContext _db;
    public GetComisionByIdQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<ComisionDto> Handle(GetComisionByIdQuery request, CancellationToken ct)
    {
        var comision = await _db.Comisiones.IgnoreQueryFilters().AsNoTracking()
            .Include(c => c.Materia)
            .FirstOrDefaultAsync(c => c.IdComision == request.Id, ct)
            ?? throw new NotFoundException($"comisión {request.Id}");
        return GetComisionesQueryHandler.ToDto(comision);
    }
}

public sealed record CreateComisionCommand(CrearComisionRequest Request) : IRequest<ComisionDto>;

public sealed class CreateComisionCommandHandler : IRequestHandler<CreateComisionCommand, ComisionDto>
{
    private readonly IApplicationDbContext _db;
    public CreateComisionCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<ComisionDto> Handle(CreateComisionCommand command, CancellationToken ct)
    {
        var req = command.Request;
        await ValidateAsync(req.IdMateria, req.IdPeriodo, req.Codigo, null, req.CupoMaximo, req.ListaEsperaMaxima, ct);

        var comision = new Comision
        {
            IdMateria = req.IdMateria,
            IdPeriodo = req.IdPeriodo,
            Codigo = req.Codigo,
            Turno = req.Turno,
            ModalidadDictado = req.ModalidadDictado,
            CupoMaximo = req.CupoMaximo,
            ListaEsperaMaxima = req.ListaEsperaMaxima,
            Activo = true,
        };
        _db.Comisiones.Add(comision);
        await _db.SaveChangesAsync(ct);
        return GetComisionesQueryHandler.ToDto(comision);
    }

    internal async Task ValidateAsync(int idMateria, int idPeriodo, string codigo, int? idComision,
        int? cupoMaximo, int? listaEsperaMaxima, CancellationToken ct)
    {
        if (!await _db.Materias.IgnoreQueryFilters().AnyAsync(m => m.IdMateria == idMateria, ct))
            throw new NotFoundException($"materia {idMateria}");
        if (!await _db.Periodos.IgnoreQueryFilters().AnyAsync(p => p.IdPeriodo == idPeriodo, ct))
            throw new NotFoundException($"período {idPeriodo}");
        if (cupoMaximo is <= 0)
            throw new ReglaDeNegocioException("El cupo máximo debe ser mayor que cero.");
        if (listaEsperaMaxima is < 0)
            throw new ReglaDeNegocioException("La lista de espera máxima no puede ser negativa.");

        var existe = await _db.Comisiones.IgnoreQueryFilters().AnyAsync(
            c => c.IdPeriodo == idPeriodo && c.IdMateria == idMateria && c.Codigo == codigo
                 && (idComision == null || c.IdComision != idComision), ct);
        if (existe)
            throw new ReglaDeNegocioException($"Ya existe la comisión '{codigo}' para esa materia y período.");
    }
}

public sealed record UpdateComisionCommand(int Id, ActualizarComisionRequest Request) : IRequest<ComisionDto>;

public sealed class UpdateComisionCommandHandler : IRequestHandler<UpdateComisionCommand, ComisionDto>
{
    private readonly IApplicationDbContext _db;
    public UpdateComisionCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<ComisionDto> Handle(UpdateComisionCommand command, CancellationToken ct)
    {
        var req = command.Request;
        var comision = await _db.Comisiones.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.IdComision == command.Id, ct)
            ?? throw new NotFoundException($"comisión {command.Id}");

        if (!await _db.Materias.IgnoreQueryFilters().AnyAsync(m => m.IdMateria == req.IdMateria, ct))
            throw new NotFoundException($"materia {req.IdMateria}");
        if (!await _db.Periodos.IgnoreQueryFilters().AnyAsync(p => p.IdPeriodo == req.IdPeriodo, ct))
            throw new NotFoundException($"período {req.IdPeriodo}");
        if (req.CupoMaximo is <= 0)
            throw new ReglaDeNegocioException("El cupo máximo debe ser mayor que cero.");
        if (req.ListaEsperaMaxima is < 0)
            throw new ReglaDeNegocioException("La lista de espera máxima no puede ser negativa.");

        var duplicada = await _db.Comisiones.IgnoreQueryFilters().AnyAsync(
            c => c.IdPeriodo == req.IdPeriodo && c.IdMateria == req.IdMateria && c.Codigo == req.Codigo
                 && c.IdComision != command.Id, ct);
        if (duplicada)
            throw new ReglaDeNegocioException($"Ya existe la comisión '{req.Codigo}' para esa materia y período.");

        comision.IdMateria = req.IdMateria;
        comision.IdPeriodo = req.IdPeriodo;
        comision.Codigo = req.Codigo;
        comision.Turno = req.Turno;
        comision.ModalidadDictado = req.ModalidadDictado;
        comision.CupoMaximo = req.CupoMaximo;
        comision.ListaEsperaMaxima = req.ListaEsperaMaxima;
        comision.Activo = req.Activo;

        await _db.SaveChangesAsync(ct);
        return GetComisionesQueryHandler.ToDto(comision);
    }
}

public sealed record DeleteComisionCommand(int Id) : IRequest<Unit>;

public sealed class DeleteComisionCommandHandler : IRequestHandler<DeleteComisionCommand, Unit>
{
    private readonly IApplicationDbContext _db;
    public DeleteComisionCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Unit> Handle(DeleteComisionCommand command, CancellationToken ct)
    {
        var comision = await _db.Comisiones.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.IdComision == command.Id, ct)
            ?? throw new NotFoundException($"comisión {command.Id}");

        comision.Activo = false;
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
