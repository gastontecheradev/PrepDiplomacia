using System.ComponentModel.DataAnnotations;
using PrepDiplomacia.Domain.Common;

namespace PrepDiplomacia.Domain.Entities;

/// <summary>
/// Comentarios sobre posts del blog. Por defecto requieren moderación
/// (Aprobado=false). El admin los aprueba desde el panel.
/// </summary>
public class ComentarioBlog : EntidadBase
{
    public int PostBlogId { get; set; }
    public PostBlog PostBlog { get; set; } = null!;

    [Required, MaxLength(120)]
    public string NombreAutor { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(180)]
    public string EmailAutor { get; set; } = string.Empty;

    /// <summary>Sitio web opcional del comentarista.</summary>
    [MaxLength(200)]
    public string? SitioWeb { get; set; }

    [Required, MaxLength(2000)]
    public string Contenido { get; set; } = string.Empty;

    /// <summary>True una vez aprobado por Carolina. Default false.</summary>
    public bool Aprobado { get; set; } = false;

    /// <summary>IP de quien comentó (para anti-spam y moderación).</summary>
    [MaxLength(45)]
    public string? IpRemitente { get; set; }

    /// <summary>Para hilos de respuesta (opcional).</summary>
    public int? ComentarioPadreId { get; set; }
    public ComentarioBlog? ComentarioPadre { get; set; }
    public ICollection<ComentarioBlog> Respuestas { get; set; } = new List<ComentarioBlog>();
}
