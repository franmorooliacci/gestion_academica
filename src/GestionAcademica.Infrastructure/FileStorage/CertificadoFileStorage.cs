using GestionAcademica.Application.Interfaces;

namespace GestionAcademica.Infrastructure.FileStorage;

/// <summary>
/// Almacenamiento local de PDFs en un directorio protegido (fuera de wwwroot). La API
/// es el único punto que puede leerlos, mediante el endpoint de descarga autenticado.
/// </summary>
public sealed class CertificadoFileStorage : ICertificadoFileStorage
{
    private readonly string _directorioBase;

    public CertificadoFileStorage(CertificadoSettings settings)
    {
        var baseDir = settings.DirectorioBase;
        _directorioBase = Path.GetFullPath(
            Path.IsPathRooted(baseDir)
                ? baseDir
                : Path.Combine(Directory.GetCurrentDirectory(), baseDir));
    }

    public async Task<string> GuardarAsync(string nombreArchivo, byte[] contenido, CancellationToken ct = default)
    {
        var ruta = ResolverRuta(nombreArchivo);
        Directory.CreateDirectory(_directorioBase);
        await File.WriteAllBytesAsync(ruta, contenido, ct);
        return Path.GetFileName(ruta);
    }

    public async Task<byte[]?> LeerAsync(string rutaRelativa, CancellationToken ct = default)
    {
        var ruta = ResolverRuta(rutaRelativa);
        return File.Exists(ruta) ? await File.ReadAllBytesAsync(ruta, ct) : null;
    }

    public Task EliminarAsync(string rutaRelativa, CancellationToken ct = default)
    {
        try
        {
            var ruta = ResolverRuta(rutaRelativa);
            if (File.Exists(ruta))
                File.Delete(ruta);
        }
        catch (IOException) { /* best effort */ }
        catch (UnauthorizedAccessException) { /* best effort */ }

        return Task.CompletedTask;
    }

    /// <summary>Resuelve y valida que el nombre no pueda escapar del directorio base.</summary>
    private string ResolverRuta(string nombreArchivo)
    {
        var nombre = Path.GetFileName(nombreArchivo);
        if (string.IsNullOrWhiteSpace(nombre) || !string.Equals(nombre, nombreArchivo, StringComparison.Ordinal))
            throw new ArgumentException("Nombre de archivo de certificado inválido.", nameof(nombreArchivo));

        var ruta = Path.GetFullPath(Path.Combine(_directorioBase, nombre));
        var prefijo = _directorioBase.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!ruta.StartsWith(prefijo, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Ruta de certificado fuera del directorio permitido.", nameof(nombreArchivo));

        return ruta;
    }
}
