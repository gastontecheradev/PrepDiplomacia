using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PrepDiplomacia.Infrastructure.Newsletter;

public interface INewsletterService
{
    /// <summary>
    /// Suscribe un email al audience de Mailchimp.
    /// Si Mailchimp falla, retorna false pero NO lanza excepción
    /// (la copia local en BD ya se guardó por separado).
    /// </summary>
    Task<ResultadoSuscripcion> SuscribirAsync(string email, string? nombre = null,
                                              CancellationToken ct = default);
}

public record ResultadoSuscripcion(bool Exito, string? MailchimpId, string? Error);

/// <summary>
/// Cliente Mailchimp Marketing API v3.0 — endpoint /lists/{listId}/members.
///
/// No usamos un SDK porque el oficial de C# está poco mantenido.
/// HttpClient nativo + Bearer auth alcanza para nuestro caso.
/// </summary>
public class MailchimpService : INewsletterService
{
    private readonly HttpClient _http;
    private readonly MailchimpOptions _opt;
    private readonly ILogger<MailchimpService> _logger;

    public MailchimpService(HttpClient http, IOptions<MailchimpOptions> opt, ILogger<MailchimpService> logger)
    {
        _http = http;
        _opt = opt.Value;
        _logger = logger;

        if (!string.IsNullOrWhiteSpace(_opt.ApiKey) && !string.IsNullOrWhiteSpace(_opt.DataCenter))
        {
            _http.BaseAddress = new Uri($"https://{_opt.DataCenter}.api.mailchimp.com/3.0/");

            // Mailchimp acepta la API key con cualquier user-name vía Basic auth.
            var bytes = Encoding.UTF8.GetBytes($"anystring:{_opt.ApiKey}");
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(bytes));
        }
    }

    public async Task<ResultadoSuscripcion> SuscribirAsync(string email, string? nombre = null,
                                                            CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_opt.ApiKey) || string.IsNullOrWhiteSpace(_opt.AudienceId))
        {
            // Sin Mailchimp configurado: solo guardamos copia local (lo hace el caller).
            _logger.LogInformation("Mailchimp no configurado; suscripción solo en BD local para {Email}", email);
            return new ResultadoSuscripcion(true, null, null);
        }

        try
        {
            var partes = (nombre ?? string.Empty).Trim().Split(' ', 2);
            var firstName = partes.Length > 0 ? partes[0] : string.Empty;
            var lastName  = partes.Length > 1 ? partes[1] : string.Empty;

            var payload = new
            {
                email_address = email,
                status = _opt.DoubleOptIn ? "pending" : "subscribed",
                merge_fields = new { FNAME = firstName, LNAME = lastName }
            };

            var resp = await _http.PostAsJsonAsync(
                $"lists/{_opt.AudienceId}/members", payload, ct);

            if (resp.IsSuccessStatusCode)
            {
                var data = await resp.Content.ReadFromJsonAsync<MailchimpMemberResponse>(cancellationToken: ct);
                _logger.LogInformation("Suscripto a Mailchimp: {Email} (id={Id})", email, data?.Id);
                return new ResultadoSuscripcion(true, data?.Id, null);
            }

            var contenido = await resp.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("Mailchimp retornó {Status}: {Cuerpo}", (int)resp.StatusCode, contenido);
            return new ResultadoSuscripcion(false, null, contenido);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al suscribir a Mailchimp: {Email}", email);
            return new ResultadoSuscripcion(false, null, ex.Message);
        }
    }

    private sealed class MailchimpMemberResponse
    {
        public string? Id { get; set; }
        public string? Email_address { get; set; }
        public string? Status { get; set; }
    }
}
