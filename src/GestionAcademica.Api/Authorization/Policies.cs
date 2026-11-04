using GestionAcademica.Application.Common;
using Microsoft.AspNetCore.Authorization;

namespace GestionAcademica.Api.Authorization;

/// <summary>Políticas de autorización basadas en el rol activo del token.</summary>
public static class Policies
{
    public const string Administrativo = "administrativo";
    public const string Alumno = "alumno";
    public const string Profesor = "profesor";
    public const string AdministrativoOAlumno = "administrativo_o_alumno";
    public const string AdministrativoOProfesor = "administrativo_o_profesor";

    /// <summary>
    /// Exige que el rol activo sea uno de los permitidos y que además esté presente
    /// entre los roles emitidos en el token (el token se emite por cliente).
    /// </summary>
    public static void RequireActiveRole(AuthorizationPolicyBuilder policy, params string[] roles)
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context =>
        {
            var active = context.User.FindFirst(AppClaims.ActiveRole)?.Value;
            return !string.IsNullOrWhiteSpace(active)
                   && roles.Contains(active, StringComparer.OrdinalIgnoreCase)
                   && context.User.IsInRole(active);
        });
    }
}
