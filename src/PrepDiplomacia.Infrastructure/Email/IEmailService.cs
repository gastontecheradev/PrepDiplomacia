namespace PrepDiplomacia.Infrastructure.Email;

public interface IEmailService
{
    /// <summary>Envía un email HTML simple a un destinatario.</summary>
    Task<bool> EnviarAsync(string destinatario, string asunto, string cuerpoHtml,
                           string? replyTo = null, CancellationToken ct = default);

    /// <summary>
    /// Envía un email a Carolina con los datos de un formulario de contacto del sitio.
    /// </summary>
    Task<bool> EnviarFormularioContactoAsync(
        string nombre, string emailRemitente, string? telefono,
        string? asunto, string mensaje, string origen,
        CancellationToken ct = default);

    /// <summary>Confirmación al inscripto + notificación al admin.</summary>
    Task<bool> EnviarNotificacionInscripcionAsync(
        string nombreInscripto, string emailInscripto, string nombrePlan,
        CancellationToken ct = default);

    /// <summary>Notificación al alumno de pago confirmado y acceso habilitado.</summary>
    Task<bool> EnviarConfirmacionPagoAsync(
        string nombreAlumno, string emailAlumno, string nombrePlan,
        decimal monto, string moneda, CancellationToken ct = default);
}
