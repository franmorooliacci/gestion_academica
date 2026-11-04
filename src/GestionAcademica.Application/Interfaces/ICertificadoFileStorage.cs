namespace GestionAcademica.Application.Interfaces;

/// <summary>
/// Guarda y lee los PDFs de certificados en una carpeta local protegida (fuera de
/// cualquier directorio servido estáticamente). Solo la API accede a esos archivos.
/// </summary>
public interface ICertificadoFileStorage
{
    /// <summary>Guarda el contenido y devuelve la ruta relativa a registrar en la BD.</summary>
    Task<string> GuardarAsync(string nombreArchivo, byte[] contenido, CancellationToken ct = default);

    /// <summary>Lee un archivo por su ruta relativa. Devuelve null si no existe.</summary>
    Task<byte[]?> LeerAsync(string rutaRelativa, CancellationToken ct = default);

    /// <summary>Elimina un archivo si existe (best effort).</summary>
    Task EliminarAsync(string rutaRelativa, CancellationToken ct = default);
}
