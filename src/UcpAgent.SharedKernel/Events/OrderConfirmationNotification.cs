using System.Diagnostics.CodeAnalysis;

namespace UcpAgent.SharedKernel.Events;

/// <summary>
/// Payload da notificação de confirmação de pedido — publicado no RabbitMQ.
/// Não carrega PII diretamente: o worker busca email/nome no banco pelo CustomerId.
/// </summary>
[ExcludeFromCodeCoverage]
public record OrderConfirmationNotification(
    string OrderId,
    string SessionId,
    string CustomerId,
    decimal Total,
    int ItemCount,
    DateTime CreatedAt);
