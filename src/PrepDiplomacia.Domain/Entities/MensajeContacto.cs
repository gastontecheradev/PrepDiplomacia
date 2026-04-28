using System.ComponentModel.DataAnnotations;
using PrepDiplomacia.Domain.Common;

namespace PrepDiplomacia.Domain.Entities;

/// <summary>
/// Mensaje recibido desde algún formulario de contacto.
/// Se guarda copia en BD y además se envía por email a Carolina.
/// </summary>
public class MensajeContacto : EntidadBase
{
    [Required, MaxLength(120)]
    public string Nombre { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(180)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Telefono { get; set; }

    [MaxLength(150)]
    public string? Asunto { get; set; }

    [Required, MaxLength(4000)]
    public string Mensaje { get; set; } = string.Empty;

    /// <summary>Origen del formulario ("inscripcion", "contacto_general", "modal_info").</summary>
    [MaxLength(80)]
    public string? Origen { get; set; }

    public bool Leido { get; set; } = false;
    public bool Respondido { get; set; } = false;

    /// <summary>Notas internas del admin.</summary>
    [MaxLength(2000)]
    public string? Notas { get; set; }

    [MaxLength(45)]
    public string? IpRemitente { get; set; }
}
