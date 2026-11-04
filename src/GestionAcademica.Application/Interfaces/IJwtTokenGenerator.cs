using GestionAcademica.Shared.Contracts.DTOs;
using GestionAcademica.Shared.Contracts.Enums;

namespace GestionAcademica.Application.Interfaces;

public sealed record JwtTokenResult(string Token, DateTime ExpiresAt);

/// <summary>
/// Generación de tokens JWT con los roles del cliente, el rol activo actual y las carreras
/// en las que el usuario está inscripto.
/// </summary>
public interface IJwtTokenGenerator
{
    JwtTokenResult GenerateToken(
        int personaId,
        string username,
        IReadOnlyCollection<string> roles,
        string? activeRole,
        ClientApp client,
        IReadOnlyCollection<CarreraActivaDto>? carreras = null);
}
