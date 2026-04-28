using System.ComponentModel.DataAnnotations;
using PrepDiplomacia.Domain.Common;
using PrepDiplomacia.Domain.Enums;

namespace PrepDiplomacia.Domain.Entities;

/// <summary>
/// Inscripción de un candidato/alumno al curso. Si el pago se completó,
/// el estado pasa a Activa y el usuario obtiene acceso al rol Alumno.
/// </summary>
public class Inscripcion : EntidadBase
{
    /// <summary>Datos del candidato (capturados al inscribirse, antes de pagar).</summary>
    [Required, MaxLength(120)]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(180)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Telefono { get; set; }

    [MaxLength(200)]
    public string? FormacionAcademica { get; set; }

    [MaxLength(2000)]
    public string? ConsultaAdicional { get; set; }

    /// <summary>FK opcional al usuario Identity. Se asigna cuando el pago se completa.</summary>
    [MaxLength(450)]
    public string? UsuarioId { get; set; }

    public int PlanCursoId { get; set; }
    public PlanCurso PlanCurso { get; set; } = null!;

    public ModalidadPago ModalidadElegida { get; set; }

    public EstadoInscripcion Estado { get; set; } = EstadoInscripcion.EnListaDeEspera;

    public DateTime? FechaActivacion { get; set; }

    /// <summary>Notas internas del admin sobre esta inscripción.</summary>
    [MaxLength(2000)]
    public string? Notas { get; set; }

    public ICollection<Pago> Pagos { get; set; } = new List<Pago>();
}
