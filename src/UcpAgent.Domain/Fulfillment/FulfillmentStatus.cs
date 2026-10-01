namespace UcpAgent.Domain.Fulfillment;

/// <summary>
/// Representa cada etapa do ciclo de vida de um pedido,
/// desde a confirmação de pagamento até a entrega final.
/// Preparado para integração futura com transportadoras e WMS.
/// </summary>
public enum FulfillmentStatus
{
    // ── Pré-fulfillment ────────────────────────────────────────────────────────
    PaymentConfirmed  = 0,   // Pagamento aprovado — aguardando início do fulfillment

    // ── Fulfillment interno ───────────────────────────────────────────────────
    Preparing         = 1,   // Picking & Packing em andamento no CD
    ReadyToShip       = 2,   // Embalado — aguardando coleta da transportadora

    // ── Logística / Last Mile ─────────────────────────────────────────────────
    HandedToCarrier   = 3,   // Coletado pela transportadora
    InTransit         = 4,   // Em trânsito na malha logística
    OutForDelivery    = 5,   // Saiu para entrega (última milha)
    DeliveryAttempted = 6,   // Tentativa de entrega não concluída

    // ── Conclusão ─────────────────────────────────────────────────────────────
    Delivered         = 7,   // Entregue ao destinatário
    Returned          = 8,   // Devolvido ao CD (logística reversa)
    Cancelled         = 9,   // Cancelado antes da expedição
}
