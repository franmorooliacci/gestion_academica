using GestionAcademica.Application.Common;
using GestionAcademica.Shared.Contracts.Constants;
using GestionAcademica.Shared.Contracts.DTOs;
using GestionAcademica.Shared.Contracts.Enums;
using Xunit;

namespace GestionAcademica.UnitTests;

public sealed class RoleScopeTests
{
    [Fact]
    public void El_escritorio_solo_habilita_administrativo()
    {
        Assert.Equal(new[] { AppRoles.Administrativo }, RoleScope.For(ClientApp.Desktop));
        Assert.True(RoleScope.Permite(ClientApp.Desktop, AppRoles.Administrativo));
        Assert.False(RoleScope.Permite(ClientApp.Desktop, AppRoles.Alumno));
        Assert.False(RoleScope.Permite(ClientApp.Desktop, AppRoles.Profesor));
    }

    [Fact]
    public void La_web_solo_habilita_alumno_y_profesor()
    {
        Assert.Equal(new[] { AppRoles.Alumno, AppRoles.Profesor }, RoleScope.For(ClientApp.Web));
        Assert.True(RoleScope.Permite(ClientApp.Web, AppRoles.Alumno));
        Assert.True(RoleScope.Permite(ClientApp.Web, AppRoles.Profesor));
        Assert.False(RoleScope.Permite(ClientApp.Web, AppRoles.Administrativo));
    }

    [Fact]
    public void Filtra_los_roles_de_la_persona_segun_el_cliente()
    {
        var roles = new[]
        {
            new RolDto(1, AppRoles.Administrativo),
            new RolDto(2, AppRoles.Alumno),
            new RolDto(3, AppRoles.Profesor),
        };

        var desktop = RoleScope.Filtrar(roles, ClientApp.Desktop);
        var web = RoleScope.Filtrar(roles, ClientApp.Web);

        Assert.Equal(new[] { AppRoles.Administrativo }, desktop.Select(r => r.Nombre));
        Assert.Equal(new[] { AppRoles.Alumno, AppRoles.Profesor }, web.Select(r => r.Nombre));
    }
}
