namespace PrepDiplomacia.Domain.Entities;

/// <summary>
/// Tabla intermedia para la relación M:N entre Posts y Tags.
/// Configurada en BlogConfigurations con clave compuesta.
/// </summary>
public class PostBlogTag
{
    public int PostBlogId { get; set; }
    public PostBlog PostBlog { get; set; } = null!;

    public int TagBlogId { get; set; }
    public TagBlog TagBlog { get; set; } = null!;
}
