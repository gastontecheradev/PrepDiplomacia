using System.ComponentModel.DataAnnotations;
using PrepDiplomacia.Domain.Common;
using PrepDiplomacia.Domain.Enums;

namespace PrepDiplomacia.Domain.Entities;

/// <summary>
/// Pago / transacción asociado a una inscripción.
/// Cada inscripción puede tener uno (pago único) o varios (cuotas) pagos.
/// </summary>
public class Pago : EntidadBase
{
    public int InscripcionId { get; set; }
    public Inscripcion Inscripcion { get; set; } = null!;

    public decimal Monto { get; set; }

    [MaxLength(3)]
    public string Moneda { get; set; } = "usd";

    public EstadoPago Estado { get; set; } = EstadoPago.Pendiente;

    /// <summary>Stripe Checkout Session ID (cs_xxx).</summary>
    [MaxLength(120)]
    public string? StripeSessionId { get; set; }

    /// <summary>Stripe PaymentIntent ID (pi_xxx).</summary>
    [MaxLength(120)]
    public string? StripePaymentIntentId { get; set; }

    /// <summary>Stripe Subscription ID (sub_xxx) si el pago es en cuotas.</summary>
    [MaxLength(120)]
    public string? StripeSubscriptionId { get; set; }

    /// <summary>Stripe Customer ID (cus_xxx) para reusar en pagos futuros.</summary>
    [MaxLength(120)]
    public string? StripeCustomerId { get; set; }

    /// <summary>Stripe Invoice ID si aplica.</summary>
    [MaxLength(120)]
    public string? StripeInvoiceId { get; set; }

    public DateTime? FechaPago { get; set; }

    /// <summary>Mensaje de error si el pago falló.</summary>
    [MaxLength(500)]
    public string? MensajeError { get; set; }

    /// <summary>Si fue cuota, número de cuota (1, 2, 3...).</summary>
    public int? NumeroCuota { get; set; }

    /// <summary>Si fue cuota, total de cuotas del plan.</summary>
    public int? TotalCuotas { get; set; }
}
