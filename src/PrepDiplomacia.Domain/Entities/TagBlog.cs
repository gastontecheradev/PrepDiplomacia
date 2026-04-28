using System.ComponentModel.DataAnnotations;
using PrepDiplomacia.Domain.Common;

namespace PrepDiplomacia.Domain.Entities;

public class TagBlog : EntidadBase
{
    [Required, MaxLength(60)]
    public string Nombre { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string Slug { get; set; } = string.Empty;

    public ICollection<PostBlogTag> PostTags { get; set; } = new List<PostBlogTag>();
}
