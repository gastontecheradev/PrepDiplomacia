namespace PrepDiplomacia.Domain.Enums;

public enum EstadoPublicacion
{
    /// <summary>Borrador — solo visible para Carolina en el admin.</summary>
    Borrador = 0,

    /// <summary>Publicado y visible en el sitio público.</summary>
    Publicado = 1,

    /// <summary>Despublicado — oculto del sitio público pero conservado.</summary>
    Archivado = 2
}
