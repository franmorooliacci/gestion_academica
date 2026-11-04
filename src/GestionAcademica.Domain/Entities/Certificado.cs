using GestionAcademica.Shared.Contracts.Enums;

namespace GestionAcademica.Domain.Entities;

/// <summary>Mapea la tabla `certificados`.</summary>
public sealed class Certificado
{
    public int IdCertificado { get; set; }
    public int IdInscripcionCarrera { get; set; }
    public TipoCertificado Tipo { get; set; }
    public DateTime FechaGeneracion { get; set; }
    public string? RutaArchivo { get; set; }
    public string CodigoValidacion { get; set; } = null!;

    public InscripcionCarrera InscripcionCarrera { get; set; } = null!;
}
