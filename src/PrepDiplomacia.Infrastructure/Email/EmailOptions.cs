namespace PrepDiplomacia.Infrastructure.Email;

/// <summary>
/// Opciones SMTP. En desarrollo se llenan con dotnet user-secrets;
/// en producción con variables de entorno o Azure App Service Configuration.
///
/// Ejemplo (user-secrets):
///   dotnet user-secrets set "Email:Smtp:Host"     "smtp.gmail.com"
///   dotnet user-secrets set "Email:Smtp:Port"     "587"
///   dotnet user-secrets set "Email:Smtp:Usuario"  "contacto@prepdiplomacia.com"
///   dotnet user-secrets set "Email:Smtp:Password" "xxxx xxxx xxxx xxxx"   // App Password
/// </summary>
public class EmailOptions
{
    public const string SectionName = "Email";

    public SmtpOptions Smtp { get; set; } = new();

    /// <summary>Email "From" mostrado al destinatario.</summary>
    public string FromEmail { get; set; } = "contacto@prepdiplomacia.com";

    /// <summary>Nombre "From" mostrado al destinatario.</summary>
    public string FromNombre { get; set; } = "Prep Diplomacia";

    /// <summary>Dirección a la que se envían los formularios de contacto.</summary>
    public string DestinoFormularios { get; set; } = "contacto@prepdiplomacia.com";
}

public class SmtpOptions
{
    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public string Usuario { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool UsarStartTls { get; set; } = true;
}
