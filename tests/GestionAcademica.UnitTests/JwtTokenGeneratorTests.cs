using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using GestionAcademica.Application.Common;
using GestionAcademica.Infrastructure.Security;
using GestionAcademica.Shared.Contracts.Constants;
using GestionAcademica.Shared.Contracts.DTOs;
using GestionAcademica.Shared.Contracts.Enums;
using Microsoft.Extensions.Options;
using Xunit;

namespace GestionAcademica.UnitTests;

public sealed class JwtTokenGeneratorTests
{
    private const string Secret = "unit-test-secret-with-at-least-32-characters!!";

    private static JwtTokenGenerator CrearGenerador() => new(Options.Create(new JwtSettings
    {
        Issuer = "GestionAcademica.Test",
        Audience = "GestionAcademica.Test.Clients",
        Secret = Secret,
        ExpirationMinutes = 60,
    }));

    [Fact]
    public void Incluye_roles_rol_activo_y_cliente()
    {
        var generador = CrearGenerador();

        var resultado = generador.GenerateToken(
            7, "ana", new[] { AppRoles.Alumno, AppRoles.Profesor }, AppRoles.Profesor, ClientApp.Web);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(resultado.Token);
        Assert.Equal("7", token.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal("ana", token.Claims.Single(c => c.Type == JwtRegisteredClaimNames.UniqueName).Value);
        Assert.Equal(new[] { AppRoles.Alumno, AppRoles.Profesor },
            token.Claims.Where(c => c.Type is "role" or ClaimTypes.Role)
                .Select(c => c.Value).OrderBy(v => v));
        Assert.Equal(AppRoles.Profesor, token.Claims.Single(c => c.Type == AppClaims.ActiveRole).Value);
        Assert.Equal("web", token.Claims.Single(c => c.Type == AppClaims.Client).Value);
    }

    [Fact]
    public void Incluye_las_carreras_como_json_con_enums_string()
    {
        var generador = CrearGenerador();
        var carreras = new List<CarreraActivaDto>
        {
            new(11, 2, "Licenciatura en Sistemas", EstadoInscripcionCarrera.Activa),
            new(12, 3, "Tecnicatura en Programación", EstadoInscripcionCarrera.Inactiva),
        };

        var resultado = generador.GenerateToken(
            7, "ana", new[] { AppRoles.Alumno }, AppRoles.Alumno, ClientApp.Web, carreras);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(resultado.Token);
        var claim = token.Claims.Single(c => c.Type == AppClaims.Carreras);

        using var json = JsonDocument.Parse(claim.Value);
        Assert.Equal(2, json.RootElement.GetArrayLength());
        Assert.Equal(11, json.RootElement[0].GetProperty("IdInscripcionCarrera").GetInt32());
        Assert.Equal(2, json.RootElement[0].GetProperty("IdCarrera").GetInt32());
        Assert.Equal("Licenciatura en Sistemas", json.RootElement[0].GetProperty("NombreCarrera").GetString());
        Assert.Equal("activa", json.RootElement[0].GetProperty("Estado").GetString());
        Assert.Equal("inactiva", json.RootElement[1].GetProperty("Estado").GetString());
    }

    [Fact]
    public void No_incluye_el_claim_de_carreras_si_no_hay_carreras()
    {
        var generador = CrearGenerador();

        var resultado = generador.GenerateToken(
            9, "administrativo", new[] { AppRoles.Administrativo }, AppRoles.Administrativo,
            ClientApp.Desktop, Array.Empty<CarreraActivaDto>());

        var token = new JwtSecurityTokenHandler().ReadJwtToken(resultado.Token);
        Assert.DoesNotContain(token.Claims, c => c.Type == AppClaims.Carreras);
        Assert.Equal("desktop", token.Claims.Single(c => c.Type == AppClaims.Client).Value);
    }
}
