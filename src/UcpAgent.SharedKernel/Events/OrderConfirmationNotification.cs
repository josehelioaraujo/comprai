using System.Diagnostics.CodeAnalysis;

namespace UcpAgent.SharedKernel.Events;

/// <summary>
/// Payload da notificação de confirmação de pedido — publicado no RabbitMQ.
/// Inclui dados suficientes para montar e-mail rico sem consulta adicional ao banco.
/// </summary>
[ExcludeFromCodeCoverage]
public record OrderConfirmationNotification(
    string OrderId,
    string SessionId,
    string CustomerId,
    decimal Total,
    int ItemCount,
    DateTime CreatedAt,
    // Dados adicionais para e-mail rico (preenchidos pelo RedisCheckoutAdapter)
    string CustomerEmail  = "",
    string CustomerName   = "",
    string ItemsJson      = "",   // JSON de IReadOnlyList<CartItemDto>
    string PaymentMethod  = "");
