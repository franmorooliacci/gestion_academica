namespace GestionAcademica.Domain.Entities;

/// <summary>Mapea la tabla `personas_roles` (clave compuesta).</summary>
public sealed class PersonaRol
{
    public int IdPersona { get; set; }
    public int IdRol { get; set; }
    public bool Activo { get; set; } = true;

    public Persona Persona { get; set; } = null!;
    public Rol Rol { get; set; } = null!;
}
