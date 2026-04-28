using System.ComponentModel.DataAnnotations;
using PrepDiplomacia.Domain.Common;
using PrepDiplomacia.Domain.Enums;

namespace PrepDiplomacia.Domain.Entities;

/// <summary>
/// Plan / paquete del curso ofrecido (Básico, Completo, etc.).
/// Cada plan puede ofrecerse en pago único y/o cuotas, y mapea a un Stripe Price.
/// </summary>
public class PlanCurso : EntidadBase
{
    [Required, MaxLength(120)]
    public string Nombre { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Codigo { get; set; } = string.Empty; // ej: "BASICO_2027"

    [MaxLength(500)]
    public string? Descripcion { get; set; }

    /// <summary>Precio total del plan en USD (o moneda configurada).</summary>
    public decimal PrecioTotal { get; set; }

    /// <summary>Moneda ISO 4217 — "usd", "uyu". Configurable.</summary>
    [MaxLength(3)]
    public string Moneda { get; set; } = "usd";

    public ModalidadPago ModalidadPago { get; set; } = ModalidadPago.PagoUnico;

    /// <summary>Cantidad de cuotas si ModalidadPago = Cuotas. Null o 1 si es pago único.</summary>
    public int? CantidadCuotas { get; set; }

    /// <summary>Stripe Price ID para pago único (price_xxx).</summary>
    [MaxLength(100)]
    public string? StripePriceIdUnico { get; set; }

    /// <summary>Stripe Price ID para suscripción mensual (price_xxx) — si modalidad = Cuotas.</summary>
    [MaxLength(100)]
    public string? StripePriceIdCuotas { get; set; }

    /// <summary>Características del plan, una por línea, para mostrar en la página.</summary>
    [MaxLength(2000)]
    public string? Caracteristicas { get; set; }

    public bool Activo { get; set; } = true;

    /// <summary>Orden de visualización en la página de inscripción.</summary>
    public int Orden { get; set; } = 0;

    /// <summary>Plan destacado (se renderiza con highlight).</summary>
    public bool Destacado { get; set; } = false;

    public ICollection<Inscripcion> Inscripciones { get; set; } = new List<Inscripcion>();
}
