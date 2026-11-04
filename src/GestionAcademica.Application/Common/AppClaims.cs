namespace GestionAcademica.Application.Common;

/// <summary>Nombres de claims personalizados usados en el JWT.</summary>
public static class AppClaims
{
    public const string ActiveRole = "active_role";

    /// <summary>Cliente para el que fue emitido el token (`desktop` o `web`).</summary>
    public const string Client = "client";

    /// <summary>JSON con las carreras activas del alumno (id de inscripción, id de carrera, nombre y estado).</summary>
    public const string Carreras = "carreras";
}
