namespace UcpAgent.Domain.Fulfillment;

/// <summary>
/// Agregado de Fulfillment — controla o ciclo de vida pós-pagamento.
/// Mantém histórico imutável (append-only) de todos os eventos de status.
/// </summary>
public sealed class FulfillmentAggregate
{
    public string              OrderId       { get; private set; }
    public FulfillmentStatus   CurrentStatus { get; private set; }
    public string?             TrackingCode  { get; private set; }
    public string?             CarrierCode   { get; private set; }
    public DateTime            CreatedAt     { get; private set; }
    public DateTime            UpdatedAt     { get; private set; }

    private readonly List<FulfillmentEvent> _events = [];
    public IReadOnlyList<FulfillmentEvent> Events => _events.AsReadOnly();

    private FulfillmentAggregate() { OrderId = ""; }

    public static FulfillmentAggregate Create(string orderId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(orderId);
        var agg = new FulfillmentAggregate
        {
            OrderId       = orderId,
            CurrentStatus = FulfillmentStatus.PaymentConfirmed,
            CreatedAt     = DateTime.UtcNow,
            UpdatedAt     = DateTime.UtcNow,
        };
        agg.AppendEvent(FulfillmentStatus.PaymentConfirmed, "Pagamento confirmado — aguardando fulfillment");
        return agg;
    }

    public static FulfillmentAggregate Reconstitute(string orderId, IEnumerable<FulfillmentEvent> history)
    {
        var agg = new FulfillmentAggregate { OrderId = orderId };
        foreach (var evt in history.OrderBy(e => e.OccurredAt))
        {
            agg._events.Add(evt);
            agg.ApplyEvent(evt);
        }
        return agg;
    }

    public void AdvanceTo(FulfillmentStatus newStatus, string description,
        string? trackingCode = null, string? carrierCode = null, string? location = null,
        string? operatorId = null)
    {
        if (CurrentStatus is FulfillmentStatus.Delivered
                          or FulfillmentStatus.Cancelled
                          or FulfillmentStatus.Returned)
            throw new InvalidOperationException(
                $"Pedido já está em estado final: {CurrentStatus}.");

        if (newStatus <= CurrentStatus && newStatus != FulfillmentStatus.Cancelled)
            throw new InvalidOperationException(
                $"Não é possível regredir status de {CurrentStatus} para {newStatus}.");

        if (trackingCode is not null) TrackingCode = trackingCode;
        if (carrierCode  is not null) CarrierCode  = carrierCode;

        AppendEvent(newStatus, description, carrierCode, trackingCode, location, operatorId);
    }

    public void Cancel(string reason, string? operatorId = null)
    {
        if (CurrentStatus is FulfillmentStatus.Delivered
                          or FulfillmentStatus.Cancelled
                          or FulfillmentStatus.Returned)
            throw new InvalidOperationException(
                $"Pedido já está em estado final: {CurrentStatus}.");

        AppendEvent(FulfillmentStatus.Cancelled, reason, operatorId: operatorId);
    }

    private void AppendEvent(FulfillmentStatus status, string description,
        string? carrierCode = null, string? trackingCode = null,
        string? location = null, string? operatorId = null)
    {
        var evt = new FulfillmentEvent(
            OrderId:      OrderId,
            Status:       status,
            Description:  description,
            OccurredAt:   DateTime.UtcNow,
            CarrierCode:  carrierCode,
            TrackingCode: trackingCode,
            Location:     location,
            OperatorId:   operatorId
        );
        _events.Add(evt);
        ApplyEvent(evt);
    }

    private void ApplyEvent(FulfillmentEvent evt)
    {
        CurrentStatus = evt.Status;
        UpdatedAt     = evt.OccurredAt;
        if (evt.TrackingCode is not null) TrackingCode = evt.TrackingCode;
        if (evt.CarrierCode  is not null) CarrierCode  = evt.CarrierCode;
    }
}
