using GestionAcademica.Application.Common;
using GestionAcademica.Domain.Exceptions;
using Xunit;

namespace GestionAcademica.UnitTests;

public sealed class RoleAssignmentPolicyTests
{
    private const int RolAdministrativo = 1;
    private const int RolAlumno = 2;
    private const int RolProfesor = 3;

    [Fact]
    public void Rechaza_asignar_el_rol_administrativo()
    {
        var ex = Assert.Throws<AccesoDenegadoException>(
            () => RoleAssignmentPolicy.ValidarAlta(new[] { RolAlumno, RolAdministrativo }, RolAdministrativo));

        Assert.Contains("rol de gestión", ex.Message);
    }

    [Fact]
    public void Permite_asignar_alumno_y_profesor()
    {
        RoleAssignmentPolicy.ValidarAlta(new[] { RolAlumno, RolProfesor }, RolAdministrativo);
    }

    [Fact]
    public void No_permite_quitar_el_rol_a_un_administrativo()
    {
        Assert.Throws<AccesoDenegadoException>(() =>
            RoleAssignmentPolicy.ValidarActualizacion(
                tieneRolAdministrativo: true,
                idsRolesSolicitados: new[] { RolAlumno },
                idsRolesActuales: new[] { RolAdministrativo },
                idRolAdministrativo: RolAdministrativo));
    }

    [Fact]
    public void Permite_editar_perfil_sin_tocar_los_roles_de_un_administrativo()
    {
        RoleAssignmentPolicy.ValidarActualizacion(
            tieneRolAdministrativo: true,
            idsRolesSolicitados: null,
            idsRolesActuales: new[] { RolAdministrativo },
            idRolAdministrativo: RolAdministrativo);
    }

    [Fact]
    public void No_permite_dar_de_baja_a_un_administrativo()
    {
        Assert.Throws<AccesoDenegadoException>(
            () => RoleAssignmentPolicy.ValidarBaja(tieneRolAdministrativo: true));
    }
}
