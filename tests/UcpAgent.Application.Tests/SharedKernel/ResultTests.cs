using UcpAgent.SharedKernel;
using Xunit;

namespace UcpAgent.Application.Tests.SharedKernel;

public class ResultTests
{
    [Fact]
    public void Ok_SetsIsSuccessTrue()
    {
        var result = Result<string>.Ok("valor");
        Assert.True(result.IsSuccess);
        Assert.Equal("valor", result.Value);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Fail_SetsIsSuccessFalse()
    {
        var result = Result<string>.Fail("erro");
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.Equal("erro", result.Error);
    }

    [Fact]
    public void Ok_NonGeneric_SetsIsSuccessTrue()
    {
        var result = Result.Ok();
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Fail_NonGeneric_SetsIsSuccessFalse()
    {
        var result = Result.Fail("erro genérico");
        Assert.False(result.IsSuccess);
        Assert.Equal("erro genérico", result.Error);
    }

    [Fact]
    public void Ok_WithNullValue_IsStillSuccess()
    {
        var result = Result<string?>.Ok(null);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
    }
}
