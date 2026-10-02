using FluentAssertions;
using UcpAgent.Domain.Fulfillment;
using Xunit;

namespace UcpAgent.Application.Tests.Fulfillment;

public sealed class FulfillmentAggregateTests
{
    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_ValidOrderId_StartsAsPaymentConfirmed()
    {
        var agg = FulfillmentAggregate.Create("order-001");

        agg.CurrentStatus.Should().Be(FulfillmentStatus.PaymentConfirmed);
        agg.OrderId.Should().Be("order-001");
    }

    [Fact]
    public void Create_AppendInitialEvent()
    {
        var agg = FulfillmentAggregate.Create("order-002");

        agg.Events.Should().HaveCount(1);
        agg.Events[0].Status.Should().Be(FulfillmentStatus.PaymentConfirmed);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_EmptyOrderId_Throws(string orderId)
    {
        var act = () => FulfillmentAggregate.Create(orderId);

        act.Should().Throw<ArgumentException>();
    }

    // ── AdvanceTo ─────────────────────────────────────────────────────────────

    [Fact]
    public void AdvanceTo_Preparing_UpdatesStatus()
    {
        var agg = FulfillmentAggregate.Create("order-003");

        agg.AdvanceTo(FulfillmentStatus.Preparing, "Iniciando picking");

        agg.CurrentStatus.Should().Be(FulfillmentStatus.Preparing);
        agg.Events.Should().HaveCount(2);
    }

    [Fact]
    public void AdvanceTo_FullPipeline_AllEventsAppended()
    {
        var agg = FulfillmentAggregate.Create("order-004");
        var statuses = new[]
        {
            FulfillmentStatus.Preparing,
            FulfillmentStatus.ReadyToShip,
            FulfillmentStatus.HandedToCarrier,
            FulfillmentStatus.InTransit,
            FulfillmentStatus.OutForDelivery,
            FulfillmentStatus.Delivered,
        };

        foreach (var s in statuses)
            agg.AdvanceTo(s, $"Avançando para {s}");

        agg.CurrentStatus.Should().Be(FulfillmentStatus.Delivered);
        agg.Events.Should().HaveCount(statuses.Length + 1); // +1 do Create
    }

    [Fact]
    public void AdvanceTo_WithTrackingCode_PersistsCode()
    {
        var agg = FulfillmentAggregate.Create("order-005");
        agg.AdvanceTo(FulfillmentStatus.Preparing, "Picking");
        agg.AdvanceTo(FulfillmentStatus.ReadyToShip, "Embalado");

        agg.AdvanceTo(FulfillmentStatus.HandedToCarrier, "Coletado",
            trackingCode: "BR123456789BR", carrierCode: "CORREIOS");

        agg.TrackingCode.Should().Be("BR123456789BR");
        agg.CarrierCode.Should().Be("CORREIOS");
    }

    [Fact]
    public void AdvanceTo_Regression_Throws()
    {
        var agg = FulfillmentAggregate.Create("order-006");
        agg.AdvanceTo(FulfillmentStatus.Preparing,   "Picking");
        agg.AdvanceTo(FulfillmentStatus.ReadyToShip, "Embalado");

        var act = () => agg.AdvanceTo(FulfillmentStatus.Preparing, "Tentativa de regressão");

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*regredir*");
    }

    [Fact]
    public void AdvanceTo_AfterDelivered_Throws()
    {
        var agg = FulfillmentAggregate.Create("order-007");
        agg.AdvanceTo(FulfillmentStatus.Preparing,       "Picking");
        agg.AdvanceTo(FulfillmentStatus.ReadyToShip,     "Embalado");
        agg.AdvanceTo(FulfillmentStatus.HandedToCarrier, "Coletado");
        agg.AdvanceTo(FulfillmentStatus.InTransit,       "Trânsito");
        agg.AdvanceTo(FulfillmentStatus.OutForDelivery,  "Saiu");
        agg.AdvanceTo(FulfillmentStatus.Delivered,       "Entregue");

        var act = () => agg.AdvanceTo(FulfillmentStatus.Preparing, "Após entrega");

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*estado final*");
    }

    // ── Cancel ────────────────────────────────────────────────────────────────

    [Fact]
    public void Cancel_FromPreparing_SetsStatusCancelled()
    {
        var agg = FulfillmentAggregate.Create("order-008");
        agg.AdvanceTo(FulfillmentStatus.Preparing, "Picking");

        agg.Cancel("Cancelado a pedido do cliente");

        agg.CurrentStatus.Should().Be(FulfillmentStatus.Cancelled);
    }

    [Fact]
    public void Cancel_AppendsCancelEvent()
    {
        var agg = FulfillmentAggregate.Create("order-009");

        agg.Cancel("Pagamento estornado");

        agg.Events.Last().Status.Should().Be(FulfillmentStatus.Cancelled);
        agg.Events.Last().Description.Should().Be("Pagamento estornado");
    }

    [Fact]
    public void Cancel_AfterCancelled_Throws()
    {
        var agg = FulfillmentAggregate.Create("order-010");
        agg.Cancel("Primeiro cancelamento");

        // Cancel() agora tem guard de estado final — deve lançar exception
        var act = () => agg.Cancel("Segundo cancelamento");

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*estado final*");
    }

    [Fact]
    public void Cancel_AfterDelivered_Throws()
    {
        var agg = FulfillmentAggregate.Create("order-010b");
        agg.AdvanceTo(FulfillmentStatus.Preparing,       "Picking");
        agg.AdvanceTo(FulfillmentStatus.ReadyToShip,     "Embalado");
        agg.AdvanceTo(FulfillmentStatus.HandedToCarrier, "Coletado");
        agg.AdvanceTo(FulfillmentStatus.InTransit,       "Trânsito");
        agg.AdvanceTo(FulfillmentStatus.OutForDelivery,  "Saiu");
        agg.AdvanceTo(FulfillmentStatus.Delivered,       "Entregue");

        var act = () => agg.Cancel("Cancelar após entrega");

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*estado final*");
    }

    // ── Reconstitute ──────────────────────────────────────────────────────────

    [Fact]
    public void Reconstitute_FromHistory_RestoresCurrentStatus()
    {
        var history = new List<FulfillmentEvent>
        {
            new("order-011", FulfillmentStatus.PaymentConfirmed, "Pago",     DateTime.UtcNow.AddMinutes(-5)),
            new("order-011", FulfillmentStatus.Preparing,        "Picking",  DateTime.UtcNow.AddMinutes(-3)),
            new("order-011", FulfillmentStatus.ReadyToShip,      "Embalado", DateTime.UtcNow.AddMinutes(-1)),
        };

        var agg = FulfillmentAggregate.Reconstitute("order-011", history);

        agg.CurrentStatus.Should().Be(FulfillmentStatus.ReadyToShip);
        agg.Events.Should().HaveCount(3);
    }

    [Fact]
    public void Reconstitute_WithTrackingCode_RestoresCode()
    {
        var history = new List<FulfillmentEvent>
        {
            new("order-012", FulfillmentStatus.PaymentConfirmed, "Pago",     DateTime.UtcNow.AddMinutes(-5)),
            new("order-012", FulfillmentStatus.HandedToCarrier,  "Coletado", DateTime.UtcNow.AddMinutes(-1),
                TrackingCode: "BR999888777BR"),
        };

        var agg = FulfillmentAggregate.Reconstitute("order-012", history);

        agg.TrackingCode.Should().Be("BR999888777BR");
    }

    [Fact]
    public void Reconstitute_OutOfOrderHistory_SortsCorrectly()
    {
        var t0 = DateTime.UtcNow.AddMinutes(-10);
        var t1 = DateTime.UtcNow.AddMinutes(-5);
        var history = new List<FulfillmentEvent>
        {
            new("order-013", FulfillmentStatus.Preparing,        "Picking", t1),
            new("order-013", FulfillmentStatus.PaymentConfirmed, "Pago",    t0),
        };

        var agg = FulfillmentAggregate.Reconstitute("order-013", history);

        agg.CurrentStatus.Should().Be(FulfillmentStatus.Preparing);
    }

    // ── Append-only ───────────────────────────────────────────────────────────

    [Fact]
    public void Events_AreReadOnly_CannotBeModifiedExternally()
    {
        var agg = FulfillmentAggregate.Create("order-014");

        var events = agg.Events;
        var act    = () => ((System.Collections.Generic.IList<FulfillmentEvent>)events).Add(
            new FulfillmentEvent("order-014", FulfillmentStatus.Preparing, "Injeção", DateTime.UtcNow));

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void Events_CountGrowsMonotonically()
    {
        var agg = FulfillmentAggregate.Create("order-015");
        var counts = new List<int> { agg.Events.Count };

        agg.AdvanceTo(FulfillmentStatus.Preparing, "Picking");
        counts.Add(agg.Events.Count);

        agg.AdvanceTo(FulfillmentStatus.ReadyToShip, "Embalado");
        counts.Add(agg.Events.Count);

        counts.Should().BeInAscendingOrder();
    }
}
