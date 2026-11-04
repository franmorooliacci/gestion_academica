using GestionAcademica.Shared.Contracts.Enums;

namespace GestionAcademica.Domain.Entities;

/// <summary>Mapea la tabla `inscripciones_comision`.</summary>
public sealed class InscripcionComision
{
    public int IdInscripcionComision { get; set; }
    public int IdInscripcionCarrera { get; set; }
    public int IdComision { get; set; }
    public EstadoInscripcionComision Estado { get; set; } = EstadoInscripcionComision.Inscripto;
    public CondicionCursada? Condicion { get; set; }
    public DateTime FechaInscripcion { get; set; }

    public InscripcionCarrera InscripcionCarrera { get; set; } = null!;
    public Comision Comision { get; set; } = null!;
}
