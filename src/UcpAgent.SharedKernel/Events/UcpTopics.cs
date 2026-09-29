namespace UcpAgent.SharedKernel.Events;

/// <summary>
/// Nomes canônicos dos tópicos Kafka do fluxo UCP.
/// Um tópico por etapa — Search → Cart → Checkout → Order.
/// </summary>
public static class UcpTopics
{
    public const string SearchQueried  = "ucp.search.queried";   // busca realizada
    public const string CartItemAdded  = "ucp.cart.item_added";  // item adicionado ao carrinho
    public const string OrderCreated   = "ucp.order.created";    // checkout concluído, pedido criado
    public const string OrderUpdated   = "ucp.order.updated";    // status do pedido alterado
}
