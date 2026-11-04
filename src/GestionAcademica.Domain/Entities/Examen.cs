namespace GestionAcademica.Domain.Entities;

/// <summary>Mapea la tabla `examenes`.</summary>
public sealed class Examen
{
    public int IdExamen { get; set; }
    public int IdMateria { get; set; }
    public int? IdPeriodo { get; set; }
    public DateTime Fecha { get; set; }
    public string? Aula { get; set; }

    public Materia Materia { get; set; } = null!;
    public Periodo? Periodo { get; set; }
    public ICollection<InscripcionExamen> InscripcionesExamen { get; set; } = new List<InscripcionExamen>();
}
