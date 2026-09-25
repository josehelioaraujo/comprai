using Moq;
using UcpAgent.Application.Checkout;
using UcpAgent.SharedKernel;
using UcpAgent.SharedKernel.Ports;
using Xunit;

namespace UcpAgent.Application.Tests.Checkout;

public sealed class CheckoutHandlerTests
{
    private static CustomerDto MakeCustomer() =>
        new("Helio Andrade", "helio@comprai.app", "11999999999", "Rua Teste, 123");

    private static readonly UcpMetrics _metrics = new();

    [Fact]
    public async Task Handle_ValidSession_ReturnsOrderId()
    {
        var checkout = new Mock<ICheckoutPort>();
        var handler  = new CheckoutHandler(checkout.Object, _metrics);
        var customer = MakeCustomer();
        var command  = new CheckoutCommand("session-1", customer);

        checkout.Setup(c => c.ProcessAsync("session-1", customer, default))
                .ReturnsAsync(new CheckoutResultDto("ORDER-001", true, null));

        var result = await handler.Handle(command, default);

        Assert.True(result.IsSuccess);
        Assert.Equal("ORDER-001", result.Value!.OrderId);
        Assert.True(result.Value.Success);
    }

    [Fact]
    public async Task Handle_CallsPortWithCorrectSession()
    {
        var checkout = new Mock<ICheckoutPort>();
        var handler  = new CheckoutHandler(checkout.Object, _metrics);
        var customer = MakeCustomer();
        var command  = new CheckoutCommand("session-42", customer);

        checkout.Setup(c => c.ProcessAsync(It.IsAny<string>(), It.IsAny<CustomerDto>(), default))
                .ReturnsAsync(new CheckoutResultDto("ORDER-042", true, null));

        await handler.Handle(command, default);

        checkout.Verify(c => c.ProcessAsync("session-42", customer, default), Times.Once);
    }

    [Fact]
    public async Task Handle_CheckoutFails_ReturnsErrorInDto()
    {
        var checkout = new Mock<ICheckoutPort>();
        var handler  = new CheckoutHandler(checkout.Object, _metrics);
        var customer = MakeCustomer();
        var command  = new CheckoutCommand("session-empty", customer);

        checkout.Setup(c => c.ProcessAsync("session-empty", customer, default))
                .ReturnsAsync(new CheckoutResultDto(string.Empty, false, "Carrinho vazio"));

        var result = await handler.Handle(command, default);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.Success);
        Assert.Equal("Carrinho vazio", result.Value.Error);
    }
}
