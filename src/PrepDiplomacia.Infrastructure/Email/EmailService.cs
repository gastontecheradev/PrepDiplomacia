using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace PrepDiplomacia.Infrastructure.Email;

/// <summary>
/// Implementación de envío de email vía SMTP con MailKit.
/// Configurada para Gmail SMTP con App Password (puerto 587 STARTTLS).
///
/// Para Gmail:
///   1. Activar 2FA en la cuenta.
///   2. Generar App Password en https://myaccount.google.com/apppasswords
///   3. Pegarla en Email:Smtp:Password (user-secrets / variables de entorno).
/// </summary>
public class EmailService : IEmailService
{
    private readonly EmailOptions _opt;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailOptions> opt, ILogger<EmailService> logger)
    {
        _opt = opt.Value;
        _logger = logger;
    }

    public async Task<bool> EnviarAsync(string destinatario, string asunto, string cuerpoHtml,
                                        string? replyTo = null, CancellationToken ct = default)
    {
        try
        {
            var mensaje = new MimeMessage();
            mensaje.From.Add(new MailboxAddress(_opt.FromNombre, _opt.FromEmail));
            mensaje.To.Add(MailboxAddress.Parse(destinatario));
            mensaje.Subject = asunto;

            if (!string.IsNullOrWhiteSpace(replyTo))
                mensaje.ReplyTo.Add(MailboxAddress.Parse(replyTo));

            mensaje.Body = new BodyBuilder { HtmlBody = cuerpoHtml }.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(
                _opt.Smtp.Host,
                _opt.Smtp.Port,
                _opt.Smtp.UsarStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.SslOnConnect,
                ct);
            await client.AuthenticateAsync(_opt.Smtp.Usuario, _opt.Smtp.Password, ct);
            await client.SendAsync(mensaje, ct);
            await client.DisconnectAsync(true, ct);

            _logger.LogInformation("Email enviado a {Destinatario} — {Asunto}", destinatario, asunto);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar email a {Destinatario}", destinatario);
            return false;
        }
    }

    public Task<bool> EnviarFormularioContactoAsync(
        string nombre, string emailRemitente, string? telefono,
        string? asunto, string mensaje, string origen,
        CancellationToken ct = default)
    {
        var asuntoFinal = $"[Prep Diplomacia] Nuevo mensaje desde {origen}";
        var html = $$"""
            <div style="font-family: Arial, sans-serif; color: #2C3E50; max-width: 600px; margin: auto;">
                <div style="background: #0C3F67; padding: 20px; color: #fff;">
                    <h2 style="margin: 0; font-size: 18px;">Nuevo mensaje desde el sitio</h2>
                </div>
                <div style="padding: 24px; background: #f5f8fa;">
                    <table style="width: 100%; border-collapse: collapse;">
                        <tr><td style="padding: 6px 0; width: 130px;"><strong>Origen:</strong></td><td>{{origen}}</td></tr>
                        <tr><td style="padding: 6px 0;"><strong>Nombre:</strong></td><td>{{nombre}}</td></tr>
                        <tr><td style="padding: 6px 0;"><strong>Email:</strong></td><td>{{emailRemitente}}</td></tr>
                        <tr><td style="padding: 6px 0;"><strong>Teléfono:</strong></td><td>{{telefono ?? "—"}}</td></tr>
                        <tr><td style="padding: 6px 0;"><strong>Asunto:</strong></td><td>{{asunto ?? "—"}}</td></tr>
                    </table>
                    <hr style="margin: 18px 0; border: 0; border-top: 1px solid #e2e8ef;">
                    <p style="white-space: pre-wrap; line-height: 1.6;">{{System.Net.WebUtility.HtmlEncode(mensaje)}}</p>
                </div>
                <div style="padding: 12px; text-align: center; font-size: 12px; color: #888;">
                    Este mensaje fue enviado automáticamente desde el sitio de Prep Diplomacia.
                </div>
            </div>
        """;

        return EnviarAsync(_opt.DestinoFormularios, asuntoFinal, html, replyTo: emailRemitente, ct: ct);
    }

    public async Task<bool> EnviarNotificacionInscripcionAsync(
        string nombreInscripto, string emailInscripto, string nombrePlan,
        CancellationToken ct = default)
    {
        // 1. Confirmación al usuario.
        var htmlUsuario = $$"""
            <div style="font-family: Arial, sans-serif; color: #2C3E50; max-width: 600px; margin: auto;">
                <div style="background: #0C3F67; padding: 30px 20px; color: #fff; text-align: center;">
                    <h2 style="margin: 0;">¡Recibimos tu inscripción!</h2>
                </div>
                <div style="padding: 28px 24px;">
                    <p>Hola {{nombreInscripto}},</p>
                    <p>Recibimos tu inscripción al <strong>{{nombrePlan}}</strong> de Prep Diplomacia.</p>
                    <p>En breve te llegará un segundo correo con los pasos siguientes para concretar el pago y activar tu acceso.</p>
                    <p>Si tenés cualquier consulta podés responder a este correo o escribirnos a <a href="mailto:prepdiplomaciauy@gmail.com" style="color: #0C3F67;">prepdiplomaciauy@gmail.com</a>.</p>
                    <p style="margin-top: 30px;">Cordialmente,<br><strong>Carolina Techera</strong><br>Prep Diplomacia</p>
                </div>
            </div>
        """;
        var ok1 = await EnviarAsync(emailInscripto, "Recibimos tu inscripción — Prep Diplomacia", htmlUsuario, ct: ct);

        // 2. Notificación al admin.
        var htmlAdmin = $$"""
            <p>Nueva inscripción al programa:</p>
            <ul>
                <li><strong>Nombre:</strong> {{nombreInscripto}}</li>
                <li><strong>Email:</strong> {{emailInscripto}}</li>
                <li><strong>Plan:</strong> {{nombrePlan}}</li>
            </ul>
            <p>Verificá el panel admin para más detalles.</p>
        """;
        var ok2 = await EnviarAsync(_opt.DestinoFormularios,
            $"[Prep Diplomacia] Nueva inscripción: {nombreInscripto}", htmlAdmin, ct: ct);

        return ok1 && ok2;
    }

    public Task<bool> EnviarConfirmacionPagoAsync(
        string nombreAlumno, string emailAlumno, string nombrePlan,
        decimal monto, string moneda, CancellationToken ct = default)
    {
        var html = $$"""
            <div style="font-family: Arial, sans-serif; color: #2C3E50; max-width: 600px; margin: auto;">
                <div style="background: #0C3F67; padding: 30px 20px; color: #fff; text-align: center;">
                    <h2 style="margin: 0;">¡Pago confirmado!</h2>
                </div>
                <div style="padding: 28px 24px;">
                    <p>Hola {{nombreAlumno}},</p>
                    <p>Confirmamos tu pago por <strong>{{monto:N2}} {{moneda.ToUpper()}}</strong> correspondiente al <strong>{{nombrePlan}}</strong>.</p>
                    <p>Tu acceso al área privada de alumnos quedó habilitado. Podés ingresar con el correo y la contraseña que registraste.</p>
                    <p style="text-align: center; margin: 30px 0;">
                        <a href="https://prepdiplomacia.uy/cuenta/login" style="background: #F3BD2D; color: #061d30; padding: 12px 28px; text-decoration: none; font-weight: 700; letter-spacing: 0.1em; text-transform: uppercase; font-size: 12px;">Ingresar al área de alumnos</a>
                    </p>
                    <p>Cualquier consulta, escribinos a <a href="mailto:prepdiplomaciauy@gmail.com" style="color: #0C3F67;">prepdiplomaciauy@gmail.com</a>.</p>
                    <p style="margin-top: 30px;">Bienvenido al programa.<br><strong>Carolina Techera</strong><br>Prep Diplomacia</p>
                </div>
            </div>
        """;
        return EnviarAsync(emailAlumno, "Pago confirmado — Acceso habilitado", html, ct: ct);
    }
}
