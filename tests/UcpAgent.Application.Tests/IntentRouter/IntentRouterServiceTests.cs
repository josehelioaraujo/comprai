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
        var result = _sut.Detect("notebook gamer RTX");
        result.Intent.Should().Be(IntentType.SearchProducts);
        result.ExtractedQuery.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Detect_FraseDeSearch_LimpaStopwords()
    {
        var result = _sut.Detect("quero um notebook barato");
        result.Intent.Should().Be(IntentType.SearchProducts);
        result.ExtractedQuery.Should().NotContain("quero");
        result.ExtractedQuery.Should().Contain("notebook");
    }

    // ── Unknown ───────────────────────────────────────────────────────────────

    [Fact]
    public void Detect_FraseLongaSemIntencao_RetornaUnknown()
    {
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

    // ── Prioridade ────────────────────────────────────────────────────────────

    [Fact]
    public void Detect_CheckoutTemPrioridade_SobreSearch()
    {
        var result = _sut.Detect("finalizar minha compra agora");
        result.Intent.Should().Be(IntentType.Checkout);
    }

    [Fact]
    public void Detect_RawInput_SemprePreservado()
    {
        var input = "buscar notebook gamer";
        var result = _sut.Detect(input);
        result.RawInput.Should().Be(input);
    }

    // ── Mutantes linha 51: input null ?? string.Empty ────────────────────────

    [Fact]
    public void Detect_InputNull_RawInputEhStringVazia()
    {
        var result = _sut.Detect(null!);
        result.Intent.Should().Be(IntentType.Unknown);
        result.RawInput.Should().Be(string.Empty);
    }

    [Fact]
    public void Detect_InputNull_RawInputNaoEhPlaceholder()
    {
        var result = _sut.Detect(null!);
        result.RawInput.Should().NotBe("Stryker was here!");
        result.RawInput.Should().Be(string.Empty);
    }

    // ── Mutante linha 73: WordCount <= 5 vs < 5 ──────────────────────────────

    [Theory]
    [InlineData("notebook gamer barato bom rapido")]
    [InlineData("tenis running confortavel leve duravel")]
    public void Detect_FraseComExatamenteCincoPalavras_RetornaSearch(string input)
    {
        var result = _sut.Detect(input);
        result.Intent.Should().Be(IntentType.SearchProducts);
    }

    [Fact]
    public void Detect_FraseComSeisPalavrasSemIntencao_RetornaUnknown()
    {
        var result = _sut.Detect("essa frase tem seis palavras aqui");
        result.Intent.Should().Be(IntentType.Unknown);
    }

    // ── Mutantes linha 83-84: CleanQuery espaco nao vazio ────────────────────

    [Fact]
    public void Detect_CleanQuery_ContemTermoPrincipalAposRemocaoStopword()
    {
        var result = _sut.Detect("quero notebook gamer");
        result.Intent.Should().Be(IntentType.SearchProducts);
        result.ExtractedQuery.Should().Contain("notebook");
        result.ExtractedQuery.Should().NotContain("quero");
    }

    [Fact]
    public void Detect_CleanQuery_NaoContemEspacoDuplo()
    {
        var result = _sut.Detect("buscar ver notebook");
        result.Intent.Should().Be(IntentType.SearchProducts);
        result.ExtractedQuery.Should().Contain("notebook");
        result.ExtractedQuery.Should().NotContain("  ");
    }

    // ── Mutantes linha 90: ExtractProductId ──────────────────────────────────

    [Fact]
    public void Detect_RemoveComProductId_ExtraiIdCorreto()
    {
        var result = _sut.Detect("remover ABC123 do carrinho");
        result.Intent.Should().Be(IntentType.RemoveFromCart);
        result.ProductId.Should().Be("ABC123");
        result.ProductId.Should().NotBeNull();
    }

    [Fact]
    public void Detect_RemoveSemProductId_ProductIdNulo()
    {
        var result = _sut.Detect("remover item do carrinho");
        result.Intent.Should().Be(IntentType.RemoveFromCart);
        result.ProductId.Should().BeNull();
    }

}
