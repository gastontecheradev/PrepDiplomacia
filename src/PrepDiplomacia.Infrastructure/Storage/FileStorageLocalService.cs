using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace PrepDiplomacia.Infrastructure.Storage;

public interface IFileStorageService
{
    /// <summary>
    /// Guarda un archivo subido en una subcarpeta de /wwwroot/uploads.
    /// Retorna el path relativo (ej. "/uploads/blog/abc123.jpg") apto para usar en src.
    /// </summary>
    Task<string?> GuardarImagenAsync(IFormFile archivo, string subcarpeta,
                                     CancellationToken ct = default);

    /// <summary>Elimina un archivo existente. No falla si no existe.</summary>
    void Eliminar(string? rutaRelativa);
}

/// <summary>
/// Almacenamiento local en wwwroot/uploads. Cuando el sitio crezca
/// se puede sustituir por Azure Blob / S3 cambiando esta única clase.
///
/// Restricciones de seguridad:
///  - Solo se aceptan extensiones de imagen conocidas (whitelist).
///  - Se renombra el archivo con GUID — nunca confiamos en el nombre original.
///  - Tamaño máximo configurable (5 MB por defecto).
/// </summary>
public class FileStorageLocalService : IFileStorageService
{
    private static readonly string[] ExtensionesPermitidas =
        new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".svg" };

    private const long TamañoMaximoBytes = 5 * 1024 * 1024; // 5 MB

    private readonly IWebHostEnvironment _env;
    private readonly ILogger<FileStorageLocalService> _logger;

    public FileStorageLocalService(IWebHostEnvironment env, ILogger<FileStorageLocalService> logger)
    {
        _env = env;
        _logger = logger;
    }

    public async Task<string?> GuardarImagenAsync(IFormFile archivo, string subcarpeta,
                                                  CancellationToken ct = default)
    {
        if (archivo is null || archivo.Length == 0) return null;

        if (archivo.Length > TamañoMaximoBytes)
        {
            _logger.LogWarning("Archivo demasiado grande: {Tamaño} bytes", archivo.Length);
            throw new InvalidOperationException($"El archivo supera el tamaño máximo de {TamañoMaximoBytes / (1024 * 1024)} MB.");
        }

        var ext = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        if (!ExtensionesPermitidas.Contains(ext))
            throw new InvalidOperationException($"Tipo de archivo no permitido. Permitidos: {string.Join(", ", ExtensionesPermitidas)}");

        // Sanitizamos la subcarpeta — solo letras/números/guiones.
        var sub = string.Concat(subcarpeta.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_'));
        if (string.IsNullOrEmpty(sub)) sub = "general";

        var carpetaAbsoluta = Path.Combine(_env.WebRootPath, "uploads", sub);
        Directory.CreateDirectory(carpetaAbsoluta);

        var nombreArchivo = $"{Guid.NewGuid():N}{ext}";
        var rutaAbsoluta = Path.Combine(carpetaAbsoluta, nombreArchivo);

        await using (var stream = File.Create(rutaAbsoluta))
        {
            await archivo.CopyToAsync(stream, ct);
        }

        var rutaRelativa = $"/uploads/{sub}/{nombreArchivo}";
        _logger.LogInformation("Imagen guardada: {Ruta}", rutaRelativa);
        return rutaRelativa;
    }

    public void Eliminar(string? rutaRelativa)
    {
        if (string.IsNullOrWhiteSpace(rutaRelativa)) return;
        if (!rutaRelativa.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase)) return;

        try
        {
            var rutaAbsoluta = Path.Combine(_env.WebRootPath,
                rutaRelativa.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(rutaAbsoluta)) File.Delete(rutaAbsoluta);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo eliminar {Ruta}", rutaRelativa);
        }
    }
}
