namespace GestionAcademica.Application.Interfaces;

/// <summary>
/// Configuración de la emisión de certificados (sección `Certificados`).
/// La implementación vive en Infrastructure para que Application no dependa de IConfiguration.
/// </summary>
public interface ICertificadoSettings
{
    /// <summary>URL base pública usada para armar el enlace de validación impreso en el QR.</summary>
    string UrlPublicaBase { get; }
}
