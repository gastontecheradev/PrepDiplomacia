using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using PrepDiplomacia.Domain.Entities;
using PrepDiplomacia.Domain.Enums;

namespace PrepDiplomacia.Web.Models.ViewModels;

public class DashboardViewModel
{
    public int TotalPosts { get; set; }
    public int PostsBorrador { get; set; }
    public int TotalSuscriptores { get; set; }
    public int TotalInscripciones { get; set; }
    public int InscripcionesActivas { get; set; }
    public int MensajesNoLeidos { get; set; }
    public int ComentariosPendientes { get; set; }
    public List<PostBlog> UltimosPosts { get; set; } = new();
    public List<MensajeContacto> UltimosMensajes { get; set; } = new();
}

public class PostEditarViewModel
{
    public int Id { get; set; }

    [Required, StringLength(200), Display(Name = "Título")]
    public string Titulo { get; set; } = string.Empty;

    [StringLength(500), Display(Name = "Resumen")]
    public string? Resumen { get; set; }

    [Required, Display(Name = "Contenido")]
    public string Contenido { get; set; } = string.Empty;

    [Display(Name = "Imagen destacada")]
    public IFormFile? Imagen { get; set; }
    public string? ImagenActual { get; set; }
    [StringLength(200), Display(Name = "Texto alternativo de la imagen")]
    public string? ImagenAlt { get; set; }

    [StringLength(50), Display(Name = "Video de YouTube (ID)")]
    public string? YouTubeVideoId { get; set; }

    [Display(Name = "Categoría")]
    public int? CategoriaId { get; set; }

    [Display(Name = "Tags")]
    public int[] TagsSeleccionados { get; set; } = Array.Empty<int>();

    [Display(Name = "Estado")]
    public EstadoPublicacion Estado { get; set; } = EstadoPublicacion.Borrador;

    [Display(Name = "Permitir comentarios")]
    public bool ComentariosHabilitados { get; set; } = true;

    [StringLength(160), Display(Name = "Meta título (SEO)")]
    public string? MetaTitulo { get; set; }
    [StringLength(300), Display(Name = "Meta descripción (SEO)")]
    public string? MetaDescripcion { get; set; }

    public List<CategoriaBlog> Categorias { get; set; } = new();
    public List<TagBlog> Tags { get; set; } = new();
}

public class CategoriaEditarViewModel
{
    public int Id { get; set; }
    [Required, StringLength(80), Display(Name = "Nombre")]
    public string Nombre { get; set; } = string.Empty;
    [StringLength(300), Display(Name = "Descripción")]
    public string? Descripcion { get; set; }
    [StringLength(7), Display(Name = "Color (HEX)")]
    public string? Color { get; set; }
}

public class BloqueEditarViewModel
{
    public int Id { get; set; }
    public string Clave { get; set; } = string.Empty;
    public string Etiqueta { get; set; } = string.Empty;
    public string? Seccion { get; set; }
    public string Tipo { get; set; } = "texto";
    public string? Ayuda { get; set; }

    [Required(ErrorMessage = "El valor no puede estar vacío.")]
    public string Valor { get; set; } = string.Empty;
}
