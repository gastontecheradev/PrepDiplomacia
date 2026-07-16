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

/// <summary>
/// ABM de Noticias. Reutiliza el mismo motor de publicaciones que el blog
/// (entidad, editor, subida de imágenes, slugs y estados), forzando siempre
/// Tipo = Noticia para que aparezcan en /noticias y no en /blog.
/// </summary>
[Area("Admin")]
[Authorize(Roles = RolesSistema.Admin)]
[Route("admin/noticias")]
public class NoticiasController : Controller
{
    private const TipoPublicacion Tipo = TipoPublicacion.Noticia;

    private readonly IBlogService _blog;
    private readonly IFileStorageService _storage;
    private readonly UserManager<UsuarioAplicacion> _userManager;

    public NoticiasController(IBlogService blog, IFileStorageService storage,
                              UserManager<UsuarioAplicacion> userManager)
    {
        _blog = blog;
        _storage = storage;
        _userManager = userManager;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index() => View(await _blog.ListarTodosAsync(Tipo));

    [HttpGet("nueva")]
    public async Task<IActionResult> Nueva()
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
        var noticia = await _blog.ObtenerPorIdAsync(id);
        if (noticia is null || noticia.Tipo != Tipo) return NotFound();

        var vm = new PostEditarViewModel
        {
            Id = noticia.Id,
            Titulo = noticia.Titulo,
            Resumen = noticia.Resumen,
            Contenido = noticia.Contenido,
            ImagenActual = noticia.ImagenDestacada,
            ImagenAlt = noticia.ImagenAlt,
            YouTubeVideoId = noticia.YouTubeVideoId,
            CategoriaId = noticia.CategoriaId,
            TagsSeleccionados = noticia.PostTags.Select(pt => pt.TagBlogId).ToArray(),
            Estado = noticia.Estado,
            ComentariosHabilitados = noticia.ComentariosHabilitados,
            MetaTitulo = noticia.MetaTitulo,
            MetaDescripcion = noticia.MetaDescripcion,
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

        string? imagenRuta = vm.ImagenActual;
        if (vm.Imagen is not null && vm.Imagen.Length > 0)
        {
            try
            {
                imagenRuta = await _storage.GuardarImagenAsync(vm.Imagen, "noticias");
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

        var noticia = new PostBlog
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
            Tipo = Tipo,
            ComentariosHabilitados = vm.ComentariosHabilitados,
            MetaTitulo = vm.MetaTitulo,
            MetaDescripcion = vm.MetaDescripcion,
            AutorId = admin?.Id ?? string.Empty
        };

        if (vm.Id == 0)
            await _blog.CrearAsync(noticia, vm.TagsSeleccionados);
        else
            await _blog.ActualizarAsync(noticia, vm.TagsSeleccionados);

        TempData["Ok"] = "Noticia guardada.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("eliminar/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id)
    {
        var noticia = await _blog.ObtenerPorIdAsync(id);
        if (noticia is null || noticia.Tipo != Tipo) return NotFound();

        if (!string.IsNullOrEmpty(noticia.ImagenDestacada))
            _storage.Eliminar(noticia.ImagenDestacada);

        await _blog.EliminarAsync(id);
        TempData["Ok"] = "Noticia eliminada.";
        return RedirectToAction(nameof(Index));
    }
}
