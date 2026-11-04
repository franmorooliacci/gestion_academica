using GestionAcademica.Shared.Contracts.Constants;

namespace GestionAcademica.Application.Common;

/// <summary>Identidad del usuario autenticado que ejecuta un caso de uso.</summary>
public sealed record Solicitante(int IdPersona, string RolActivo)
{
    public bool EsAdministrativo => RolActivo == AppRoles.Administrativo;
    public bool EsAlumno => RolActivo == AppRoles.Alumno;
    public bool EsProfesor => RolActivo == AppRoles.Profesor;
}
