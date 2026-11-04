using GestionAcademica.Shared.Contracts.Enums;

namespace GestionAcademica.Domain.Entities;

/// <summary>Mapea la tabla `inscripciones_carrera`.</summary>
public sealed class InscripcionCarrera
{
    public int IdInscripcionCarrera { get; set; }
    public int IdPersona { get; set; }
    public int IdCarrera { get; set; }
    public DateTime FechaInscripcion { get; set; }
    public short? AnioUltimaReinscripcion { get; set; }
    public DateTime? FechaReinscripcion { get; set; }
    public EstadoInscripcionCarrera Estado { get; set; } = EstadoInscripcionCarrera.Activa;

    public Persona Persona { get; set; } = null!;
    public Carrera Carrera { get; set; } = null!;
    public ICollection<InscripcionComision> InscripcionesComision { get; set; } = new List<InscripcionComision>();
    public ICollection<InscripcionExamen> InscripcionesExamen { get; set; } = new List<InscripcionExamen>();
    public ICollection<Certificado> Certificados { get; set; } = new List<Certificado>();
}
