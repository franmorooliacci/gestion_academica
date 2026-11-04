using GestionAcademica.Shared.Contracts.Enums;

namespace GestionAcademica.Domain.Entities;

/// <summary>Mapea la tabla `inscripciones_examen`.</summary>
public sealed class InscripcionExamen
{
    public int IdInscripcionExamen { get; set; }
    public int IdInscripcionCarrera { get; set; }
    public int IdExamen { get; set; }
    public EstadoInscripcionExamen Estado { get; set; } = EstadoInscripcionExamen.Inscripto;
    public decimal? Nota { get; set; }
    public DateTime FechaInscripcion { get; set; }

    public InscripcionCarrera InscripcionCarrera { get; set; } = null!;
    public Examen Examen { get; set; } = null!;
}
