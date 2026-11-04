namespace GestionAcademica.Domain.Entities;

/// <summary>Mapea la tabla `roles`.</summary>
public sealed class Rol
{
    public int IdRol { get; set; }
    public string Nombre { get; set; } = null!;
    public bool Activo { get; set; } = true;

    public ICollection<PersonaRol> PersonaRoles { get; set; } = new List<PersonaRol>();
}
