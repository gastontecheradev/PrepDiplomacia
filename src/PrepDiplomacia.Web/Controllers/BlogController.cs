using Microsoft.AspNetCore.Mvc;
using PrepDiplomacia.Domain.Entities;
using PrepDiplomacia.Infrastructure.Services;
using PrepDiplomacia.Web.Models.ViewModels;

namespace PrepDiplomacia.Web.Controllers;

[Route("blog")]
public class BlogController : Controller
{
    private const int TamanioPagina = 9;

    private readonly IBlogService _blog;

    public BlogController(IBlogService blog) => _blog = blog;

    [HttpGet("")]
    public async Task<IActionResult> Index(int pagina = 1, int? categoriaId = null,
                                           int? tagId = null, string? q = null)
    {
        var (posts, total) = await _blog.ListarPublicadosAsync(pagina, TamanioPagina, categoriaId, tagId, q);
        var totalPaginas = (int)Math.Ceiling(total / (double)TamanioPagina);
        if (totalPaginas == 0) totalPaginas = 1;

        var vm = new BlogPaginadoViewModel
        {
            Posts = posts,
            Pagina = pagina,
            TotalPaginas = totalPaginas,
            Total = total,
            Busqueda = q,
            CategoriaId = categoriaId,
            TagId = tagId,
            Categorias = await _blog.ListarCategoriasAsync(),
            Tags = await _blog.ListarTagsAsync()
        };
        return View(vm);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Detalle(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return NotFound();
        var post = await _blog.ObtenerPublicadoPorSlugAsync(slug);
        if (post is null) return NotFound();

        // Incrementamos vistas en background; no esperamos el resultado en la respuesta.
        _ = _blog.IncrementarVistasAsync(post.Id);

        return View(post);
    }

    [HttpPost("comentar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Comentar(ComentarioViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            TempData["ComentarioError"] = "Revisá los datos del comentario.";
            return RedirectToAction("Detalle", new { slug = TempData["SlugActual"] ?? "" });
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var nuevo = new ComentarioBlog
        {
            PostBlogId  = vm.PostBlogId,
            NombreAutor = vm.NombreAutor,
            EmailAutor  = vm.EmailAutor,
            SitioWeb    = vm.SitioWeb,
            Contenido   = vm.Contenido,
            IpRemitente = ip
        };
        await _blog.CrearComentarioAsync(nuevo);

        TempData["ComentarioOk"] = "Tu comentario fue enviado y aparecerá una vez que sea aprobado.";

        // Para volver al post: necesitamos el slug. Lo cargamos a partir del post.
        var post = await _blog.ObtenerPorIdAsync(vm.PostBlogId);
        return RedirectToAction("Detalle", new { slug = post?.Slug ?? string.Empty });
    }
}
