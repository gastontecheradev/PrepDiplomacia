using System.ComponentModel.DataAnnotations;
using PrepDiplomacia.Domain.Common;

namespace PrepDiplomacia.Domain.Entities;

/// <summary>
/// Bloque de contenido editable del sitio público.
/// Cada bloque tiene una Clave única (ej: "home.hero.titulo") que las vistas Razor
/// usan para renderizar el contenido editado por Carolina desde el admin.
///
/// Si una clave no existe en BD, las vistas usan un valor por defecto (fallback).
/// Esto permite que el sitio funcione incluso sin haber poblado la tabla.
/// </summary>
public class BloqueContenido : EntidadBase
{
    /// <summary>Clave única identificadora (ej. "home.hero.titulo").</summary>
    [Required, MaxLength(120)]
    public string Clave { get; set; } = string.Empty;

    /// <summary>Etiqueta legible mostrada en el panel admin.</summary>
    [Required, MaxLength(200)]
    public string Etiqueta { get; set; } = string.Empty;

    /// <summary>Sección del sitio (ej. "Inicio", "Programa", "Sobre Prep").</summary>
    [MaxLength(80)]
    public string? Seccion { get; set; }

    /// <summary>Texto / HTML editable.</summary>
    [Required]
    public string Valor { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de editor: "texto" (input simple), "parrafo" (textarea),
    /// "html" (editor enriquecido). Permite renderizar la UI correcta en admin.
    /// </summary>
    [MaxLength(20)]
    public string Tipo { get; set; } = "texto";

    /// <summary>Orden de aparición dentro de la sección en el panel admin.</summary>
    public int Orden { get; set; } = 0;

    /// <summary>Nota interna para Carolina sobre dónde aparece este bloque.</summary>
    [MaxLength(300)]
    public string? Ayuda { get; set; }
}
