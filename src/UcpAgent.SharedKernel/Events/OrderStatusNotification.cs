using System.Diagnostics.CodeAnalysis;

namespace UcpAgent.SharedKernel.Events;

/// <summary>
/// Payload da notificação de atualização de status — publicado no RabbitMQ.
/// Não carrega PII diretamente: o worker busca email no banco pelo CustomerId.
/// </summary>
[ExcludeFromCodeCoverage]
public record OrderStatusNotification(
    string OrderId,
    string CustomerId,
    string OldStatus,
    string NewStatus,
    DateTime UpdatedAt);
