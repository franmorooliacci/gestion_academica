namespace GestionAcademica.Application.Models;

/// <summary>Contenido binario de un certificado listo para descargar.</summary>
public sealed record CertificadoArchivo(byte[] Contenido, string NombreArchivo, string ContentType);
