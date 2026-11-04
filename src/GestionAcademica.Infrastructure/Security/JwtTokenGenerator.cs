using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using GestionAcademica.Application.Common;
using GestionAcademica.Application.Interfaces;
using GestionAcademica.Shared.Contracts.DTOs;
using GestionAcademica.Shared.Contracts.Enums;
using GestionAcademica.Shared.Contracts.Serialization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GestionAcademica.Infrastructure.Security;

/// <summary>
/// Genera JWT con el id de persona, la lista de roles del cliente, el rol activo,
/// el cliente y las carreras del alumno (claim `carreras` como JSON).
/// </summary>
public sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    private static readonly JsonSerializerOptions CarrerasJsonOptions = new()
    {
        Converters = { new JsonStringEnumMemberConverter() },
    };

    private readonly JwtSettings _settings;

    public JwtTokenGenerator(IOptions<JwtSettings> settings) => _settings = settings.Value;

    public JwtTokenResult GenerateToken(
        int personaId,
        string username,
        IReadOnlyCollection<string> roles,
        string? activeRole,
        ClientApp client,
        IReadOnlyCollection<CarreraActivaDto>? carreras = null)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_settings.ExpirationMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var resolvedActiveRole = activeRole ?? roles.FirstOrDefault();

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, personaId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(AppClaims.Client, client.ToValue()),
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        if (!string.IsNullOrWhiteSpace(resolvedActiveRole))
            claims.Add(new Claim(AppClaims.ActiveRole, resolvedActiveRole));

        // Carreras del alumno: el cliente puede resolver el contexto sin volver a llamar a /me.
        if (carreras is { Count: > 0 })
            claims.Add(new Claim(AppClaims.Carreras, JsonSerializer.Serialize(carreras, CarrerasJsonOptions)));

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtTokenResult(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
