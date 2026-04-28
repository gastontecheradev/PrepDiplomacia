using System.ComponentModel.DataAnnotations;

namespace PrepDiplomacia.Web.Models.ViewModels;

public class LoginViewModel
{
    [Required, EmailAddress, Display(Name = "Correo electrónico")]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Recordarme")]
    public bool RecordarSesion { get; set; }

    public string? UrlRetorno { get; set; }
}

public class RegistroAlumnoViewModel
{
    [Required, StringLength(120), Display(Name = "Nombre completo")]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required, EmailAddress, Display(Name = "Correo electrónico")]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 8),
     Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password),
     Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden."),
     Display(Name = "Confirmar contraseña")]
    public string ConfirmarPassword { get; set; } = string.Empty;
}

public class RecuperarPasswordViewModel
{
    [Required, EmailAddress, Display(Name = "Correo electrónico")]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordViewModel
{
    [Required] public string Email { get; set; } = string.Empty;
    [Required] public string Token { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 8),
     Display(Name = "Contraseña nueva")]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password),
     Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden."),
     Display(Name = "Confirmar contraseña")]
    public string ConfirmarPassword { get; set; } = string.Empty;
}

public class CambiarPasswordViewModel
{
    [Required, DataType(DataType.Password), Display(Name = "Contraseña actual")]
    public string PasswordActual { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 8),
     Display(Name = "Contraseña nueva")]
    public string PasswordNueva { get; set; } = string.Empty;

    [Required, DataType(DataType.Password),
     Compare(nameof(PasswordNueva), ErrorMessage = "Las contraseñas no coinciden."),
     Display(Name = "Confirmar contraseña nueva")]
    public string ConfirmarPasswordNueva { get; set; } = string.Empty;
}
