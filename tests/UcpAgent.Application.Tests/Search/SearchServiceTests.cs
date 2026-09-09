using FluentAssertions;
using MediatR;
using NSubstitute;
using UcpAgent.Application.Search;
using UcpAgent.SharedKernel;
using UcpAgent.SharedKernel.Models;
using Xunit;

namespace UcpAgent.Application.Tests.Search;

public sealed class SearchServiceTests
{
    private readonly ISender _mediator = Substitute.For<ISender>();

    private static SearchResult MakeResult(int count = 3) =>
        new(Enumerable.Range(1, count)
            .Select(i => new ProductDto(i.ToString(), $"Produto {i}", i * 100m, null, null, null, "mock", null, 5))
            .ToList(), count, 1, 10, "mock");

    [Fact]
    public async Task SearchAsync_QueryValida_RetornaProdutos()
    {
        var expected = Result<SearchResult>.Ok(MakeResult(3));
        _mediator.Send(Arg.Any<SearchProductsQuery>(), Arg.Any<CancellationToken>())
                 .Returns(expected);

        var service = new SearchService(_mediator);
        var result  = await service.SearchAsync("notebook");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task SearchAsync_PassaLimitCorretoParaQuery()
    {
        _mediator.Send(Arg.Any<SearchProductsQuery>(), Arg.Any<CancellationToken>())
                 .Returns(Result<SearchResult>.Ok(MakeResult()));

        var service = new SearchService(_mediator);
        await service.SearchAsync("tênis", limit: 5);

        await _mediator.Received(1).Send(
            Arg.Is<SearchProductsQuery>(q => q.Query == "tênis" && q.PageSize == 5),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_LimitPadrao_UsaDez()
    {
        _mediator.Send(Arg.Any<SearchProductsQuery>(), Arg.Any<CancellationToken>())
                 .Returns(Result<SearchResult>.Ok(MakeResult()));

        var service = new SearchService(_mediator);
        await service.SearchAsync("celular");

        await _mediator.Received(1).Send(
            Arg.Is<SearchProductsQuery>(q => q.PageSize == 10),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_SemResultados_RetornaListaVazia()
    {
        var empty = Result<SearchResult>.Ok(new SearchResult([], 0, 1, 10, "mock"));
        _mediator.Send(Arg.Any<SearchProductsQuery>(), Arg.Any<CancellationToken>())
                 .Returns(empty);

        var service = new SearchService(_mediator);
        var result  = await service.SearchAsync("xyzabc");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }
}
