using UcpAgent.Api.Models;
using UcpAgent.Application.IntentRouter;
using UcpAgent.Application.Search;
using UcpAgent.Application.Cart;
using UcpAgent.Application.Checkout;
using UcpAgent.Application.Order;
using UcpAgent.SharedKernel.Models;
using UcpAgent.SharedKernel.Ports;
using MediatR;

namespace UcpAgent.Api.Endpoints;

public static class IntentEndpoints
{
    public static void MapIntentEndpoints(this WebApplication app)
    {
        app.MapPost("/api/intent", async (
            IntentRequest        req,
            IIntentRouterService router,
            IMediator            mediator) =>
        {
            var intent = router.Detect(req.Text, req.SessionId);

            var (success, data, message) = intent.Intent switch
            {
                IntentType.SearchProducts => await HandleSearch(intent, mediator),
                IntentType.AddToCart      => await HandleAddToCart(intent, mediator),
                IntentType.ViewCart       => await HandleViewCart(intent),
                IntentType.Checkout       => await HandleCheckout(intent, mediator),
                IntentType.GetOrder       => await HandleGetOrder(intent, mediator),
                _                         => (false, (object?)null, "Nao entendi. Tente: buscar produto, ver carrinho, finalizar pedido.")
            };

            var response = new IntentResponse(intent.Intent.ToString(), success, data, message);
            return success ? Results.Ok(response) : Results.UnprocessableEntity(response);
        })
        .WithName("ProcessIntent")
        .WithTags("Intent")
        .Produces<IntentResponse>(200)
        .Produces<IntentResponse>(422)
        .WithOpenApi();
    }

    private static async Task<(bool, object?, string?)> HandleSearch(IntentResult i, IMediator m)
    {
        var r = await m.Send(new SearchProductsQuery(i.ExtractedQuery ?? i.RawInput, 1, 10, null, null, null));
        return r.IsSuccess ? (true, (object?)r.Value, null) : (false, null, r.Error);
    }

    private static async Task<(bool, object?, string?)> HandleAddToCart(IntentResult i, IMediator m)
    {
        if (i.SessionId is null) return (false, null, "SessionId obrigatorio.");
        if (i.ProductId is null) return (false, null, "Informe o ID do produto.");
        var product = new ProductDto(i.ProductId, i.ProductId, 0, null, null, null, "Unknown");
        var r = await m.Send(new AddToCartCommand(i.SessionId, product, 1));
        return r.IsSuccess ? (true, (object?)r.Value, null) : (false, null, r.Error);
    }

    private static Task<(bool, object?, string?)> HandleViewCart(IntentResult i)
        => Task.FromResult<(bool, object?, string?)>((true, (object?)new { sessionId = i.SessionId, message = "Use GET /api/cart/{sessionId}" }, null));

    private static async Task<(bool, object?, string?)> HandleCheckout(IntentResult i, IMediator m)
    {
        if (i.SessionId is null) return (false, null, "SessionId obrigatorio.");
        var customer = new CustomerDto("", "", "", "");
        var r = await m.Send(new CheckoutCommand(i.SessionId, customer));
        return r.IsSuccess ? (true, (object?)r.Value, null) : (false, null, r.Error);
    }

    private static async Task<(bool, object?, string?)> HandleGetOrder(IntentResult i, IMediator m)
    {
        if (i.OrderId is null) return (false, null, "Informe ORDER-XXXXXXXX.");
        var r = await m.Send(new GetOrderQuery(i.OrderId));
        return r.IsSuccess ? (true, (object?)r.Value, null) : (false, null, r.Error);
    }
}

