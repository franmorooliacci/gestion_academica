using GestionAcademica.Shared.Contracts.Enums;

namespace GestionAcademica.Application.Interfaces;

/// <summary>Datos de sesión recuperados de un JWT emitido por la API.</summary>
public sealed record JwtSessionInfo(int PersonaId, string? ActiveRole, ClientApp Client);

/// <summary>
/// Lee un JWT emitido por la API sin exigir que siga vigente. Se usa para renovar
/// la sesión cuando el token expiró, validando siempre firma, emisor y audiencia.
/// </summary>
public interface IJwtTokenParser
{
    JwtSessionInfo? Parse(string? token);
}
