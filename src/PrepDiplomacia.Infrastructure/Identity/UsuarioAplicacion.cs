using Microsoft.AspNetCore.Identity;

namespace PrepDiplomacia.Infrastructure.Identity;

/// <summary>
/// Usuario de la aplicación. Extiende IdentityUser de ASP.NET Identity.
/// Identity ya provee Email, UserName, PasswordHash, lockout, 2FA, etc.
/// Aquí agregamos campos del dominio: nombre completo y referencias del alumno.
/// </summary>
public class UsuarioAplicacion : IdentityUser
{
    public string? NombreCompleto { get; set; }

    /// <summary>Cuándo se creó el usuario (fuera del audit de Identity).</summary>
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    /// <summary>Última vez que el usuario inició sesión.</summary>
    public DateTime? UltimoLogin { get; set; }

    /// <summary>True si el usuario es alumno con acceso al área privada.</summary>
    public bool TieneAccesoArea { get; set; } = false;
}
