using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrepDiplomacia.Domain.Entities;
using Stripe;
using Stripe.Checkout;

namespace PrepDiplomacia.Infrastructure.Payments;

/// <summary>
/// Servicio de integración con Stripe.
///
/// Estrategia:
/// - Pago único: usamos Checkout Session en modo "payment" con un Price configurado.
/// - Cuotas: usamos Checkout Session en modo "subscription" con un Price recurrente
///   y "subscription_data.cancel_at" calculado para cortar tras N cuotas.
/// - Si el plan no tiene un Price preconfigurado en Stripe, creamos un PriceData inline
///   (price_data) usando los datos del plan en BD. Esto facilita el desarrollo inicial.
/// </summary>
public class StripeService : IStripeService
{
    private readonly StripeOptions _opt;
    private readonly ILogger<StripeService> _logger;

    public StripeService(IOptions<StripeOptions> opt, ILogger<StripeService> logger)
    {
        _opt = opt.Value;
        _logger = logger;

        if (!string.IsNullOrWhiteSpace(_opt.SecretKey))
            StripeConfiguration.ApiKey = _opt.SecretKey;
    }

    public async Task<ResultadoCheckout> CrearSesionPagoUnicoAsync(
        Inscripcion inscripcion, PlanCurso plan,
        string successUrl, string cancelUrl,
        CancellationToken ct = default)
    {
        try
        {
            var lineItems = new List<SessionLineItemOptions>();

            if (!string.IsNullOrWhiteSpace(plan.StripePriceIdUnico))
            {
                lineItems.Add(new SessionLineItemOptions
                {
                    Price = plan.StripePriceIdUnico,
                    Quantity = 1
                });
            }
            else
            {
                // Sin Price preconfigurado: lo creamos inline desde los datos del plan.
                lineItems.Add(new SessionLineItemOptions
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = plan.Moneda,
                        UnitAmount = (long)(plan.PrecioTotal * 100m), // Stripe trabaja en centavos
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = plan.Nombre,
                            Description = plan.Descripcion
                        }
                    }
                });
            }

            var opciones = new SessionCreateOptions
            {
                Mode = "payment",
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = lineItems,
                CustomerEmail = inscripcion.Email,
                ClientReferenceId = inscripcion.Id.ToString(),
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                Metadata = new Dictionary<string, string>
                {
                    ["inscripcion_id"] = inscripcion.Id.ToString(),
                    ["plan_id"]        = plan.Id.ToString(),
                    ["plan_codigo"]    = plan.Codigo
                }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(opciones, cancellationToken: ct);

            return new ResultadoCheckout(true, session.Url, session.Id, null);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Error creando Checkout Session de pago único para inscripción {Id}", inscripcion.Id);
            return new ResultadoCheckout(false, null, null, ex.Message);
        }
    }

    public async Task<ResultadoCheckout> CrearSesionCuotasAsync(
        Inscripcion inscripcion, PlanCurso plan,
        string successUrl, string cancelUrl,
        CancellationToken ct = default)
    {
        try
        {
            var totalCuotas = plan.CantidadCuotas ?? 1;

            var lineItems = new List<SessionLineItemOptions>();

            if (!string.IsNullOrWhiteSpace(plan.StripePriceIdCuotas))
            {
                lineItems.Add(new SessionLineItemOptions
                {
                    Price = plan.StripePriceIdCuotas,
                    Quantity = 1
                });
            }
            else
            {
                // Calcular monto por cuota mensual.
                var montoPorCuota = plan.PrecioTotal / totalCuotas;
                lineItems.Add(new SessionLineItemOptions
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = plan.Moneda,
                        UnitAmount = (long)(montoPorCuota * 100m),
                        Recurring = new SessionLineItemPriceDataRecurringOptions
                        {
                            Interval = "month",
                            IntervalCount = 1
                        },
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"{plan.Nombre} ({totalCuotas} cuotas)",
                            Description = plan.Descripcion
                        }
                    }
                });
            }

            var opciones = new SessionCreateOptions
            {
                Mode = "subscription",
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = lineItems,
                CustomerEmail = inscripcion.Email,
                ClientReferenceId = inscripcion.Id.ToString(),
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                Metadata = new Dictionary<string, string>
                {
                    ["inscripcion_id"] = inscripcion.Id.ToString(),
                    ["plan_id"]        = plan.Id.ToString(),
                    ["plan_codigo"]    = plan.Codigo,
                    ["total_cuotas"]   = totalCuotas.ToString()
                },
                SubscriptionData = new SessionSubscriptionDataOptions
                {
                    // El corte automático tras N cuotas se gestiona desde el webhook
                    // (ver PagoController.ManejarInvoicePaidAsync) llamando a
                    // SubscriptionService.CancelAsync cuando se alcanza la última cuota.
                    Metadata = new Dictionary<string, string>
                    {
                        ["inscripcion_id"] = inscripcion.Id.ToString(),
                        ["plan_id"] = plan.Id.ToString(),
                        ["total_cuotas"] = totalCuotas.ToString()
                    }
                }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(opciones, cancellationToken: ct);

            return new ResultadoCheckout(true, session.Url, session.Id, null);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Error creando Checkout Session de cuotas para inscripción {Id}", inscripcion.Id);
            return new ResultadoCheckout(false, null, null, ex.Message);
        }
    }

    public Event ConstruirEventoDesdeWebhook(string payloadJson, string firmaStripe)
    {
        // Verifica la firma con el webhook secret. Si falla, lanza StripeException.
        return EventUtility.ConstructEvent(payloadJson, firmaStripe, _opt.WebhookSecret);
    }
}
