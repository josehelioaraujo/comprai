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

    // Mapeamento de termos PT-BR para ingl\u00eas (para cat\u00e1logos como DummyJSON que operam em ingl\u00eas)
    private static readonly Dictionary<string, string> _ptBrToEn = new(StringComparer.OrdinalIgnoreCase)
    {
        // Eletr\u00f4nicos
        { "celular",       "smartphone"  },
        { "celulares",     "smartphones" },
        { "smartphone",    "smartphone"  },
        { "tablet",        "tablet"      },
        { "notebook",      "laptop"      },
        { "computador",    "computer"    },
        { "televis\u00e3o",     "tv"          },
        { "televisao",     "tv"          },
        { "televisor",     "tv"          },
        { "fone",          "headphone"   },
        { "fones",         "headphones"  },
        { "c\u00e2mera",        "camera"      },
        { "camera",        "camera"      },
        { "relogio",       "watch"       },
        { "rel\u00f3gio",       "watch"       },
        { "smartwatch",    "watch"       },
        { "carregador",    "charger"     },
        // Moda / acess\u00f3rios
        { "camiseta",      "t-shirt"     },
        { "cal\u00e7a",         "pants"       },
        { "calca",         "pants"       },
        { "vestido",       "dress"       },
        { "sapato",        "shoes"       },
        { "sapatos",       "shoes"       },
        { "tenis",         "sneakers"    },
        { "t\u00eanis",         "sneakers"    },
        { "bolsa",         "bag"         },
        { "mochila",       "bag"         },
        { "anel",          "ring"        },
        { "colar",         "necklace"    },
        { "brinco",        "earring"     },
        { "pulseira",      "bracelet"    },
        // Beleza / sa\u00fade
        { "perfume",       "perfume"     },
        { "creme",         "cream"       },
        { "batom",         "lipstick"    },
        { "maquiagem",     "makeup"      },
        { "skincare",      "skincare"    },
        { "shampoo",       "shampoo"     },
        // Casa
        { "sof\u00e1",          "sofa"        },
        { "sofa",          "sofa"        },
        { "mesa",          "table"       },
        { "cadeira",       "chair"       },
        { "luminaria",     "lamp"        },
        { "lumin\u00e1ria",     "lamp"        },
        { "geladeira",     "refrigerator"},
        // Outros
        { "livro",         "book"        },
        { "livros",        "books"       },
        { "jogo",          "game"        },
        { "jogos",         "games"       },
        { "brinquedo",     "toy"         },
        { "brinquedos",    "toys"        },
        { "bicicleta",     "bicycle"     },
        { "moto",          "motorcycle"  },
    };

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
        s = RxMultiSpace().Replace(s, " ").Trim();

        // Traduz termos PT-BR → inglês palavra a palavra
        var words = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var translated = words.Select(w => _ptBrToEn.TryGetValue(w, out var en) ? en : w);
        return string.Join(" ", translated).Trim();
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
