using GestionAcademica.Domain.Exceptions;

namespace GestionAcademica.Application.Common;

/// <summary>
/// El rol administrativo es el único rol de gestión del sistema y no se asigna
/// ni se retira desde la API: las cuentas administrativas se administran por seed/BD.
/// </summary>
public static class RoleAssignmentPolicy
{
    public static void ValidarAlta(IReadOnlyCollection<int>? idsRoles, int idRolAdministrativo)
    {
        if (idsRoles is not null && idsRoles.Contains(idRolAdministrativo))
            throw new AccesoDenegadoException("No se puede asignar el rol de gestión.");
    }

    public static void ValidarActualizacion(
        bool tieneRolAdministrativo,
        IReadOnlyCollection<int>? idsRolesSolicitados,
        IReadOnlyCollection<int> idsRolesActuales,
        int idRolAdministrativo)
    {
        if (tieneRolAdministrativo)
        {
            if (idsRolesSolicitados is not null && !idsRolesSolicitados.ToHashSet().SetEquals(idsRolesActuales))
                throw new AccesoDenegadoException("El rol de gestión no se puede modificar.");
            return;
        }

        ValidarAlta(idsRolesSolicitados, idRolAdministrativo);
    }

    public static void ValidarBaja(bool tieneRolAdministrativo)
    {
        if (tieneRolAdministrativo)
            throw new AccesoDenegadoException("No se puede dar de baja a un usuario administrativo.");
    }
}
