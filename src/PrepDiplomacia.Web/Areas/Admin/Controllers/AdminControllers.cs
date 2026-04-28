using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrepDiplomacia.Domain.Common;
using PrepDiplomacia.Domain.Entities;
using PrepDiplomacia.Infrastructure.Data;
using PrepDiplomacia.Infrastructure.Services;
using PrepDiplomacia.Web.Models.ViewModels;

namespace PrepDiplomacia.Web.Areas.Admin.Controllers;

// ─────────────────────────────────────────────────────────────────────────────
// NEWSLETTER (admin)
// ─────────────────────────────────────────────────────────────────────────────
[Area("Admin")]
[Authorize(Roles = RolesSistema.Admin)]
[Route("admin/newsletter")]
public class NewsletterAdminController : Controller
{
    private readonly ISuscriptorService _suscriptores;
    public NewsletterAdminController(ISuscriptorService suscriptores) => _suscriptores = suscriptores;

    [HttpGet("")]
    public async Task<IActionResult> Index() => View(await _suscriptores.ListarTodosAsync());

    [HttpPost("{id:int}/baja")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DarDeBaja(int id)
    {
        await _suscriptores.DarDeBajaAsync(id);
        TempData["Ok"] = "Suscriptor dado de baja.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/eliminar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _suscriptores.EliminarAsync(id);
        TempData["Ok"] = "Suscriptor eliminado.";
        return RedirectToAction(nameof(Index));
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// INSCRIPCIONES (admin)
// ─────────────────────────────────────────────────────────────────────────────
[Area("Admin")]
[Authorize(Roles = RolesSistema.Admin)]
[Route("admin/inscripciones")]
public class InscripcionesAdminController : Controller
{
    private readonly IInscripcionService _inscripciones;
    public InscripcionesAdminController(IInscripcionService inscripciones) => _inscripciones = inscripciones;

    [HttpGet("")]
    public async Task<IActionResult> Index() => View(await _inscripciones.ListarTodasAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detalle(int id)
    {
        var insc = await _inscripciones.ObtenerPorIdAsync(id);
        if (insc is null) return NotFound();
        return View(insc);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// MENSAJES (admin)
// ─────────────────────────────────────────────────────────────────────────────
[Area("Admin")]
[Authorize(Roles = RolesSistema.Admin)]
[Route("admin/mensajes")]
public class MensajesAdminController : Controller
{
    private readonly IMensajeService _mensajes;
    public MensajesAdminController(IMensajeService mensajes) => _mensajes = mensajes;

    [HttpGet("")]
    public async Task<IActionResult> Index() => View(await _mensajes.ListarTodosAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detalle(int id)
    {
        var m = await _mensajes.ObtenerPorIdAsync(id);
        if (m is null) return NotFound();
        if (!m.Leido) await _mensajes.MarcarLeidoAsync(id);
        return View(m);
    }

    [HttpPost("{id:int}/eliminar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _mensajes.EliminarAsync(id);
        TempData["Ok"] = "Mensaje eliminado.";
        return RedirectToAction(nameof(Index));
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// CATEGORÍAS Y TAGS (admin)
// ─────────────────────────────────────────────────────────────────────────────
[Area("Admin")]
[Authorize(Roles = RolesSistema.Admin)]
[Route("admin/categorias")]
public class CategoriasAdminController : Controller
{
    private readonly AppDbContext _db;
    public CategoriasAdminController(AppDbContext db) => _db = db;

    [HttpGet("")]
    public async Task<IActionResult> Index() =>
        View(await _db.Categorias.OrderBy(c => c.Nombre).ToListAsync());

    [HttpPost("crear")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(CategoriaEditarViewModel vm)
    {
        if (ModelState.IsValid)
        {
            _db.Categorias.Add(new CategoriaBlog
            {
                Nombre = vm.Nombre,
                Slug = SlugHelper.Generar(vm.Nombre),
                Descripcion = vm.Descripcion,
                Color = vm.Color
            });
            await _db.SaveChangesAsync();
            TempData["Ok"] = "Categoría creada.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/editar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, CategoriaEditarViewModel vm)
    {
        var c = await _db.Categorias.FindAsync(id);
        if (c is null) return NotFound();
        if (!ModelState.IsValid) return RedirectToAction(nameof(Index));

        c.Nombre = vm.Nombre;
        c.Slug = SlugHelper.Generar(vm.Nombre);
        c.Descripcion = vm.Descripcion;
        c.Color = vm.Color;
        await _db.SaveChangesAsync();
        TempData["Ok"] = "Categoría actualizada.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/eliminar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id)
    {
        var c = await _db.Categorias.FindAsync(id);
        if (c is not null)
        {
            _db.Categorias.Remove(c);
            await _db.SaveChangesAsync();
            TempData["Ok"] = "Categoría eliminada.";
        }
        return RedirectToAction(nameof(Index));
    }

    // Tags simples (pueden gestionarse desde la misma vista).
    [HttpPost("tags/crear")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearTag(string nombre)
    {
        if (!string.IsNullOrWhiteSpace(nombre))
        {
            var slug = SlugHelper.Generar(nombre);
            if (!await _db.Tags.AnyAsync(t => t.Slug == slug))
            {
                _db.Tags.Add(new TagBlog { Nombre = nombre, Slug = slug });
                await _db.SaveChangesAsync();
                TempData["Ok"] = "Tag creado.";
            }
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("tags/{id:int}/eliminar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarTag(int id)
    {
        var t = await _db.Tags.FindAsync(id);
        if (t is not null)
        {
            _db.Tags.Remove(t);
            await _db.SaveChangesAsync();
            TempData["Ok"] = "Tag eliminado.";
        }
        return RedirectToAction(nameof(Index));
    }
}
