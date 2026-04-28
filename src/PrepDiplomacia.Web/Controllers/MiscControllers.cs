using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PrepDiplomacia.Domain.Common;
using PrepDiplomacia.Domain.Entities;
using PrepDiplomacia.Infrastructure.Email;
using PrepDiplomacia.Infrastructure.Identity;
using PrepDiplomacia.Infrastructure.Services;
using PrepDiplomacia.Web.Models.ViewModels;

namespace PrepDiplomacia.Web.Controllers;

// ─────────────────────────────────────────────────────────────────────────────
// NEWSLETTER
// ─────────────────────────────────────────────────────────────────────────────
[Route("newsletter")]
public class NewsletterController : Controller
{
    private readonly ISuscriptorService _suscriptor;
    public NewsletterController(ISuscriptorService suscriptor) => _suscriptor = suscriptor;

    [HttpPost("suscribir")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Suscribir(NewsletterViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            // Para llamadas AJAX devolvemos JSON; para form normal, redirección.
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { ok = false, mensaje = "Ingresá un correo válido." });

            TempData["NewsletterError"] = "Ingresá un correo válido.";
            return Redirect(Request.Headers["Referer"].ToString() ?? "/");
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var (ok, mensaje) = await _suscriptor.SuscribirAsync(vm.Email, vm.Nombre, vm.Origen ?? "newsletter", ip);

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return Json(new { ok, mensaje });

        TempData[ok ? "NewsletterOk" : "NewsletterError"] = mensaje;
        return Redirect(Request.Headers["Referer"].ToString() ?? "/");
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// CONTACTO
// ─────────────────────────────────────────────────────────────────────────────
[Route("contacto")]
public class ContactoController : Controller
{
    private readonly IMensajeService _mensajes;
    private readonly IEmailService _email;

    public ContactoController(IMensajeService mensajes, IEmailService email)
    {
        _mensajes = mensajes;
        _email = email;
    }

    [HttpGet("")]
    public IActionResult Index() => View(new ContactoViewModel());

    [HttpPost("enviar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enviar(ContactoViewModel vm)
    {
        if (!ModelState.IsValid) return View("Index", vm);

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        await _mensajes.CrearAsync(new MensajeContacto
        {
            Nombre = vm.Nombre,
            Email = vm.Email,
            Telefono = vm.Telefono,
            Asunto = vm.Asunto,
            Mensaje = vm.Mensaje,
            Origen = "contacto_general",
            IpRemitente = ip
        });

        await _email.EnviarFormularioContactoAsync(
            vm.Nombre, vm.Email, vm.Telefono, vm.Asunto, vm.Mensaje, "contacto_general");

        TempData["MensajeOk"] = "Recibimos tu mensaje. Te respondemos a la brevedad.";
        return RedirectToAction(nameof(Index));
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// ÁREA DE ALUMNOS
// ─────────────────────────────────────────────────────────────────────────────
[Route("area-alumnos")]
[Authorize(Roles = RolesSistema.Alumno + "," + RolesSistema.Admin)]
public class AreaAlumnosController : Controller
{
    private readonly UserManager<UsuarioAplicacion> _userManager;
    public AreaAlumnosController(UserManager<UsuarioAplicacion> userManager) => _userManager = userManager;

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var usuario = await _userManager.GetUserAsync(User);
        return View(usuario);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// ERROR
// ─────────────────────────────────────────────────────────────────────────────
[Route("error")]
public class ErrorController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View("Error");

    [HttpGet("{codigo:int}")]
    public IActionResult Codigo(int codigo)
    {
        ViewBag.Codigo = codigo;
        return View("Error");
    }
}
