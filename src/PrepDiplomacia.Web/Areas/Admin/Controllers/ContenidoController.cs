using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrepDiplomacia.Domain.Common;
using PrepDiplomacia.Infrastructure.Services;
using PrepDiplomacia.Web.Models.ViewModels;

namespace PrepDiplomacia.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = RolesSistema.Admin)]
[Route("admin/contenido")]
public class ContenidoController : Controller
{
    private readonly IContenidoService _contenido;

    public ContenidoController(IContenidoService contenido) => _contenido = contenido;

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        // Devolvemos los bloques agrupados por sección, listos para render.
        var agrupados = await _contenido.ObtenerTodosAgrupadosAsync();
        return View(agrupados);
    }

    [HttpPost("guardar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Guardar(BloqueEditarViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "El valor no puede estar vacío.";
            return RedirectToAction(nameof(Index));
        }

        var ok = await _contenido.ActualizarAsync(vm.Id, vm.Valor);
        TempData[ok ? "Ok" : "Error"] = ok
            ? $"Bloque «{vm.Etiqueta}» actualizado."
            : "No se encontró el bloque.";
        return RedirectToAction(nameof(Index));
    }
}
