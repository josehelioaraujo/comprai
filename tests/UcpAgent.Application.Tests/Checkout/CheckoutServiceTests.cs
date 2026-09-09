using FluentAssertions;
using MediatR;
using NSubstitute;
using UcpAgent.Application.Checkout;
using UcpAgent.SharedKernel;
using UcpAgent.SharedKernel.Ports;
using Xunit;

namespace UcpAgent.Application.Tests.Checkout;

public sealed class CheckoutServiceTests
{
    private readonly ISender _mediator = Substitute.For<ISender>();

    private static CustomerDto MakeCustomer() =>
        new("Helio Andrade", "helio@comprai.app", "11999999999", "Rua Teste, 123");

    [Fact]
    public async Task CheckoutAsync_SessaoValida_RetornaOrderId()
    {
        var expected = Result<CheckoutResultDto>.Ok(new CheckoutResultDto("ORDER-001", true, null));
        _mediator.Send(Arg.Any<CheckoutCommand>(), Arg.Any<CancellationToken>())
                 .Returns(expected);

        var service = new CheckoutService(_mediator);
        var result  = await service.CheckoutAsync("session-1", MakeCustomer());

        result.IsSuccess.Should().BeTrue();
        result.Value!.OrderId.Should().Be("ORDER-001");
    }

    [Fact]
    public async Task CheckoutAsync_CarrinhoVazio_RetornaErroNoDto()
    {
        var expected = Result<CheckoutResultDto>.Ok(new CheckoutResultDto(string.Empty, false, "Carrinho vazio"));
        _mediator.Send(Arg.Any<CheckoutCommand>(), Arg.Any<CancellationToken>())
                 .Returns(expected);

        var service = new CheckoutService(_mediator);
        var result  = await service.CheckoutAsync("session-vazia", MakeCustomer());

        result.IsSuccess.Should().BeTrue();
        result.Value!.Success.Should().BeFalse();
        result.Value.Error.Should().Be("Carrinho vazio");
    }

    [Fact]
    public async Task CheckoutAsync_DeveChamarMediatorComSessionIdCorreto()
    {
        _mediator.Send(Arg.Any<CheckoutCommand>(), Arg.Any<CancellationToken>())
                 .Returns(Result<CheckoutResultDto>.Ok(new CheckoutResultDto("ORDER-X", true, null)));

        var service  = new CheckoutService(_mediator);
        var customer = MakeCustomer();
        await service.CheckoutAsync("session-42", customer);

        await _mediator.Received(1).Send(
            Arg.Is<CheckoutCommand>(c => c.SessionId == "session-42" && c.Customer == customer),
            Arg.Any<CancellationToken>());
    }
}
