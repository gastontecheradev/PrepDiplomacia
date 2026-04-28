namespace PrepDiplomacia.Domain.Common;

/// <summary>
/// Roles del sistema. Se usan tanto en seed como en [Authorize(Roles=...)].
/// </summary>
public static class RolesSistema
{
    public const string Admin = "Admin";
    public const string Alumno = "Alumno";
}
