using UcpAgent.SharedKernel;
using Xunit;
using FluentAssertions;

namespace UcpAgent.Application.Tests.SharedKernel;

public sealed class ResultTests
{
    // ── Result<T> ─────────────────────────────────────────────────────────────

    [Fact]
    public void Ok_ComValor_IsSuccessTrue()
    {
        var result = Result<string>.Ok("dados");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("dados");
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Fail_ComMensagem_IsSuccessFalse()
    {
        var result = Result<string>.Fail("algo deu errado");

        result.IsSuccess.Should().BeFalse();
        result.Value.Should().BeNull();
        result.Error.Should().Be("algo deu errado");
    }

    [Fact]
    public void Ok_ComValorNulo_IsSuccessTrue()
    {
        // Result<T?> com value null mas sucesso — cenário válido (ex: busca sem resultado)
        var result = Result<string?>.Ok(null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Ok_ComTipoInt_RetornaValorCorreto()
    {
        var result = Result<int>.Ok(42);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Ok_ComLista_RetornaListaCorreta()
    {
        var lista = new List<string> { "a", "b", "c" };
        var result = Result<List<string>>.Ok(lista);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
    }

    // ── Result (não-genérico) ─────────────────────────────────────────────────

    [Fact]
    public void ResultNaoGenerico_Ok_IsSuccessTrue()
    {
        var result = Result.Ok();

        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();
    }

    [Fact]
    public void ResultNaoGenerico_Fail_IsSuccessFalse()
    {
        var result = Result.Fail("erro na operação");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("erro na operação");
    }

    [Fact]
    public void ResultNaoGenerico_Fail_MensagemVazia_PropagataMensagem()
    {
        var result = Result.Fail(string.Empty);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().BeEmpty();
    }
}
