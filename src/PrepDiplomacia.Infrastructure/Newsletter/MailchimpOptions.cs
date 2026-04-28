namespace PrepDiplomacia.Infrastructure.Newsletter;

/// <summary>
/// Configuración para integración con Mailchimp.
///
/// Cómo obtener:
///  1) https://mailchimp.com → Account → Extras → API keys → "Create A Key"
///  2) El DataCenter es la parte después del "-" en la API key (ej. us21).
///  3) AudienceId (List ID): Audience → Settings → "Audience name and defaults".
///
/// dotnet user-secrets set "Mailchimp:ApiKey"     "xxxxxxxxxxxxxxx-us21"
/// dotnet user-secrets set "Mailchimp:DataCenter" "us21"
/// dotnet user-secrets set "Mailchimp:AudienceId" "abcdef1234"
/// </summary>
public class MailchimpOptions
{
    public const string SectionName = "Mailchimp";

    public string ApiKey { get; set; } = string.Empty;
    public string DataCenter { get; set; } = string.Empty;
    public string AudienceId { get; set; } = string.Empty;

    /// <summary>
    /// Si true, usa double opt-in (Mailchimp envía un mail de confirmación).
    /// Recomendado para cumplir con anti-spam y GDPR.
    /// </summary>
    public bool DoubleOptIn { get; set; } = true;
}
