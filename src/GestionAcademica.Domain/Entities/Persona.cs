using GestionAcademica.Shared.Contracts.Enums;

namespace GestionAcademica.Domain.Entities;

/// <summary>Mapea la tabla `personas`.</summary>
public sealed class Persona
{
    public int IdPersona { get; set; }
    public string Nombre { get; set; } = null!;
    public string Apellido { get; set; } = null!;
    public string Dni { get; set; } = null!;
    public Sexo Sexo { get; set; }
    public DateOnly FechaNacimiento { get; set; }
    public string? Telefono { get; set; }
    public string? Domicilio { get; set; }
    public string Email { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public bool Activo { get; set; } = true;
    public DateTime FechaRegistro { get; set; }

    public ICollection<PersonaRol> PersonaRoles { get; set; } = new List<PersonaRol>();
    public ICollection<InscripcionCarrera> InscripcionesCarrera { get; set; } = new List<InscripcionCarrera>();
    public ICollection<AsignacionDocente> AsignacionesDocentes { get; set; } = new List<AsignacionDocente>();
}
