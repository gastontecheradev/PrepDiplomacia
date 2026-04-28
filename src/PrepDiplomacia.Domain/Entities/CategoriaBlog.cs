using System.ComponentModel.DataAnnotations;
using PrepDiplomacia.Domain.Common;

namespace PrepDiplomacia.Domain.Entities;

public class CategoriaBlog : EntidadBase
{
    [Required, MaxLength(80)]
    public string Nombre { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Descripcion { get; set; }

    /// <summary>Color de acento HEX para badges (ej. "#0C3F67").</summary>
    [MaxLength(7)]
    public string? Color { get; set; }

    public ICollection<PostBlog> Posts { get; set; } = new List<PostBlog>();
}
