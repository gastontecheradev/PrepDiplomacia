using Microsoft.AspNetCore.Mvc;
using PrepDiplomacia.Domain.Enums;
using PrepDiplomacia.Infrastructure.Services;
using PrepDiplomacia.Web.Models.ViewModels;

namespace PrepDiplomacia.Web.Controllers;

/// <summary>
/// Sección pública de Noticias (/noticias).
///
/// Comparte el motor de publicaciones con el Blog: mismas entidades, mismo
/// editor en el admin y las mismas categorías/tags. Lo que las separa es el
/// Tipo (Noticia vs Articulo), de modo que cada sección lista lo suyo.
/// </summary>
[Route("noticias")]
public class NoticiasController : Controller
{
    private const int TamanioPagina = 9;

    private readonly IBlogService _blog;

    public NoticiasController(IBlogService blog) => _blog = blog;

    [HttpGet("")]
    public async Task<IActionResult> Index(int pagina = 1, int? categoriaId = null,
                                           int? tagId = null, string? q = null)
    {
        var (posts, total) = await _blog.ListarPublicadosAsync(
            pagina, TamanioPagina, categoriaId, tagId, q, TipoPublicacion.Noticia);

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

        var noticia = await _blog.ObtenerPublicadoPorSlugAsync(slug, TipoPublicacion.Noticia);
        if (noticia is null) return NotFound();

        _ = _blog.IncrementarVistasAsync(noticia.Id);

        return View(noticia);
    }
}
