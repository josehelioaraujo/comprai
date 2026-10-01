namespace UcpAgent.SharedKernel.Events;

/// <summary>
/// Tópicos Kafka do sistema Comprai/UCP.
/// Convenção: comprai.{domínio}.{evento}
/// </summary>
public static class UcpTopics
{
    // ── Carrinho ──────────────────────────────────────────────────────────────
    public const string CartItemAdded   = "comprai.cart.item-added";

    // ── Pedido ────────────────────────────────────────────────────────────────
    public const string OrderCreated    = "comprai.order.created";
    public const string OrderStatusUpdated = "comprai.order.status-updated";

    // ── Fulfillment ───────────────────────────────────────────────────────────
    public const string FulfillmentStarted        = "comprai.fulfillment.started";
    public const string FulfillmentStatusChanged  = "comprai.fulfillment.status-changed";
    public const string FulfillmentCancelled      = "comprai.fulfillment.cancelled";
}
