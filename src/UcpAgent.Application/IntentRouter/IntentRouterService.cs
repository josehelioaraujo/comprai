using System.Text.RegularExpressions;

namespace UcpAgent.Application.IntentRouter;

public sealed class IntentRouterService : IIntentRouterService
{
    private static readonly Regex _rxCheckout = new(
        @"\b(finalizar?|fechar?|pagar?|concluir?|checkout|confirmar?\s+(compra|pedido)|fazer\s+pedido|efetuar|comprar\s+agora)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex _rxRemove = new(
        @"\b(remover?|tirar?|excluir?|deletar?|retirar?|apagar?)\b.*(carrinho|cart|cesta|item|produto)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex _rxAdd = new(
        @"\b(adicionar?|colocar?|botar?|incluir?|quero\s+comprar|pegar|add)\b.*(carrinho|cart|cesta|sacola)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex _rxViewCart = new(
        @"\b(ver?|mostrar?|abrir?|exibir?|listar?|o\s+que\s+(tenho|tem)|meu)\b.*(carrinho|cart|cesta|sacola)|^\s*carrinho\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex _rxOrder = new(
        @"\b(pedido|order|acompanhar?|rastrear?|status\s+do\s+pedido|onde\s+est[a\u00e1]|entrega|ORDER-[A-Z0-9]+)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex _rxSearch = new(
        @"\b(buscar?|procurar?|quero|achar?|encontrar?|pesquisar?|mostrar?|ver|exibir|listar?|preciso\s+de|tem\s|t\u00eam\s)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex _rxProductId = new(
        @"\b([A-Z]{2,}[0-9]+[A-Z0-9\-]*|[0-9]+[A-Z]{2,}[A-Z0-9\-]*)\b",
        RegexOptions.Compiled);

    private static readonly Regex _rxOrderId = new(
        @"\b(ORDER-[A-Z0-9]{8})\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

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

        if (_rxCheckout.IsMatch(text))
            return new IntentResult(IntentType.Checkout, null, null, sessionId, null, text);

        if (_rxRemove.IsMatch(text))
            return new IntentResult(IntentType.RemoveFromCart, null, ExtractProductId(text), sessionId, null, text);

        if (_rxAdd.IsMatch(text))
            return new IntentResult(IntentType.AddToCart, null, ExtractProductId(text), sessionId, null, text);

        if (_rxViewCart.IsMatch(text))
            return new IntentResult(IntentType.ViewCart, null, null, sessionId, null, text);

        if (_rxOrder.IsMatch(text))
            return new IntentResult(IntentType.GetOrder, null, null, sessionId, ExtractOrderId(text), text);

        if (_rxSearch.IsMatch(text))
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
        return Regex.Replace(s, @"\s{2,}", " ").Trim();
    }

    private static string? ExtractProductId(string input)
    {
        var m = _rxProductId.Match(input);
        return m.Success ? m.Value : null;
    }

    private static string? ExtractOrderId(string input)
    {
        var m = _rxOrderId.Match(input);
        return m.Success ? m.Value.ToUpperInvariant() : null;
    }

    private static int WordCount(string s) =>
        s.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

    private static IntentResult Unknown(string raw) =>
        new(IntentType.Unknown, null, null, null, null, raw);
}
