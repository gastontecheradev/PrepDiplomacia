using System.ComponentModel.DataAnnotations;
using PrepDiplomacia.Domain.Common;

namespace PrepDiplomacia.Domain.Entities;

/// <summary>
/// Registro de eventos de Stripe ya procesados, para garantizar idempotencia.
/// Stripe puede reenviar el mismo webhook múltiples veces; aquí evitamos
/// procesar dos veces el mismo evento.
/// </summary>
public class EventoStripeProcesado : EntidadBase
{
    /// <summary>Stripe Event ID (evt_xxx). Único — sirve de protección contra duplicados.</summary>
    [Required, MaxLength(120)]
    public string EventoId { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string TipoEvento { get; set; } = string.Empty;

    public DateTime FechaProcesamiento { get; set; } = DateTime.UtcNow;

    /// <summary>Resumen breve del resultado del procesamiento (para auditoría).</summary>
    [MaxLength(500)]
    public string? Resultado { get; set; }
}
