using UcpAgent.Api.Models;
using UcpAgent.Application.IntentRouter;
using UcpAgent.Application.Search;
using UcpAgent.Application.Cart;
using UcpAgent.Application.Checkout;
using UcpAgent.Application.Orders;

namespace UcpAgent.Api.Endpoints;

public static class IntentEndpoints
{
    public static void MapIntentEndpoints(this WebApplication app)
    {
        app.MapPost("/api/intent", async (
            IntentRequest        req,
            IIntentRouterService router,
            ISearchService       search,
            ICartService         cart,
            ICheckoutService     checkout,
            IOrderService        orders) =>
        {
            var intent = router.Detect(req.Text, req.SessionId);

            var (success, data, message) = intent.Intent switch
            {
                IntentType.SearchProducts  => await HandleSearch(intent, search),
                IntentType.AddToCart       => await HandleAddToCart(intent, cart),
                IntentType.RemoveFromCart  => await HandleRemoveFromCart(intent, cart),
                IntentType.ViewCart        => await HandleViewCart(intent, cart),
                IntentType.Checkout        => await HandleCheckout(intent, checkout),
                IntentType.GetOrder        => await HandleGetOrder(intent, orders),
                _                          => (false, (object?)null, "N\u00e3o entendi. Tente: buscar produto, ver carrinho, finalizar pedido.")
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

    private static async Task<(bool, object?, string?)> HandleSearch(IntentResult i, ISearchService s)
    {
        var r = await s.SearchAsync(i.ExtractedQuery ?? i.RawInput, limit: 10);
        return r.IsSuccess ? (true, r.Value, null) : (false, null, r.Error);
    }

    private static async Task<(bool, object?, string?)> HandleAddToCart(IntentResult i, ICartService c)
    {
        if (i.SessionId is null) return (false, null, "SessionId obrigat\u00f3rio.");
        if (i.ProductId is null) return (false, null, "Informe o ID do produto.");
        var r = await c.AddItemAsync(i.SessionId, i.ProductId, 1);
        return r.IsSuccess ? (true, r.Value, null) : (false, null, r.Error);
    }

    private static async Task<(bool, object?, string?)> HandleRemoveFromCart(IntentResult i, ICartService c)
    {
        if (i.SessionId is null) return (false, null, "SessionId obrigat\u00f3rio.");
        if (i.ProductId is null) return (false, null, "Informe o ID do produto.");
        var r = await c.RemoveItemAsync(i.SessionId, i.ProductId);
        return r.IsSuccess ? (true, r.Value, null) : (false, null, r.Error);
    }

    private static async Task<(bool, object?, string?)> HandleViewCart(IntentResult i, ICartService c)
    {
        if (i.SessionId is null) return (false, null, "SessionId obrigat\u00f3rio.");
        var r = await c.GetCartAsync(i.SessionId);
        return r.IsSuccess ? (true, r.Value, null) : (false, null, r.Error);
    }

    private static async Task<(bool, object?, string?)> HandleCheckout(IntentResult i, ICheckoutService c)
    {
        if (i.SessionId is null) return (false, null, "SessionId obrigat\u00f3rio.");
        var r = await c.CheckoutAsync(i.SessionId);
        return r.IsSuccess ? (true, r.Value, null) : (false, null, r.Error);
    }

    private static async Task<(bool, object?, string?)> HandleGetOrder(IntentResult i, IOrderService o)
    {
        if (i.OrderId is null && i.SessionId is null)
            return (false, null, "Informe ORDER-XXXXXXXX ou sessionId.");
        var r = i.OrderId is not null
            ? await o.GetByIdAsync(i.OrderId)
            : await o.GetBySessionAsync(i.SessionId!);
        return r.IsSuccess ? (true, r.Value, null) : (false, null, r.Error);
    }
}
