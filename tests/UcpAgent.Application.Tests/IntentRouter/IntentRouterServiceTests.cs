using UcpAgent.Application.IntentRouter;
using Xunit;
using FluentAssertions;

namespace UcpAgent.Application.Tests.IntentRouter;

public sealed class IntentRouterServiceTests
{
    private readonly IntentRouterService _sut = new();

    // ── Input inválido ────────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Detect_InputVazioOuNulo_RetornaUnknown(string? input)
    {
        var result = _sut.Detect(input!);
        result.Intent.Should().Be(IntentType.Unknown);
    }

    // ── Checkout ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("finalizar compra")]
    [InlineData("quero pagar")]
    [InlineData("checkout")]
    [InlineData("confirmar pedido")]
    [InlineData("fazer pedido")]
    [InlineData("comprar agora")]
    [InlineData("efetuar pagamento")]
    public void Detect_FrasesDeCheckout_RetornaCheckout(string input)
    {
        var result = _sut.Detect(input);
        result.Intent.Should().Be(IntentType.Checkout);
    }

    // ── AddToCart ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("adicionar ao carrinho")]
    [InlineData("colocar na cesta")]
    [InlineData("quero comprar e add no carrinho")]
    [InlineData("incluir no cart")]
    public void Detect_FrasesDeAddToCart_RetornaAddToCart(string input)
    {
        var result = _sut.Detect(input);
        result.Intent.Should().Be(IntentType.AddToCart);
    }

    // ── RemoveFromCart ────────────────────────────────────────────────────────

    [Theory]
    [InlineData("remover do carrinho")]
    [InlineData("tirar item do cart")]
    [InlineData("excluir produto do carrinho")]
    [InlineData("deletar item da cesta")]
    public void Detect_FrasesDeRemove_RetornaRemoveFromCart(string input)
    {
        var result = _sut.Detect(input);
        result.Intent.Should().Be(IntentType.RemoveFromCart);
    }

    // ── ViewCart ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("ver carrinho")]
    [InlineData("mostrar cart")]
    [InlineData("o que tenho no carrinho")]
    [InlineData("carrinho")]
    [InlineData("meu carrinho")]
    public void Detect_FrasesDeViewCart_RetornaViewCart(string input)
    {
        var result = _sut.Detect(input);
        result.Intent.Should().Be(IntentType.ViewCart);
    }

    // ── GetOrder ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("status do pedido")]
    [InlineData("rastrear entrega")]
    [InlineData("onde está meu pedido")]
    [InlineData("acompanhar pedido")]
    [InlineData("ORDER-AB12CD34")]
    public void Detect_FrasesDeOrder_RetornaGetOrder(string input)
    {
        var result = _sut.Detect(input);
        result.Intent.Should().Be(IntentType.GetOrder);
    }

    [Fact]
    public void Detect_ComOrderId_ExtraiOrderIdCorretamente()
    {
        var result = _sut.Detect("qual o status do ORDER-AB12CD34");
        result.Intent.Should().Be(IntentType.GetOrder);
        result.OrderId.Should().Be("ORDER-AB12CD34");
    }

    [Fact]
    public void Detect_SemOrderId_OrderIdNulo()
    {
        var result = _sut.Detect("onde está meu pedido");
        result.Intent.Should().Be(IntentType.GetOrder);
        result.OrderId.Should().BeNull();
    }

    // ── SearchProducts ────────────────────────────────────────────────────────

    [Theory]
    [InlineData("buscar notebook")]
    [InlineData("procurar tênis")]
    [InlineData("quero um celular")]
    [InlineData("mostrar produtos de informática")]
    [InlineData("preciso de fone de ouvido")]
    public void Detect_FrasesDeSearch_RetornaSearchProducts(string input)
    {
        var result = _sut.Detect(input);
        result.Intent.Should().Be(IntentType.SearchProducts);
    }

    [Fact]
    public void Detect_PalavrasSimples_RetornaSearchProducts()
    {
        // até 5 palavras sem intenção clara → trata como busca
        var result = _sut.Detect("notebook gamer RTX");
        result.Intent.Should().Be(IntentType.SearchProducts);
        result.Query.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Detect_FraseDeSearch_LimpaStopwords()
    {
        var result = _sut.Detect("quero um notebook barato");
        result.Intent.Should().Be(IntentType.SearchProducts);
        result.Query.Should().NotContain("quero");
        result.Query.Should().Contain("notebook");
    }

    // ── Unknown ───────────────────────────────────────────────────────────────

    [Fact]
    public void Detect_FraseLongaSemIntencao_RetornaUnknown()
    {
        // mais de 5 palavras sem match de regex → Unknown
        var result = _sut.Detect("essa é uma frase muito longa sem nenhuma intenção clara de compra");
        result.Intent.Should().Be(IntentType.Unknown);
    }

    // ── SessionId ─────────────────────────────────────────────────────────────

    [Fact]
    public void Detect_ComSessionId_PropagaSessionId()
    {
        var result = _sut.Detect("buscar notebook", sessionId: "session-abc");
        result.SessionId.Should().Be("session-abc");
    }

    [Fact]
    public void Detect_SemSessionId_SessionIdNulo()
    {
        var result = _sut.Detect("buscar notebook");
        result.SessionId.Should().BeNull();
    }

    // ── Prioridade de intenções ───────────────────────────────────────────────

    [Fact]
    public void Detect_CheckoutTemPrioridade_SobreSearch()
    {
        // "finalizar" deve bater checkout antes de qualquer outra intenção
        var result = _sut.Detect("finalizar minha compra agora");
        result.Intent.Should().Be(IntentType.Checkout);
    }
}
