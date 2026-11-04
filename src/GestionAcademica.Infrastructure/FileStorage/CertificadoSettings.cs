using GestionAcademica.Application.Interfaces;

namespace GestionAcademica.Infrastructure.FileStorage;

/// <summary>Configuración de la sección `Certificados` de appsettings.</summary>
public sealed class CertificadoSettings : ICertificadoSettings
{
    public const string SectionName = "Certificados";

    /// <summary>Carpeta (absoluta o relativa al directorio de trabajo) donde se guardan los PDFs.</summary>
    public string DirectorioBase { get; init; } = "storage/certificados";

    /// <summary>URL base pública usada en el QR de validación.</summary>
    public string UrlPublicaBase { get; init; } = "http://localhost:5125";
}
