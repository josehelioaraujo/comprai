namespace UcpAgent.Domain.Fulfillment;

/// <summary>
/// Representa um evento imutável no histórico de fulfillment.
/// Cada mudança de status gera um novo evento — nunca sobrescreve.
/// Preparado para persistência em banco de dados (append-only).
/// </summary>
public sealed record FulfillmentEvent(
    string          OrderId,
    FulfillmentStatus Status,
    string          Description,
    DateTime        OccurredAt,
    string?         CarrierCode    = null,   // Código da transportadora (futuro)
    string?         TrackingCode   = null,   // Código de rastreio (futuro)
    string?         Location       = null,   // Localização (futuro: GPS / cidade)
    string?         OperatorId     = null    // Id do operador/sistema que gerou o evento
);
