namespace PrepDiplomacia.Domain.Enums;

public enum EstadoPago
{
    /// <summary>Sesión de Checkout creada, todavía no completada.</summary>
    Pendiente = 0,

    /// <summary>Pago completado exitosamente.</summary>
    Completado = 1,

    /// <summary>El usuario abandonó el checkout o falló la tarjeta.</summary>
    Fallido = 2,

    /// <summary>Reembolsado total o parcialmente desde Stripe.</summary>
    Reembolsado = 3,

    /// <summary>Pago en cuotas: aún no se completaron todas las cuotas.</summary>
    EnCuotas = 4
}
