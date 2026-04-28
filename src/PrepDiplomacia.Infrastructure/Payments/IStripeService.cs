using PrepDiplomacia.Domain.Entities;

namespace PrepDiplomacia.Infrastructure.Payments;

public interface IStripeService
{
    /// <summary>
    /// Crea una sesión de Stripe Checkout para un pago único.
    /// Retorna la URL a la que se debe redirigir al usuario.
    /// </summary>
    Task<ResultadoCheckout> CrearSesionPagoUnicoAsync(
        Inscripcion inscripcion, PlanCurso plan,
        string successUrl, string cancelUrl,
        CancellationToken ct = default);

    /// <summary>
    /// Crea una sesión de Stripe Checkout para una suscripción (cuotas mensuales).
    /// </summary>
    Task<ResultadoCheckout> CrearSesionCuotasAsync(
        Inscripcion inscripcion, PlanCurso plan,
        string successUrl, string cancelUrl,
        CancellationToken ct = default);

    /// <summary>Verifica la firma del webhook y retorna el evento parseado.</summary>
    Stripe.Event ConstruirEventoDesdeWebhook(string payloadJson, string firmaStripe);
}

public record ResultadoCheckout(
    bool Exito,
    string? UrlCheckout,
    string? SessionId,
    string? Error);
