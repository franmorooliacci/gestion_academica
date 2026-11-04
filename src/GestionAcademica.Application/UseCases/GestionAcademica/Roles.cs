using GestionAcademica.Application.Interfaces;
using GestionAcademica.Shared.Contracts.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestionAcademica.Application.UseCases.GestionAcademica;

public sealed record GetRolesQuery : IRequest<List<RolDto>>;

public sealed class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, List<RolDto>>
{
    private readonly IApplicationDbContext _db;
    public GetRolesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<RolDto>> Handle(GetRolesQuery request, CancellationToken ct)
    {
        var roles = await _db.Roles.AsNoTracking()
            .OrderBy(r => r.Nombre)
            .ToListAsync(ct);
        return roles.Select(r => new RolDto(r.IdRol, r.Nombre)).ToList();
    }
}
