namespace PrepDiplomacia.Domain.Enums;

/// <summary>
/// Distingue las publicaciones del Blog de las de Noticias.
///
/// Ambas comparten la misma estructura (título, imagen, cuerpo HTML, estado,
/// SEO), por eso viven en la misma tabla y reutilizan el mismo editor del admin.
/// El tipo determina en qué sección pública aparecen: /blog o /noticias.
/// </summary>
public enum TipoPublicacion
{
    /// <summary>Artículo del blog. Aparece en /blog.</summary>
    Articulo = 0,

    /// <summary>Noticia. Aparece en /noticias.</summary>
    Noticia = 1
}
