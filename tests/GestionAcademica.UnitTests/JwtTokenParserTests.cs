using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GestionAcademica.Application.Common;
using GestionAcademica.Infrastructure.Security;
using GestionAcademica.Shared.Contracts.Constants;
using GestionAcademica.Shared.Contracts.Enums;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace GestionAcademica.UnitTests;

public sealed class JwtTokenParserTests
{
    private const string Secret = "unit-test-secret-with-at-least-32-characters!!";

    private static JwtSettings CrearSettings(int expirationMinutes = 60) => new()
    {
        Issuer = "GestionAcademica.Test",
        Audience = "GestionAcademica.Test.Clients",
        Secret = Secret,
        ExpirationMinutes = expirationMinutes,
    };

    private static JwtTokenParser CrearParser() => new(Options.Create(CrearSettings()));

    [Fact]
    public void Recupera_la_identidad_de_un_token_vigente()
    {
        var generator = new JwtTokenGenerator(Options.Create(CrearSettings()));
        var token = generator.GenerateToken(
            7, "administrativo", new[] { AppRoles.Administrativo },
            AppRoles.Administrativo, ClientApp.Desktop);

        var session = CrearParser().Parse(token.Token);

        Assert.NotNull(session);
        Assert.Equal(7, session.PersonaId);
        Assert.Equal(AppRoles.Administrativo, session.ActiveRole);
        Assert.Equal(ClientApp.Desktop, session.Client);
    }

    [Fact]
    public void Acepta_un_token_vencido_para_renovar_la_sesion()
    {
        var settings = CrearSettings();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Secret));
        var expired = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, "9"),
                new Claim(ClaimTypes.Role, AppRoles.Alumno),
                new Claim(AppClaims.ActiveRole, AppRoles.Alumno),
                new Claim(AppClaims.Client, "web"),
            ],
            notBefore: DateTime.UtcNow.AddHours(-2),
            expires: DateTime.UtcNow.AddHours(-1),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        var token = new JwtSecurityTokenHandler().WriteToken(expired);

        var session = CrearParser().Parse(token);

        Assert.NotNull(session);
        Assert.Equal(9, session.PersonaId);
        Assert.Equal(AppRoles.Alumno, session.ActiveRole);
        Assert.Equal(ClientApp.Web, session.Client);
    }

    [Fact]
    public void Rechaza_un_token_firmado_con_otra_clave()
    {
        var generator = new JwtTokenGenerator(Options.Create(new JwtSettings
        {
            Issuer = "GestionAcademica.Test",
            Audience = "GestionAcademica.Test.Clients",
            Secret = "otra-clave-distinta-con-32-caracteres-o-mas!!",
            ExpirationMinutes = 60,
        }));

        var token = generator.GenerateToken(
            7, "administrativo", new[] { AppRoles.Administrativo },
            AppRoles.Administrativo, ClientApp.Desktop);

        Assert.Null(CrearParser().Parse(token.Token));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("no-es-un-jwt")]
    public void Rechaza_tokens_ausentes_o_malformados(string? token)
    {
        Assert.Null(CrearParser().Parse(token));
    }
}
