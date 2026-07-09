using Microsoft.AspNetCore.Mvc;
using PrepDiplomacia.Infrastructure.Email;
using PrepDiplomacia.Infrastructure.Services;
using PrepDiplomacia.Web.Models.ViewModels;

namespace PrepDiplomacia.Web.Controllers;

[Route("inscripcion")]
public class InscripcionController : Controller
{
    private readonly IInscripcionService _inscripciones;
    private readonly IEmailService _email;

    public InscripcionController(
        IInscripcionService inscripciones,
        IEmailService email)
    {
        _inscripciones = inscripciones;
        _email = email;
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        return View(new InscripcionViewModel());
    }

    [HttpPost("preinscribir")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Preinscribir(InscripcionViewModel vm)
    {
        if (!ModelState.IsValid)
            return View("Index", vm);

        var preinscripcion = await _inscripciones.CrearPreinscripcionAsync(
            vm.NombreCompleto, vm.Email, vm.Telefono,
            vm.FormacionAcademica, vm.Consulta);

        // Email de confirmación al candidato + notificación al admin (sin pago).
        await _email.EnviarNotificacionPreinscripcionAsync(
            preinscripcion.NombreCompleto, preinscripcion.Email);

        TempData["MensajeOk"] =
            "¡Recibimos tu preinscripción! Te vamos a contactar a la brevedad para coordinar los próximos pasos.";

        return RedirectToAction(nameof(Index));
    }
}
