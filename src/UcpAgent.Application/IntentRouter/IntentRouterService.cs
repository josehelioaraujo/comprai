using System.Text.RegularExpressions;

namespace UcpAgent.Application.IntentRouter;

public sealed partial class IntentRouterService : IIntentRouterService
{
    [GeneratedRegex(@"\b(finalizar?|fechar?|pagar?|concluir?|checkout|confirmar?\s+(compra|pedido)|fazer\s+pedido|efetuar|comprar\s+agora)\b",
        RegexOptions.IgnoreCase)]
    private static partial Regex RxCheckout();

    [GeneratedRegex(@"\b(remover?|tirar?|excluir?|deletar?|retirar?|apagar?)\b.*(carrinho|cart|cesta|item|produto)",
        RegexOptions.IgnoreCase)]
    private static partial Regex RxRemove();

    [GeneratedRegex(@"\b(adicionar?|colocar?|botar?|incluir?|quero\s+comprar|pegar|add)\b.*(carrinho|cart|cesta|sacola)",
        RegexOptions.IgnoreCase)]
    private static partial Regex RxAdd();

    [GeneratedRegex(@"\b(ver?|mostrar?|abrir?|exibir?|listar?|o\s+que\s+(tenho|tem)|meu)\b.*(carrinho|cart|cesta|sacola)|^\s*carrinho\s*$",
        RegexOptions.IgnoreCase)]
    private static partial Regex RxViewCart();

    [GeneratedRegex(@"\b(pedido|order|acompanhar?|rastrear?|status\s+do\s+pedido|onde\s+est[a\u00e1]|entrega|ORDER-[A-Z0-9]+)\b",
        RegexOptions.IgnoreCase)]
    private static partial Regex RxOrder();

    [GeneratedRegex(@"\b(buscar?|procurar?|quero|achar?|encontrar?|pesquisar?|mostrar?|ver|exibir|listar?|preciso\s+de|tem\s|t\u00eam\s)\b",
        RegexOptions.IgnoreCase)]
    private static partial Regex RxSearch();

    [GeneratedRegex(@"\b([A-Z]{2,}[0-9]+[A-Z0-9\-]*|[0-9]+[A-Z]{2,}[A-Z0-9\-]*)\b")]
    private static partial Regex RxProductId();

    [GeneratedRegex(@"\b(ORDER-[A-Z0-9]{8})\b", RegexOptions.IgnoreCase)]
    private static partial Regex RxOrderId();

    [GeneratedRegex(@"\s{2,}")]
    private static partial Regex RxMultiSpace();

    private static readonly string[] _searchStopwords =
    [
        "buscar", "busca", "procurar", "quero", "achar", "encontrar",
        "pesquisar", "mostrar", "ver", "exibir", "listar", "preciso de",
        "preciso", "tem ", "t\u00eam ", "por favor", "pf", "pfv", "me ",
        "um ", "uma ", "algum", "alguma", "bom ", "boa ", "barato", "barata"
    ];

    public IntentResult Detect(string input, string? sessionId = null)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Unknown(input ?? string.Empty);

        var text = input.Trim();

        if (RxCheckout().IsMatch(text))
            return new IntentResult(IntentType.Checkout, null, null, sessionId, null, text);

        if (RxRemove().IsMatch(text))
            return new IntentResult(IntentType.RemoveFromCart, null, ExtractProductId(text), sessionId, null, text);

        if (RxAdd().IsMatch(text))
            return new IntentResult(IntentType.AddToCart, null, ExtractProductId(text), sessionId, null, text);

        if (RxViewCart().IsMatch(text))
            return new IntentResult(IntentType.ViewCart, null, null, sessionId, null, text);

        if (RxOrder().IsMatch(text))
            return new IntentResult(IntentType.GetOrder, null, null, sessionId, ExtractOrderId(text), text);

        if (RxSearch().IsMatch(text))
            return new IntentResult(IntentType.SearchProducts, CleanQuery(text), null, sessionId, null, text);

        if (WordCount(text) <= 5)
            return new IntentResult(IntentType.SearchProducts, text, null, sessionId, null, text);

        return Unknown(text);
    }

    private static string CleanQuery(string input)
    {
        var s = input.ToLowerInvariant();
        foreach (var sw in _searchStopwords)
            s = s.Replace(sw, " ");
        return RxMultiSpace().Replace(s, " ").Trim();
    }

    private static string? ExtractProductId(string input)
    {
        var m = RxProductId().Match(input);
        return m.Success ? m.Value : null;
    }

    private static string? ExtractOrderId(string input)
    {
        var m = RxOrderId().Match(input);
        return m.Success ? m.Value.ToUpperInvariant() : null;
    }

    private static int WordCount(string s) =>
        s.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

    private static IntentResult Unknown(string raw) =>
        new(IntentType.Unknown, null, null, null, null, raw);
}
