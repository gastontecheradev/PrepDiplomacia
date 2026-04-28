using System.ComponentModel.DataAnnotations;
using PrepDiplomacia.Domain.Common;

namespace PrepDiplomacia.Domain.Entities;

/// <summary>
/// Suscriptor del newsletter. Entidad sin login asociada al rol "Suscriptor".
/// Se sincroniza con Mailchimp; aquí guardamos copia local para auditoría.
/// </summary>
public class SuscriptorNewsletter : EntidadBase
{
    [Required, EmailAddress, MaxLength(180)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? Nombre { get; set; }

    /// <summary>True si la suscripción fue confirmada (double opt-in vía Mailchimp).</summary>
    public bool Confirmado { get; set; } = false;

    /// <summary>True si el suscriptor pidió darse de baja.</summary>
    public bool DadoDeBaja { get; set; } = false;

    public DateTime? FechaConfirmacion { get; set; }
    public DateTime? FechaBaja { get; set; }

    /// <summary>ID retornado por Mailchimp tras la sincronización (para evitar duplicados).</summary>
    [MaxLength(50)]
    public string? MailchimpId { get; set; }

    /// <summary>Origen de la suscripción ("home_cta", "footer", "modal_inscripcion", etc.).</summary>
    [MaxLength(80)]
    public string? Origen { get; set; }

    /// <summary>IP del que se suscribió, para auditoría legal.</summary>
    [MaxLength(45)]
    public string? IpSuscripcion { get; set; }
}
