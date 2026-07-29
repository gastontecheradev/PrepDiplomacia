using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrepDiplomacia.Domain.Common;
using PrepDiplomacia.Infrastructure.Services;
using PrepDiplomacia.Infrastructure.Storage;

namespace PrepDiplomacia.Web.Areas.Admin.Controllers;

/// <summary>
/// Gestión del material descargable (PDF "Qué vas a encontrar en el curso").
///
/// El PDF lo sube el admin desde la web: se guarda en /wwwroot/uploads/material
/// y su URL queda persistida en el bloque de contenido "programa.pdf.url".
/// La página del Programa lee esa clave; si está vacía, no muestra el bloque.
/// No requiere entidad ni migración propias.
/// </summary>
[Area("Admin")]
[Authorize(Roles = RolesSistema.Admin)]
[Route("admin/material")]
public class MaterialController : Controller
{
    public const string ClaveUrl    = "programa.pdf.url";
    public const string ClaveNombre = "programa.pdf.nombre";

    // PDF por defecto versionado en el repo: nunca se borra del disco.
    public const string PdfPorDefecto = "/uploads/material/programa-prepdiplomacia.pdf";

    private readonly IContenidoService _contenido;
    private readonly IFileStorageService _storage;

    public MaterialController(IContenidoService contenido, IFileStorageService storage)
    {
        _contenido = contenido;
        _storage = storage;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        ViewBag.UrlActual    = await _contenido.ObtenerAsync(ClaveUrl);
        ViewBag.NombreActual = await _contenido.ObtenerAsync(ClaveNombre);
        return View();
    }

    [HttpPost("subir")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Subir(IFormFile? archivo)
    {
        if (archivo is null || archivo.Length == 0)
        {
            TempData["Error"] = "Seleccioná un archivo PDF.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var nuevaUrl = await _storage.GuardarDocumentoAsync(archivo, "material");
            if (nuevaUrl is null)
            {
                TempData["Error"] = "No se pudo guardar el archivo.";
                return RedirectToAction(nameof(Index));
            }

            // Borramos el PDF anterior para no acumular archivos huérfanos,
            // salvo que sea el PDF por defecto versionado en el repo.
            var anterior = await _contenido.ObtenerAsync(ClaveUrl);
            if (!string.IsNullOrWhiteSpace(anterior) &&
                !anterior.Equals(PdfPorDefecto, StringComparison.OrdinalIgnoreCase))
                _storage.Eliminar(anterior);

            await _contenido.ActualizarPorClaveAsync(
                ClaveUrl, nuevaUrl, "PDF del programa (URL)", "Programa",
                "Se genera al subir el PDF desde Admin › Material descargable.");

            await _contenido.ActualizarPorClaveAsync(
                ClaveNombre, Path.GetFileName(archivo.FileName), "PDF del programa (nombre)", "Programa",
                "Nombre del archivo original, solo informativo.");

            TempData["Ok"] = "PDF actualizado. Ya está disponible en la página del Programa.";
        }
        catch (InvalidOperationException ex)
        {
            // Extensión no permitida o archivo demasiado grande.
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("quitar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Quitar()
    {
        var actual = await _contenido.ObtenerAsync(ClaveUrl);
        if (!string.IsNullOrWhiteSpace(actual) &&
            !actual.Equals(PdfPorDefecto, StringComparison.OrdinalIgnoreCase))
            _storage.Eliminar(actual);

        await _contenido.ActualizarPorClaveAsync(ClaveUrl, "", "PDF del programa (URL)", "Programa");
        await _contenido.ActualizarPorClaveAsync(ClaveNombre, "", "PDF del programa (nombre)", "Programa");

        TempData["Ok"] = "Se quitó el PDF. El bloque de descarga ya no se muestra en el sitio.";
        return RedirectToAction(nameof(Index));
    }
}
