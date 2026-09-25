# populate-metrics.ps1 — Popula todas as métricas UCP incluindo ucp_intent_detected_requests_total
# Uso: .\populate-metrics.ps1 [-BaseUrl "http://localhost:5020"] [-Rounds 3]

param(
    [string]$BaseUrl = "https://comprai.2.25.122.11.nip.io",
    [int]$Rounds = 3
)

$ErrorActionPreference = "Continue"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$sessionId = "populate-$(Get-Random -Maximum 9999)"

function Invoke-Api {
    param([string]$Method = "GET", [string]$Path, [object]$Body = $null)
    $uri = "$BaseUrl$Path"
    try {
        if ($Body) {
            $json = $Body | ConvertTo-Json -Depth 5 -Compress
            $r = Invoke-WebRequest -Method $Method -Uri $uri -Body $json `
                 -ContentType "application/json" -UseBasicParsing -TimeoutSec 15
        } else {
            $r = Invoke-WebRequest -Method $Method -Uri $uri `
                 -UseBasicParsing -TimeoutSec 15
        }
        return $r.StatusCode
    } catch {
        $resp = $_.Exception.Response; $code = if ($resp) { [int]$resp.StatusCode } else { "ERR" }
        return $code
    }
}

Write-Host "=== populate-metrics.ps1 ===" -ForegroundColor Cyan
Write-Host "BaseUrl : $BaseUrl"
Write-Host "Session : $sessionId"
Write-Host "Rounds  : $Rounds"
Write-Host ""

# ── Textos de intent por tipo ─────────────────────────────────────────────────
$intentTexts = @{
    "SearchProducts" = @(
        "quero comprar arroz 5kg",
        "buscar leite integral",
        "mostrar produtos de limpeza",
        "procurar notebook gamer",
        "tem cerveja gelada?",
        "encontrar fone de ouvido bluetooth",
        "pesquisar tenis running masculino",
        "ver smartphones samsung"
    )
    "AddToCart" = @(
        "adicionar ao carrinho MLB123456",
        "colocar na sacola o produto ABC789"
    )
    "ViewCart" = @(
        "ver meu carrinho",
        "o que tenho na cesta?",
        "mostrar carrinho"
    )
    "Checkout" = @(
        "finalizar pedido",
        "quero pagar",
        "fazer checkout agora"
    )
    "GetOrder" = @(
        "acompanhar pedido ORDER-ABCD1234",
        "status do pedido ORDER-XYZW5678"
    )
}

$totalIntents = 0
$totalSearch  = 0
$totalCart    = 0
$totalCheckout = 0

for ($round = 1; $round -le $Rounds; $round++) {
    Write-Host "--- Round $round/$Rounds ---" -ForegroundColor Yellow

    # ── 1. /api/intent — todas as intenções ──────────────────────────────────
    Write-Host "  [intent] Disparando intenções variadas..."
    foreach ($tipo in $intentTexts.Keys) {
        foreach ($texto in $intentTexts[$tipo]) {
            $body = @{ text = $texto; sessionId = $sessionId }
            $sc = Invoke-Api -Method "POST" -Path "/api/intent" -Body $body
            $totalIntents++
            Write-Host "    $tipo → '$texto' [$sc]"
            Start-Sleep -Milliseconds 300
        }
    }

    # ── 2. /api/search — funil direto ────────────────────────────────────────
    $searches = @("arroz", "leite", "feijao", "cafe", "acucar", "oleo", "frango", "pao", "manteiga", "queijo")
    Write-Host "  [search] $($searches.Count) buscas diretas..."
    foreach ($q in $searches) {
        $sc = Invoke-Api -Path "/api/search?q=$q&page=1&pageSize=5"
        $totalSearch++
        Write-Host "    /api/search?q=$q [$sc]"
        Start-Sleep -Milliseconds 200
    }

    # ── 3. /api/cart — add items ──────────────────────────────────────────────
    $products = @(
        @{ id="MLB001"; name="Arroz Tio Joao 5kg"; price=22.90; source="MercadoLivre" },
        @{ id="MLB002"; name="Leite Integral 1L";  price=5.49;  source="MercadoLivre" },
        @{ id="SHP001"; name="Feijao Carioca 1kg"; price=8.90;  source="Shopify" }
    )
    Write-Host "  [cart] Adicionando $($products.Count) produtos..."
    foreach ($p in $products) {
        $body = @{
            product  = @{ id=$p.id; name=$p.name; price=$p.price; source=$p.source; imageUrl=$null; url=$null; description=$null }
            quantity = 1
        }
        $sc = Invoke-Api -Method "POST" -Path "/api/cart/$sessionId/items" -Body $body
        $totalCart++
        Write-Host "    $($p.name) [$sc]"
        Start-Sleep -Milliseconds 200
    }

    # ── 4. /api/checkout ─────────────────────────────────────────────────────
    Write-Host "  [checkout] Checkout da sessao $sessionId..."
    $body = @{ name="Populate Script"; email="populate@test.com"; phone="11999999999"; address="Rua Teste, 123" }
    $sc = Invoke-Api -Method "POST" -Path "/api/checkout/$sessionId" -Body $body
    $totalCheckout++
    Write-Host "    /api/checkout [$sc]"

    # ── 5. /api/health/status — para métricas de health ──────────────────────
    $sc = Invoke-Api -Path "/api/health/status"
    Write-Host "  [health] /api/health/status [$sc]"

    # Nova sessão a cada round para não colidir
    $sessionId = "populate-$(Get-Random -Maximum 9999)"
    Start-Sleep -Milliseconds 500
}

Write-Host ""
Write-Host "=== Resumo ===" -ForegroundColor Green
Write-Host "  Intents  : $totalIntents  (ucp_intent_detected_requests_total)"
Write-Host "  Searches : $totalSearch   (ucp_search_requests_total)"
Write-Host "  CartAdds : $totalCart     (ucp_cart_add_items_total)"
Write-Host "  Checkouts: $totalCheckout (ucp_checkout_requests_total)"
Write-Host ""
Write-Host "Aguarde ~15s para o Prometheus raspar e verifique o OpsWatch." -ForegroundColor Cyan



