namespace PrepDiplomacia.Infrastructure.Payments;

/// <summary>
/// Configuración de Stripe. Las claves NUNCA van en appsettings.json versionado:
/// se cargan vía user-secrets (dev) o variables de entorno (prod).
///
/// Ejemplo (user-secrets):
///   dotnet user-secrets set "Stripe:SecretKey"     "sk_test_xxx"
///   dotnet user-secrets set "Stripe:PublishableKey" "pk_test_xxx"
///   dotnet user-secrets set "Stripe:WebhookSecret" "whsec_xxx"
/// </summary>
public class StripeOptions
{
    public const string SectionName = "Stripe";

    /// <summary>Clave secreta de la API (sk_test_ o sk_live_).</summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>Clave pública de la API (pk_test_ o pk_live_).</summary>
    public string PublishableKey { get; set; } = string.Empty;

    /// <summary>Secret para validar firmas de webhooks (whsec_xxx).</summary>
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>URL base del sitio (sin barra final). Para construir success/cancel URLs.</summary>
    public string SiteUrl { get; set; } = "https://prepdiplomacia.uy";

    /// <summary>Moneda por defecto (usd, uyu, eur, etc).</summary>
    public string MonedaDefault { get; set; } = "usd";
}
