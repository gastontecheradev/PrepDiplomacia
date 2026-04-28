namespace PrepDiplomacia.Domain.Enums;

public enum ModalidadPago
{
    /// <summary>Un único pago al inscribirse.</summary>
    PagoUnico = 0,

    /// <summary>Pago dividido en cuotas (Stripe Subscription o Payment Plan).</summary>
    Cuotas = 1
}
