using Microsoft.AspNetCore.Mvc;
using PrepDiplomacia.Infrastructure.Email;
using PrepDiplomacia.Infrastructure.Services;
using PrepDiplomacia.Web.Models.ViewModels;

namespace PrepDiplomacia.Web.Controllers;

[Route("inscripcion")]
public class InscripcionController : Controller
{
    private readonly IPlanCursoService _planes;
    private readonly IInscripcionService _inscripciones;
    private readonly IEmailService _email;

    public InscripcionController(
        IPlanCursoService planes,
        IInscripcionService inscripciones,
        IEmailService email)
    {
        _planes = planes;
        _inscripciones = inscripciones;
        _email = email;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var vm = new InscripcionViewModel
        {
            PlanesDisponibles = await _planes.ListarActivosAsync()
        };
        return View(vm);
    }

    [HttpPost("iniciar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Iniciar(InscripcionViewModel vm)
    {
        vm.PlanesDisponibles = await _planes.ListarActivosAsync();

        if (!ModelState.IsValid)
            return View("Index", vm);

        var plan = await _planes.ObtenerPorIdAsync(vm.PlanCursoId);
        if (plan is null || !plan.Activo)
        {
            ModelState.AddModelError(nameof(vm.PlanCursoId), "El plan seleccionado no existe.");
            return View("Index", vm);
        }

        // Forzamos modalidad según el plan elegido (no confiamos en el form).
        var modalidadFinal = plan.ModalidadPago;

        var inscripcion = await _inscripciones.CrearOActualizarPendienteAsync(
            vm.NombreCompleto, vm.Email, vm.Telefono,
            vm.FormacionAcademica, vm.Consulta,
            plan.Id, modalidadFinal);

        // Envío email de confirmación de recepción al candidato + notificación al admin.
        await _email.EnviarNotificacionInscripcionAsync(
            inscripcion.NombreCompleto, inscripcion.Email, plan.Nombre);

        // Redirigimos al checkout de Stripe.
        return RedirectToAction("Checkout", "Pago", new { id = inscripcion.Id });
    }
}
