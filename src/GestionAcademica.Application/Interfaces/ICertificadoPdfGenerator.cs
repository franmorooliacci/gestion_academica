using GestionAcademica.Application.Models;

namespace GestionAcademica.Application.Interfaces;

/// <summary>
/// Genera el PDF de un certificado (QuestPDF + QR de validación).
/// La implementación concreta vive en Infrastructure/FileStorage.
/// </summary>
public interface ICertificadoPdfGenerator
{
    byte[] Generar(CertificadoPdfModel model);
}
