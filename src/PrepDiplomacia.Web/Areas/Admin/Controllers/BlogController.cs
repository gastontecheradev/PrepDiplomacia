using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PrepDiplomacia.Domain.Common;
using PrepDiplomacia.Domain.Entities;
using PrepDiplomacia.Domain.Enums;
using PrepDiplomacia.Infrastructure.Identity;
using PrepDiplomacia.Infrastructure.Services;
using PrepDiplomacia.Infrastructure.Storage;
using PrepDiplomacia.Web.Models.ViewModels;

namespace PrepDiplomacia.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = RolesSistema.Admin)]
[Route("admin/blog")]
public class BlogController : Controller
{
    private readonly IBlogService _blog;
    private readonly IFileStorageService _storage;
    private readonly UserManager<UsuarioAplicacion> _userManager;

    public BlogController(IBlogService blog, IFileStorageService storage,
                          UserManager<UsuarioAplicacion> userManager)
    {
        _blog = blog;
        _storage = storage;
        _userManager = userManager;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index() => View(await _blog.ListarTodosAsync());

    [HttpGet("nuevo")]
    public async Task<IActionResult> Nuevo()
    {
        var vm = new PostEditarViewModel
        {
            Categorias = await _blog.ListarCategoriasAsync(),
            Tags = await _blog.ListarTagsAsync()
        };
        return View("Editar", vm);
    }

    [HttpGet("editar/{id:int}")]
    public async Task<IActionResult> Editar(int id)
    {
        var post = await _blog.ObtenerPorIdAsync(id);
        if (post is null) return NotFound();

        var vm = new PostEditarViewModel
        {
            Id = post.Id,
            Titulo = post.Titulo,
            Resumen = post.Resumen,
            Contenido = post.Contenido,
            ImagenActual = post.ImagenDestacada,
            ImagenAlt = post.ImagenAlt,
            YouTubeVideoId = post.YouTubeVideoId,
            CategoriaId = post.CategoriaId,
            TagsSeleccionados = post.PostTags.Select(pt => pt.TagBlogId).ToArray(),
            Estado = post.Estado,
            ComentariosHabilitados = post.ComentariosHabilitados,
            MetaTitulo = post.MetaTitulo,
            MetaDescripcion = post.MetaDescripcion,
            Categorias = await _blog.ListarCategoriasAsync(),
            Tags = await _blog.ListarTagsAsync()
        };
        return View("Editar", vm);
    }

    [HttpPost("guardar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Guardar(PostEditarViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.Categorias = await _blog.ListarCategoriasAsync();
            vm.Tags = await _blog.ListarTagsAsync();
            return View("Editar", vm);
        }

        // Subimos imagen si vino una nueva.
        string? imagenRuta = vm.ImagenActual;
        if (vm.Imagen is not null && vm.Imagen.Length > 0)
        {
            try
            {
                imagenRuta = await _storage.GuardarImagenAsync(vm.Imagen, "blog");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(nameof(vm.Imagen), ex.Message);
                vm.Categorias = await _blog.ListarCategoriasAsync();
                vm.Tags = await _blog.ListarTagsAsync();
                return View("Editar", vm);
            }
        }

        var admin = await _userManager.GetUserAsync(User);

        var post = new PostBlog
        {
            Id = vm.Id,
            Titulo = vm.Titulo,
            Resumen = vm.Resumen ?? string.Empty,
            Contenido = vm.Contenido,
            ImagenDestacada = imagenRuta,
            ImagenAlt = vm.ImagenAlt,
            YouTubeVideoId = vm.YouTubeVideoId,
            CategoriaId = vm.CategoriaId,
            Estado = vm.Estado,
            ComentariosHabilitados = vm.ComentariosHabilitados,
            MetaTitulo = vm.MetaTitulo,
            MetaDescripcion = vm.MetaDescripcion,
            AutorId = admin?.Id ?? string.Empty
        };

        if (vm.Id == 0)
            await _blog.CrearAsync(post, vm.TagsSeleccionados);
        else
            await _blog.ActualizarAsync(post, vm.TagsSeleccionados);

        TempData["Ok"] = "Publicación guardada.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("eliminar/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id)
    {
        var post = await _blog.ObtenerPorIdAsync(id);
        if (post is not null && !string.IsNullOrEmpty(post.ImagenDestacada))
            _storage.Eliminar(post.ImagenDestacada);

        await _blog.EliminarAsync(id);
        TempData["Ok"] = "Publicación eliminada.";
        return RedirectToAction(nameof(Index));
    }

    // ── Comentarios ─────────────────────────────────────────────────────────
    [HttpGet("comentarios")]
    public async Task<IActionResult> Comentarios()
    {
        return View(await _blog.ListarComentariosPendientesAsync());
    }

    [HttpPost("comentarios/{id:int}/aprobar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AprobarComentario(int id)
    {
        await _blog.AprobarComentarioAsync(id);
        TempData["Ok"] = "Comentario aprobado.";
        return RedirectToAction(nameof(Comentarios));
    }

    [HttpPost("comentarios/{id:int}/eliminar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarComentario(int id)
    {
        await _blog.EliminarComentarioAsync(id);
        TempData["Ok"] = "Comentario eliminado.";
        return RedirectToAction(nameof(Comentarios));
    }
}
