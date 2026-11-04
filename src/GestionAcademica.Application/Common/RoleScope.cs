using GestionAcademica.Shared.Contracts.Constants;
using GestionAcademica.Shared.Contracts.DTOs;
using GestionAcademica.Shared.Contracts.Enums;

namespace GestionAcademica.Application.Common;

/// <summary>
/// Roles habilitados para cada cliente. El escritorio es exclusivo del personal
/// administrativo; la web es el canal de alumnos y profesores.
/// </summary>
public static class RoleScope
{
    private static readonly string[] DesktopRoles = [AppRoles.Administrativo];
    private static readonly string[] WebRoles = [AppRoles.Alumno, AppRoles.Profesor];

    public static IReadOnlyList<string> For(ClientApp client) => client switch
    {
        ClientApp.Desktop => DesktopRoles,
        ClientApp.Web => WebRoles,
        _ => Array.Empty<string>(),
    };

    public static bool Permite(ClientApp client, string rol)
        => For(client).Contains(rol, StringComparer.OrdinalIgnoreCase);

    public static List<RolDto> Filtrar(IEnumerable<RolDto> roles, ClientApp client)
    {
        var permitidos = For(client);
        return roles
            .Where(r => permitidos.Contains(r.Nombre, StringComparer.OrdinalIgnoreCase))
            .ToList();
    }
}
