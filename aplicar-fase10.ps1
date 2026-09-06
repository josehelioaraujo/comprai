# Phase 10 - execute da raiz do projeto (onde esta comprai.sln)
param([string]$ProjectRoot = (Get-Location).Path)

Set-Location $ProjectRoot
$zipSrc = Join-Path $PSScriptRoot "src"

# ── 1. Copiar arquivos novos ─────────────────────────────────────────────────
New-Item -ItemType Directory -Force -Path "src\UcpAgent.SharedKernel\Events" | Out-Null
New-Item -ItemType Directory -Force -Path "src\UcpAgent.Infrastructure\Messaging" | Out-Null

$copies = @(
    @{ S = "UcpAgent.SharedKernel\Ports\IEventPublisher.cs";              D = "src\UcpAgent.SharedKernel\Ports\IEventPublisher.cs" }
    @{ S = "UcpAgent.SharedKernel\Events\OrderCreatedEvent.cs";           D = "src\UcpAgent.SharedKernel\Events\OrderCreatedEvent.cs" }
    @{ S = "UcpAgent.SharedKernel\Events\OrderStatusUpdatedEvent.cs";     D = "src\UcpAgent.SharedKernel\Events\OrderStatusUpdatedEvent.cs" }
    @{ S = "UcpAgent.SharedKernel\Events\SearchQueryLoggedEvent.cs";      D = "src\UcpAgent.SharedKernel\Events\SearchQueryLoggedEvent.cs" }
    @{ S = "UcpAgent.Infrastructure\Messaging\KafkaEventPublisher.cs";    D = "src\UcpAgent.Infrastructure\Messaging\KafkaEventPublisher.cs" }
    @{ S = "UcpAgent.Infrastructure\Messaging\NullEventPublisher.cs";     D = "src\UcpAgent.Infrastructure\Messaging\NullEventPublisher.cs" }
    @{ S = "UcpAgent.Infrastructure\Checkout\RedisCheckoutAdapter.cs";    D = "src\UcpAgent.Infrastructure\Checkout\RedisCheckoutAdapter.cs" }
)

foreach ($c in $copies) {
    $src = (Resolve-Path (Join-Path $zipSrc $c.S)).Path
    $dst = (Join-Path $ProjectRoot $c.D)
    if ($src -ne $dst) {
        Copy-Item $src $dst -Force
        Write-Host "Copiado: $($c.D)"
    } else {
        Write-Host "Ja no lugar: $($c.D)"
    }
}

# ── 2. Patch SearchProductsHandler.cs ───────────────────────────────────────
$handlerPath = "src\UcpAgent.Application\Search\SearchProductsHandler.cs"
$h = [System.IO.File]::ReadAllText($handlerPath)

if ($h -notmatch "IEventPublisher") {

    $evUsing = "using UcpAgent.SharedKernel.Events;"
    if ($h -notmatch [regex]::Escape($evUsing)) {
        $h = $evUsing + [Environment]::NewLine + $h
    }

    # Adicionar IEventPublisher no construtor
    $h = [regex]::Replace($h,
        '(IEnumerable<IProductCatalogPort>\s+\w+)\)',
        '$1, IEventPublisher events)')

    # Bloco publish antes do return final
    $publish = [Environment]::NewLine +
        '        await events.PublishAsync(' + [Environment]::NewLine +
        '            "search.query.logged",' + [Environment]::NewLine +
        '            new SearchQueryLoggedEvent(' + [Environment]::NewLine +
        '                request.Query, request.Category,' + [Environment]::NewLine +
        '                request.MinPrice, request.MaxPrice,' + [Environment]::NewLine +
        '                aggregated.Count, DateTime.UtcNow),' + [Environment]::NewLine +
        '            cancellationToken);' + [Environment]::NewLine + [Environment]::NewLine

    $h = [regex]::Replace($h,
        '(        return new SearchResult\()',
        $publish + '        return new SearchResult(')

    [System.IO.File]::WriteAllText($handlerPath, $h, [System.Text.Encoding]::UTF8)
    Write-Host "Atualizado: SearchProductsHandler.cs"
} else {
    Write-Host "Sem mudanca: SearchProductsHandler.cs (IEventPublisher ja existe)"
}

# ── 3. Patch Program.cs ──────────────────────────────────────────────────────
$programPath = "src\UcpAgent.Api\Program.cs"
$p = [System.IO.File]::ReadAllText($programPath)

if ($p -notmatch "IEventPublisher") {

    $u1 = "using UcpAgent.Infrastructure.Messaging;"
    $u2 = "using UcpAgent.SharedKernel.Ports;"
    if ($p -notmatch [regex]::Escape($u1)) { $p = $u1 + [Environment]::NewLine + $p }
    if ($p -notmatch [regex]::Escape($u2)) { $p = $u2 + [Environment]::NewLine + $p }

    $block = [Environment]::NewLine +
        '// IEventPublisher' + [Environment]::NewLine +
        'if (features.GetValue<bool>("UsarKafka"))' + [Environment]::NewLine +
        '{' + [Environment]::NewLine +
        '    var bootstrapServers = builder.Configuration["Kafka:BootstrapServers"] ?? "localhost:9092";' + [Environment]::NewLine +
        '    builder.Services.AddSingleton<IEventPublisher>(_ => new KafkaEventPublisher(bootstrapServers));' + [Environment]::NewLine +
        '}' + [Environment]::NewLine +
        'else' + [Environment]::NewLine +
        '{' + [Environment]::NewLine +
        '    builder.Services.AddSingleton<IEventPublisher, NullEventPublisher>();' + [Environment]::NewLine +
        '}' + [Environment]::NewLine

    $p = [regex]::Replace($p,
        '(var app = builder\.Build\(\);)',
        $block + '$1')

    [System.IO.File]::WriteAllText($programPath, $p, [System.Text.Encoding]::UTF8)
    Write-Host "Atualizado: Program.cs"
} else {
    Write-Host "Sem mudanca: Program.cs (IEventPublisher ja existe)"
}

# ── 4. CHANGELOG.md ──────────────────────────────────────────────────────────
$changelogPath = "CHANGELOG.md"
$date = (Get-Date -Format "yyyy-MM-dd")
$entry = "## [1.0.0] - $date" + [Environment]::NewLine + [Environment]::NewLine +
    "### Added" + [Environment]::NewLine +
    "- **Phase 10 - Kafka Messaging**: porta IEventPublisher no SharedKernel" + [Environment]::NewLine +
    "- KafkaEventPublisher com Confluent.Kafka (Acks=Leader, timeout 5s)" + [Environment]::NewLine +
    "- NullEventPublisher para ambientes sem Kafka (UsarKafka=false)" + [Environment]::NewLine +
    "- Evento OrderCreatedEvent publicado no topico order.created apos checkout" + [Environment]::NewLine +
    "- Evento SearchQueryLoggedEvent publicado no topico search.query.logged apos busca" + [Environment]::NewLine +
    "- Feature flag UsarKafka + config Kafka:BootstrapServers no appsettings" + [Environment]::NewLine + [Environment]::NewLine

$existing = [System.IO.File]::ReadAllText($changelogPath)
[System.IO.File]::WriteAllText($changelogPath, $entry + $existing, [System.Text.Encoding]::UTF8)
Write-Host "Atualizado: CHANGELOG.md"

Write-Host ""
Write-Host "Pronto! Rode agora:"
Write-Host "  dotnet build comprai.sln --configuration Release"
