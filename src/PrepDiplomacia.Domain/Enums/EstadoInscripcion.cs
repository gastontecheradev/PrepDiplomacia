namespace PrepDiplomacia.Domain.Enums;

public enum EstadoInscripcion
{
    /// <summary>El candidato dejó sus datos en la lista de espera.</summary>
    EnListaDeEspera = 0,

    /// <summary>El candidato inició el flujo de pago pero no lo completó.</summary>
    PagoPendiente = 1,

    /// <summary>El pago se completó. Acceso al área de alumnos habilitado.</summary>
    Activa = 2,

    /// <summary>El candidato pidió cancelar / no concretó el pago.</summary>
    Cancelada = 3,

    /// <summary>El curso terminó.</summary>
    Finalizada = 4
}
