using NSubstitute;
using UcpAgent.Application.Payment;
using UcpAgent.Api.Mocks;
using UcpAgent.SharedKernel.Ports;
using Xunit;

namespace UcpAgent.Application.Tests.Payment;

public sealed class ProcessPaymentHandlerTests
{
    private readonly IPaymentPort _paymentPort = Substitute.For<IPaymentPort>();
    private readonly ProcessPaymentHandler _handler;

    public ProcessPaymentHandlerTests()
        => _handler = new ProcessPaymentHandler(_paymentPort);

    [Fact]
    public async Task Handle_ApprovedPayment_ReturnsSuccess()
    {
        // Arrange
        var method  = new PaymentMethodDto("mock", null, null);
        var command = new ProcessPaymentCommand("ORDER-001", 250m, "BRL", method);

        _paymentPort.ProcessAsync(command.OrderId, command.Amount, command.Currency, method)
            .Returns(new PaymentResultDto("MOCK-PAY-001", true, "approved", null, null, null));

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("approved", result.Value!.Status);
    }

    [Fact]
    public async Task Handle_FailedPayment_ReturnsError()
    {
        // Arrange
        var method  = new PaymentMethodDto("stripe", "pm_card_declined", null);
        var command = new ProcessPaymentCommand("ORDER-002", 100m, "BRL", method);

        _paymentPort.ProcessAsync(command.OrderId, command.Amount, command.Currency, method)
            .Returns(new PaymentResultDto(string.Empty, false, "failed", null, null, "Cartão recusado"));

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Cartão recusado", result.Error);
    }

    [Fact]
    public async Task MockPaymentAdapter_AlwaysApproves()
    {
        // Arrange
        var adapter = new MockPaymentAdapter();
        var method  = new PaymentMethodDto("mock", null, null);

        // Act
        var result = await adapter.ProcessAsync("ORDER-003", 99m, "BRL", method);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("approved", result.Status);
        Assert.StartsWith("MOCK-PAY-", result.PaymentId);
    }
}
