using System.ComponentModel.DataAnnotations;
using PrepDiplomacia.Domain.Entities;
using PrepDiplomacia.Domain.Enums;

namespace PrepDiplomacia.Web.Models.ViewModels;

// ── Inscripción ─────────────────────────────────────────────────────────────
public class InscripcionViewModel
{
    [Required(ErrorMessage = "Ingresá tu nombre completo.")]
    [StringLength(120)]
    [Display(Name = "Nombre completo")]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresá tu correo.")]
    [EmailAddress(ErrorMessage = "El correo no es válido.")]
    [StringLength(180)]
    [Display(Name = "Correo electrónico")]
    public string Email { get; set; } = string.Empty;

    [Phone, StringLength(50)]
    [Display(Name = "Teléfono (opcional)")]
    public string? Telefono { get; set; }

    [StringLength(200)]
    [Display(Name = "Formación académica")]
    public string? FormacionAcademica { get; set; }

    [StringLength(2000)]
    [Display(Name = "Consulta o comentario")]
    public string? Consulta { get; set; }

    [Required]
    [Display(Name = "Plan elegido")]
    public int PlanCursoId { get; set; }

    [Display(Name = "Modalidad de pago")]
    public ModalidadPago Modalidad { get; set; } = ModalidadPago.PagoUnico;

    [Display(Name = "Acepto los Términos y Condiciones y la Política de Privacidad.")]
    [Range(typeof(bool), "true", "true", ErrorMessage = "Debés aceptar los términos para continuar.")]
    public bool AceptaTerminos { get; set; }

    public List<PlanCurso> PlanesDisponibles { get; set; } = new();
}

// ── Contacto ────────────────────────────────────────────────────────────────
public class ContactoViewModel
{
    [Required, StringLength(120)] public string Nombre { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(180)] public string Email { get; set; } = string.Empty;
    [Phone, StringLength(50)] public string? Telefono { get; set; }
    [StringLength(150)] public string? Asunto { get; set; }
    [Required, StringLength(4000)] public string Mensaje { get; set; } = string.Empty;
    public bool AceptaTerminos { get; set; }
}

// ── Newsletter ──────────────────────────────────────────────────────────────
public class NewsletterViewModel
{
    [Required, EmailAddress, StringLength(180)] public string Email { get; set; } = string.Empty;
    [StringLength(120)] public string? Nombre { get; set; }
    public string? Origen { get; set; }
}

// ── Comentario ──────────────────────────────────────────────────────────────
public class ComentarioViewModel
{
    [Required] public int PostBlogId { get; set; }
    [Required, StringLength(120)] public string NombreAutor { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(180)] public string EmailAutor { get; set; } = string.Empty;
    [Url, StringLength(200)] public string? SitioWeb { get; set; }
    [Required, StringLength(2000)] public string Contenido { get; set; } = string.Empty;
}

// ── Blog: paginado ──────────────────────────────────────────────────────────
public class BlogPaginadoViewModel
{
    public List<PostBlog> Posts { get; set; } = new();
    public int Pagina { get; set; } = 1;
    public int TotalPaginas { get; set; } = 1;
    public int Total { get; set; }
    public string? Busqueda { get; set; }
    public int? CategoriaId { get; set; }
    public int? TagId { get; set; }
    public List<CategoriaBlog> Categorias { get; set; } = new();
    public List<TagBlog> Tags { get; set; } = new();
}
