using FluentAssertions;
using UcpAgent.SharedKernel;
using Xunit;

namespace UcpAgent.Application.Tests.SharedKernel;

public class ResultTests
{
    [Fact]
    public void Ok_DeveRetornarResultadoComSucesso()
    {
        var result = Result<string>.Ok("valor");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("valor");
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Fail_DeveRetornarResultadoComErro()
    {
        var result = Result<string>.Fail("erro");

        result.IsSuccess.Should().BeFalse();
        result.Value.Should().BeNull();
        result.Error.Should().Be("erro");
    }

    [Fact]
    public void Ok_ComValorNulo_DeveSerValido()
    {
        var result = Result<string?>.Ok(null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Fail_ComMensagemVazia_DeveRetornarErroVazio()
    {
        var result = Result<int>.Fail("");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().BeEmpty();
    }
}
