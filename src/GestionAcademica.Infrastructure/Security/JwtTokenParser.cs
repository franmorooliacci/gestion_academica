using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GestionAcademica.Application.Common;
using GestionAcademica.Application.Interfaces;
using GestionAcademica.Shared.Contracts.Enums;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GestionAcademica.Infrastructure.Security;

/// <summary>
/// Valida firma, emisor y audiencia de un JWT sin exigir vigencia. Permite renovar
/// la sesión a partir de un token vencido, siempre que haya sido emitido por la API.
/// </summary>
public sealed class JwtTokenParser : IJwtTokenParser
{
    private readonly JwtSettings _settings;

    public JwtTokenParser(IOptions<JwtSettings> settings) => _settings = settings.Value;

    public JwtSessionInfo? Parse(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var handler = new JwtSecurityTokenHandler();

        try
        {
            var principal = handler.ValidateToken(
                token,
                new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = false,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _settings.Issuer,
                    ValidAudience = _settings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(_settings.Secret)),
                    RoleClaimType = ClaimTypes.Role,
                    ClockSkew = TimeSpan.FromMinutes(1),
                },
                out _);

            var rawId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (!int.TryParse(rawId, out var personaId))
                return null;

            var rawClient = principal.FindFirst(AppClaims.Client)?.Value;
            if (!Enum.TryParse<ClientApp>(rawClient, ignoreCase: true, out var client))
                return null;

            return new JwtSessionInfo(
                personaId,
                principal.FindFirst(AppClaims.ActiveRole)?.Value,
                client);
        }
        catch (Exception ex) when (ex is SecurityTokenException or ArgumentException or FormatException)
        {
            return null;
        }
    }
}
