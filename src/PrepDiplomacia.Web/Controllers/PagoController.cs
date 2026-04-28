using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PrepDiplomacia.Domain.Entities;
using PrepDiplomacia.Domain.Enums;
using PrepDiplomacia.Infrastructure.Data;
using PrepDiplomacia.Infrastructure.Email;
using PrepDiplomacia.Infrastructure.Identity;
using PrepDiplomacia.Infrastructure.Payments;
using PrepDiplomacia.Infrastructure.Services;
using Stripe;
using Stripe.Checkout;

namespace PrepDiplomacia.Web.Controllers;

[Route("pago")]
public class PagoController : Controller
{
    private readonly IInscripcionService _inscripciones;
    private readonly IPlanCursoService _planes;
    private readonly IStripeService _stripe;
    private readonly IEmailService _email;
    private readonly StripeOptions _stripeOpt;
    private readonly AppDbContext _db;
    private readonly UserManager<UsuarioAplicacion> _userManager;
    private readonly ILogger<PagoController> _logger;

    public PagoController(
        IInscripcionService inscripciones,
        IPlanCursoService planes,
        IStripeService stripe,
        IEmailService email,
        IOptions<StripeOptions> stripeOpt,
        AppDbContext db,
        UserManager<UsuarioAplicacion> userManager,
        ILogger<PagoController> logger)
    {
        _inscripciones = inscripciones;
        _planes = planes;
        _stripe = stripe;
        _email = email;
        _stripeOpt = stripeOpt.Value;
        _db = db;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet("checkout/{id:int}")]
    public async Task<IActionResult> Checkout(int id)
    {
        var inscripcion = await _inscripciones.ObtenerPorIdAsync(id);
        if (inscripcion is null) return NotFound();

        var plan = await _planes.ObtenerPorIdAsync(inscripcion.PlanCursoId);
        if (plan is null) return NotFound();

        // Si Stripe no está configurado, mostramos un mensaje en la vista.
        if (string.IsNullOrWhiteSpace(_stripeOpt.SecretKey))
        {
            ViewBag.StripeNoConfigurado = true;
            return View("Checkout", inscripcion);
        }

        var siteUrl = string.IsNullOrWhiteSpace(_stripeOpt.SiteUrl)
            ? $"{Request.Scheme}://{Request.Host}"
            : _stripeOpt.SiteUrl.TrimEnd('/');

        var successUrl = $"{siteUrl}/pago/success?inscripcion={inscripcion.Id}&session={{CHECKOUT_SESSION_ID}}";
        var cancelUrl  = $"{siteUrl}/pago/cancel?inscripcion={inscripcion.Id}";

        var resultado = inscripcion.ModalidadElegida == ModalidadPago.Cuotas
            ? await _stripe.CrearSesionCuotasAsync(inscripcion, plan, successUrl, cancelUrl)
            : await _stripe.CrearSesionPagoUnicoAsync(inscripcion, plan, successUrl, cancelUrl);

        if (!resultado.Exito || string.IsNullOrEmpty(resultado.UrlCheckout))
        {
            ViewBag.ErrorStripe = resultado.Error ?? "No pudimos iniciar el pago. Probá nuevamente o contactanos.";
            return View("Checkout", inscripcion);
        }

        // Guardamos un Pago "Pendiente" antes de redirigir, para auditoría.
        var pagoPendiente = new Pago
        {
            InscripcionId = inscripcion.Id,
            Monto = plan.PrecioTotal,
            Moneda = plan.Moneda,
            Estado = inscripcion.ModalidadElegida == ModalidadPago.Cuotas
                ? EstadoPago.EnCuotas
                : EstadoPago.Pendiente,
            StripeSessionId = resultado.SessionId,
            TotalCuotas = plan.CantidadCuotas
        };
        _db.Pagos.Add(pagoPendiente);
        await _db.SaveChangesAsync();

        return Redirect(resultado.UrlCheckout);
    }

    [HttpGet("success")]
    public async Task<IActionResult> Success(int? inscripcion, string? session)
    {
        // El pago real se confirma vía webhook (que ya pudo haber llegado o no).
        // Si el webhook ya activó la inscripción, mostramos confirmación.
        // Si no, mostramos un mensaje "estamos procesando tu pago".
        if (inscripcion.HasValue)
        {
            var ins = await _inscripciones.ObtenerPorIdAsync(inscripcion.Value);
            ViewBag.YaActiva = ins?.Estado == EstadoInscripcion.Activa;
            return View(ins);
        }
        return View(null);
    }

    [HttpGet("cancel")]
    public IActionResult Cancel(int? inscripcion) => View();

    /// <summary>
    /// Webhook de Stripe. Procesa eventos checkout.session.completed,
    /// invoice.paid, invoice.payment_failed con idempotencia.
    /// </summary>
    [HttpPost("webhook")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Webhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var firma = Request.Headers["Stripe-Signature"].ToString();

        Event evento;
        try
        {
            evento = _stripe.ConstruirEventoDesdeWebhook(json, firma);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Firma de webhook inválida");
            return BadRequest("Firma inválida");
        }

        // Idempotencia: si ya procesamos este evento, no hacer nada.
        if (await _db.EventosStripe.AnyAsync(e => e.EventoId == evento.Id))
        {
            _logger.LogInformation("Evento Stripe duplicado ignorado: {Id}", evento.Id);
            return Ok();
        }

        try
        {
            switch (evento.Type)
            {
                case "checkout.session.completed":
                    await ManejarCheckoutSessionCompletedAsync(evento);
                    break;

                case "invoice.paid":
                    await ManejarInvoicePaidAsync(evento);
                    break;

                case "invoice.payment_failed":
                    await ManejarInvoicePaymentFailedAsync(evento);
                    break;

                default:
                    _logger.LogInformation("Evento Stripe ignorado: {Tipo}", evento.Type);
                    break;
            }

            _db.EventosStripe.Add(new EventoStripeProcesado
            {
                EventoId = evento.Id,
                TipoEvento = evento.Type,
                Resultado = "OK"
            });
            await _db.SaveChangesAsync();
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando evento Stripe {Id} ({Tipo})", evento.Id, evento.Type);
            // Devolvemos 500 para que Stripe reintente.
            return StatusCode(500);
        }
    }

    private async Task ManejarCheckoutSessionCompletedAsync(Event evento)
    {
        var session = evento.Data.Object as Session;
        if (session is null) return;

        if (!int.TryParse(session.ClientReferenceId, out var inscripcionId))
        {
            _logger.LogWarning("ClientReferenceId no parseable: {Ref}", session.ClientReferenceId);
            return;
        }

        var inscripcion = await _inscripciones.ObtenerPorIdAsync(inscripcionId);
        if (inscripcion is null) return;

        var plan = await _planes.ObtenerPorIdAsync(inscripcion.PlanCursoId);

        // Buscar el Pago "Pendiente" creado al iniciar Checkout.
        var pago = await _db.Pagos.FirstOrDefaultAsync(p => p.StripeSessionId == session.Id);
        pago ??= new Pago
        {
            InscripcionId = inscripcion.Id,
            Monto = (session.AmountTotal ?? 0) / 100m,
            Moneda = session.Currency ?? "usd",
            StripeSessionId = session.Id
        };
        if (pago.Id == 0) _db.Pagos.Add(pago);

        pago.StripePaymentIntentId = session.PaymentIntentId;
        pago.StripeSubscriptionId = session.SubscriptionId;
        pago.StripeCustomerId = session.CustomerId;
        pago.FechaPago = DateTime.UtcNow;
        pago.NumeroCuota = inscripcion.ModalidadElegida == ModalidadPago.Cuotas ? 1 : null;

        // En pago único, marcamos Completado de una.
        // En cuotas, queda EnCuotas y los siguientes invoice.paid agregan más Pagos.
        pago.Estado = inscripcion.ModalidadElegida == ModalidadPago.Cuotas
            ? EstadoPago.EnCuotas
            : EstadoPago.Completado;

        await _db.SaveChangesAsync();

        // Activar inscripción + crear/asociar usuario Identity con rol Alumno.
        await _inscripciones.ActivarPostPagoAsync(inscripcion.Id, session.CustomerId);

        // Email de confirmación + reset password para que defina su contraseña propia.
        if (plan is not null)
        {
            await _email.EnviarConfirmacionPagoAsync(
                inscripcion.NombreCompleto, inscripcion.Email,
                plan.Nombre, pago.Monto, pago.Moneda);

            await EnviarLinkResetPasswordAsync(inscripcion.Email);
        }
    }

    private async Task ManejarInvoicePaidAsync(Event evento)
    {
        var invoice = evento.Data.Object as Invoice;
        if (invoice is null || string.IsNullOrEmpty(invoice.SubscriptionId)) return;

        // Buscamos un Pago previo con este SubscriptionId para ubicar la inscripción.
        var pagoExistente = await _db.Pagos
            .Include(p => p.Inscripcion).ThenInclude(i => i.PlanCurso)
            .FirstOrDefaultAsync(p => p.StripeSubscriptionId == invoice.SubscriptionId);

        if (pagoExistente is null) return;

        // Contamos cuántas cuotas ya hay registradas para esta inscripción.
        var nroCuota = await _db.Pagos
            .Where(p => p.InscripcionId == pagoExistente.InscripcionId
                     && p.StripeSubscriptionId == invoice.SubscriptionId
                     && p.Estado == EstadoPago.Completado)
            .CountAsync() + 1;

        // Solo creamos un nuevo registro de Pago si esta invoice no fue ya registrada.
        if (await _db.Pagos.AnyAsync(p => p.StripeInvoiceId == invoice.Id))
            return;

        var nuevoPago = new Pago
        {
            InscripcionId = pagoExistente.InscripcionId,
            Monto = (invoice.AmountPaid) / 100m,
            Moneda = invoice.Currency ?? "usd",
            Estado = EstadoPago.Completado,
            StripeInvoiceId = invoice.Id,
            StripeSubscriptionId = invoice.SubscriptionId,
            StripeCustomerId = invoice.CustomerId,
            StripePaymentIntentId = invoice.PaymentIntentId,
            FechaPago = DateTime.UtcNow,
            NumeroCuota = nroCuota,
            TotalCuotas = pagoExistente.TotalCuotas
        };
        _db.Pagos.Add(nuevoPago);
        await _db.SaveChangesAsync();

        // En la primera cuota activamos la inscripción si todavía no está activa.
        if (nroCuota == 1 && pagoExistente.Inscripcion.Estado != EstadoInscripcion.Activa)
        {
            await _inscripciones.ActivarPostPagoAsync(pagoExistente.InscripcionId, invoice.CustomerId);
            if (pagoExistente.Inscripcion?.PlanCurso is not null)
            {
                await _email.EnviarConfirmacionPagoAsync(
                    pagoExistente.Inscripcion.NombreCompleto,
                    pagoExistente.Inscripcion.Email,
                    pagoExistente.Inscripcion.PlanCurso.Nombre,
                    nuevoPago.Monto, nuevoPago.Moneda);
                await EnviarLinkResetPasswordAsync(pagoExistente.Inscripcion.Email);
            }
        }
    }

    private async Task ManejarInvoicePaymentFailedAsync(Event evento)
    {
        var invoice = evento.Data.Object as Invoice;
        if (invoice is null) return;

        _logger.LogWarning("Invoice fallida: {Id} sub={Sub}", invoice.Id, invoice.SubscriptionId);

        var pagoExistente = await _db.Pagos
            .FirstOrDefaultAsync(p => p.StripeSubscriptionId == invoice.SubscriptionId);
        if (pagoExistente is null) return;

        var pagoFallo = new Pago
        {
            InscripcionId = pagoExistente.InscripcionId,
            Monto = (invoice.AmountDue) / 100m,
            Moneda = invoice.Currency ?? "usd",
            Estado = EstadoPago.Fallido,
            StripeInvoiceId = invoice.Id,
            StripeSubscriptionId = invoice.SubscriptionId,
            StripeCustomerId = invoice.CustomerId,
            FechaPago = DateTime.UtcNow,
            MensajeError = "invoice.payment_failed",
            TotalCuotas = pagoExistente.TotalCuotas
        };
        _db.Pagos.Add(pagoFallo);
        await _db.SaveChangesAsync();

        // Notificar al admin para hacer seguimiento.
        await _email.EnviarAsync(
            "prepdiplomaciauy@gmail.com",
            $"[Prep Diplomacia] Cuota fallida — Inscripción #{pagoExistente.InscripcionId}",
            $"<p>Falló el cobro de una cuota.</p><p>Subscription: {invoice.SubscriptionId}</p><p>Invoice: {invoice.Id}</p>");
    }

    private async Task EnviarLinkResetPasswordAsync(string email)
    {
        var usuario = await _userManager.FindByEmailAsync(email);
        if (usuario is null) return;

        var token = await _userManager.GeneratePasswordResetTokenAsync(usuario);
        var enc = System.Net.WebUtility.UrlEncode(token);
        var siteUrl = string.IsNullOrWhiteSpace(_stripeOpt.SiteUrl)
            ? $"{Request.Scheme}://{Request.Host}"
            : _stripeOpt.SiteUrl.TrimEnd('/');
        var url = $"{siteUrl}/cuenta/reset-password?email={System.Net.WebUtility.UrlEncode(email)}&token={enc}";

        var html = $$"""
            <p>Te damos la bienvenida al área de alumnos de Prep Diplomacia.</p>
            <p>Para definir tu contraseña hacé clic en el siguiente enlace:</p>
            <p><a href="{{url}}" style="background:#F3BD2D;color:#061d30;padding:12px 22px;text-decoration:none;font-weight:700;letter-spacing:.08em;text-transform:uppercase;font-size:12px;">Definir mi contraseña</a></p>
            <p>Si no funciona el botón, copiá y pegá esta URL en tu navegador:<br>{{url}}</p>
        """;
        await _email.EnviarAsync(email, "Definí tu contraseña — Prep Diplomacia", html);
    }
}
