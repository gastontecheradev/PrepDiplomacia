namespace PrepDiplomacia.Domain.Common;

/// <summary>
/// Clase base para entidades persistentes. Provee campos de auditoría
/// y un Id de tipo int (AUTO_INCREMENT en SQLite/SQL Server).
/// </summary>
public abstract class EntidadBase
{
    public int Id { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaActualizacion { get; set; }
}
