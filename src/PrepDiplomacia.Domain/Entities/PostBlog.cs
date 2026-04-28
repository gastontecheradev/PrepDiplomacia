using System.ComponentModel.DataAnnotations;
using PrepDiplomacia.Domain.Common;
using PrepDiplomacia.Domain.Enums;

namespace PrepDiplomacia.Domain.Entities;

/// <summary>
/// Publicación del blog. Soporta cuerpo HTML enriquecido,
/// imagen destacada subida al servidor, y video de YouTube embebido.
/// </summary>
public class PostBlog : EntidadBase
{
    [Required, MaxLength(200)]
    public string Titulo { get; set; } = string.Empty;

    /// <summary>Slug URL-friendly único. Se genera automáticamente desde el título.</summary>
    [Required, MaxLength(220)]
    public string Slug { get; set; } = string.Empty;

    /// <summary>Resumen breve para listados y meta description.</summary>
    [MaxLength(500)]
    public string Resumen { get; set; } = string.Empty;

    /// <summary>HTML enriquecido del cuerpo del post.</summary>
    [Required]
    public string Contenido { get; set; } = string.Empty;

    /// <summary>Path relativo a la imagen destacada bajo /uploads/blog/.</summary>
    [MaxLength(300)]
    public string? ImagenDestacada { get; set; }

    /// <summary>Texto alternativo de la imagen destacada (accesibilidad / SEO).</summary>
    [MaxLength(200)]
    public string? ImagenAlt { get; set; }

    /// <summary>ID de YouTube (la parte después de v=). Se renderiza embebido.</summary>
    [MaxLength(50)]
    public string? YouTubeVideoId { get; set; }

    public EstadoPublicacion Estado { get; set; } = EstadoPublicacion.Borrador;

    public DateTime? FechaPublicacion { get; set; }

    /// <summary>Cantidad de vistas — incrementada al abrir el post.</summary>
    public int Vistas { get; set; } = 0;

    public bool ComentariosHabilitados { get; set; } = true;

    /// <summary>SEO: meta title (opcional, fallback al título).</summary>
    [MaxLength(160)]
    public string? MetaTitulo { get; set; }

    /// <summary>SEO: meta description (opcional, fallback al resumen).</summary>
    [MaxLength(300)]
    public string? MetaDescripcion { get; set; }

    /// <summary>FK al autor (Identity user). Por ahora siempre Carolina.</summary>
    [Required, MaxLength(450)]
    public string AutorId { get; set; } = string.Empty;

    /// <summary>Categoría principal del post (1:N). Opcional.</summary>
    public int? CategoriaId { get; set; }
    public CategoriaBlog? Categoria { get; set; }

    /// <summary>Tags muchos a muchos.</summary>
    public ICollection<PostBlogTag> PostTags { get; set; } = new List<PostBlogTag>();

    public ICollection<ComentarioBlog> Comentarios { get; set; } = new List<ComentarioBlog>();
}
