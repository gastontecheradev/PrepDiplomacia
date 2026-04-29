using Microsoft.AspNetCore.Mvc;

namespace PrepDiplomacia.Web.Controllers;

public class SobreController : Controller
{
    [Route("sobre-prep")]
    public IActionResult Index() => View();
}

public class ProgramaController : Controller
{
    [Route("programa")]
    public IActionResult Index() => View();
}

public class LegalController : Controller
{
    [Route("legal/terminos")]
    public IActionResult Terminos() => View();

    [Route("legal/privacidad")]
    public IActionResult Privacidad() => View();
}
