using GestionAcademica.Application.Common;
using GestionAcademica.Application.Interfaces;
using GestionAcademica.Domain.Exceptions;
using GestionAcademica.Shared.Contracts.DTOs;
using GestionAcademica.Shared.Contracts.Enums;
using Microsoft.EntityFrameworkCore;

namespace GestionAcademica.Application.Services;

public interface IUserProfileService
{
    Task<UserDto> BuildAsync(
        int personaId,
        string? activeRole,
        ClientApp? client = null,
        CancellationToken ct = default);
}

/// <summary>Construye el perfil completo del usuario (roles + carreras) usado por login/me/switch-role.</summary>
public sealed class UserProfileService : IUserProfileService
{
    private readonly IApplicationDbContext _db;

    public UserProfileService(IApplicationDbContext db) => _db = db;

    public async Task<UserDto> BuildAsync(
        int personaId,
        string? activeRole,
        ClientApp? client = null,
        CancellationToken ct = default)
    {
        var persona = await _db.Personas
            .AsNoTracking()
            .Include(p => p.PersonaRoles.Where(pr => pr.Activo))
                .ThenInclude(pr => pr.Rol)
            .Include(p => p.InscripcionesCarrera)
                .ThenInclude(ic => ic.Carrera)
            .FirstOrDefaultAsync(p => p.IdPersona == personaId && p.Activo, ct)
            ?? throw new NotFoundException($"persona {personaId}");

        var roles = persona.PersonaRoles
            .Where(pr => pr.Activo && pr.Rol is not null && pr.Rol.Activo)
            .Select(pr => pr.Rol)
            .DistinctBy(r => r.IdRol)
            .OrderBy(r => r.Nombre)
            .Select(r => new RolDto(r.IdRol, r.Nombre))
            .ToList();

        if (client.HasValue)
            roles = RoleScope.Filtrar(roles, client.Value);

        var carreras = persona.InscripcionesCarrera
            .Where(ic => ic.Carrera is not null)
            .Select(ic => new CarreraActivaDto(
                ic.IdInscripcionCarrera,
                ic.IdCarrera,
                ic.Carrera.Nombre,
                ic.Estado))
            .ToList();

        var resolved = activeRole;
        if (string.IsNullOrWhiteSpace(resolved)
            || roles.All(r => !string.Equals(r.Nombre, resolved, StringComparison.OrdinalIgnoreCase)))
        {
            resolved = roles.FirstOrDefault()?.Nombre;
        }

        return new UserDto(
            persona.IdPersona,
            persona.Nombre,
            persona.Apellido,
            persona.Email,
            persona.Username,
            resolved,
            roles,
            carreras);
    }
}
