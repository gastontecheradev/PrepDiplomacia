using Microsoft.AspNetCore.Mvc;
using PrepDiplomacia.Infrastructure.Services;

namespace PrepDiplomacia.Web.Controllers;

public class HomeController : Controller
{
    private readonly IBlogService _blog;

    public HomeController(IBlogService blog) => _blog = blog;

    public async Task<IActionResult> Index()
    {
        // Cargamos los 3 posts más recientes para la sección "Últimas publicaciones".
        var recientes = await _blog.ObtenerRecientesAsync(3);
        return View(recientes);
    }
}
