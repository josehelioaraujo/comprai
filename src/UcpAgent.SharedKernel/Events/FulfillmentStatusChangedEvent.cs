using System.Diagnostics.CodeAnalysis;

namespace UcpAgent.SharedKernel.Events;

/// <summary>
/// Evento publicado no Kafka a cada mudança de status do fulfillment.
/// Consumidores possíveis: notificação por email, SMS, push, dashboard operacional.
/// </summary>
[ExcludeFromCodeCoverage]
public record FulfillmentStatusChangedEvent(
    string   OrderId,
    string   Status,
    string   Description,
    DateTime OccurredAt,
    string?  TrackingCode = null,
    string?  CarrierCode  = null,
    string?  Location     = null
);
