# Changelog

## [1.0.48] — V048 — 2026-10-05

### Feat — Persistência PostgreSQL + Outbox Pattern (Dapper + DbUp)

#### Infraestrutura
- **`comprai-postgres`** adicionado ao `docker-compose.yml` e `deploy/docker-compose.yml` — `postgres:16-alpine`, porta 5432, `TZ=UTC`/`PGTZ=UTC`, volume `postgres_data`, healthcheck `pg_isready`
- **`POSTGRES_PASSWORD`** via GitHub Secret — injeção no `.env` da VPS via `grep -q + sed -i` no `ci-cd.yml`; `.env.example` criado com todas as variáveis documentadas
- **`db-migrate.yml`** — workflow manual com `dry_run` e `target` (ex: rodar só até V002)

#### Schema — 17 tabelas
- **`V001__initial_schema.sql`** em `src/UcpAgent.Infrastructure/Persistence/Migrations/` — embutido no assembly como `EmbeddedResource`
- Grupos: `customer`/`customer_address`/`session` · `order`/`order_item`/`order_history` · `fulfillment_event` (append-only) · `payment`/`refund` · `webhook_event` · `order_outbox`/`payment_outbox`/`notification_outbox` · `notification` · `search_log`/`cart_snapshot`/`idempotency_key`
- `TIMESTAMPTZ` em todos os campos de data/hora; `IdempotencyKey UNIQUE` nas tabelas retentáveis; índices parciais `WHERE status IN ('pending','failed')` nas 3 outboxes

#### Migrations — DbUp
- **`dbup-postgresql 5.0.x`** adicionado ao `.csproj` — substitui initializer manual
- **`CompraiDbInitializer`** — `EnsureDatabase` + `WithScriptsEmbeddedInAssembly` + `WithTransactionPerScript` + `LogToConsole`
- Roda na startup via `initializer.Initialize()` após `app.UseCors()`
- Novo script = novo `V00X__descricao.sql` — DbUp aplica automaticamente no próximo deploy

#### Dapper + Factory
- **`IDbConnectionFactory`** / **`NpgsqlConnectionFactory`** — retorna `NpgsqlConnection` (implementa `IAsyncDisposable` — `await using` funciona)
- Dapper `2.1.35` + Npgsql `9.0.3` adicionados ao `UcpAgent.Infrastructure.csproj`

#### Repositórios
- **`CustomerRepository`** — `UpsertAsync` com `ON CONFLICT (email) DO UPDATE`
- **`OrderRepository`** — `SaveAsync`: INSERT `order` + `order_item` + `order_outbox` em **1 transação atômica**
- **`PaymentRepository`** — `ConfirmAsync`: INSERT `payment` + `payment_outbox` em **1 transação atômica**
- **`PostgresFulfillmentRepository`** — implementa `IFulfillmentRepository`; append-only `fulfillment_event`; `Reconstitute` do agregado via histórico
- **`OutboxRepository`** — polling das 3 outboxes com `FOR UPDATE SKIP LOCKED`; `MarkSent/Failed` com backoff exponencial

#### Outbox Workers
- **`OrderOutboxWorker`** — `PeriodicTimer(5s)` → Kafka (`ucp.order.*`)
- **`PaymentOutboxWorker`** — `PeriodicTimer(5s)` → Kafka (`ucp.payment.*`)
- **`NotificationOutboxWorker`** — `PeriodicTimer(5s)` → RabbitMQ (`notifications.*`)
- Backoff exponencial: `30s → 60s → 120s → ... → 300s (máx)`

#### Program.cs
- Feature flag `UsarPostgres` adicionada
- DI: `IDbConnectionFactory`, `CompraiDbInitializer`, 4 repositórios, `IFulfillmentRepository`, 3 workers (condicionados por `UsarKafka`/`UsarRabbitMQ`)


## [1.0.48] — V047 — 2026-10-04

### Fix
- **paymentDetail propagado corretamente ao Order** — `IntentRenderer` e `ChatWindow` agora passam `detail?` de `PixCard`/`StripeCard` para `handlePaymentConfirmed`
- `useChat.handlePaymentConfirmed` recebe `paymentDetail?` e persiste em `newOrder.paymentDetail`
- CI estava verde (confirmado via Actions API) — CI/CD e Deploy Web ✅ nos commits `64c2a812` e `25adf96c`


## [1.0.47] — 2026-10-04

### Mobile UX — Meus Pedidos, Drilldown e Qualidade Visual

#### Fixes
- **OrderList simplificado**: lista exibe apenas nº do pedido + valor; clique abre drilldown com `OrderTrackingCard`
- **Drilldown completo**: produtos (nome/qtd/valor), frete, total, forma de pagamento + detalhe (Pix/cartão), endereço de entrega, timeline de rastreio colapsável
- **Dismiss sem bolha**: `handleViewOrders` não emite texto na bolha — card exibido diretamente
- **Bug carrinho 2ª compra**: `clearSession()` + `createSession()` após pagamento confirmado — `cartSnapshot` capturado antes de zerar
- **Botão × universal**: `OrderTrackingCard` e `OrderList` com botão fechar em todos os estados (incluindo Entregue)
- **Stepper oculto em idle**: `MobileUcpProgressBar` retorna `null` quando `step === 'idle'`
- **Stepper some ao fechar**: dismiss do card `order_list` reseta `step → idle`; dismiss de `order_status` mantém o step
- **Sem duplicação de cards**: `handleViewOrders` usa `upsertBotMessage` — clicar várias vezes atualiza o mesmo card
- **Botão "Buscar produto" removido**: redundante com o input de chat
- **Botão "Nova busca" removido**: redundante com o input de chat
- **Botão "Meus pedidos"**: aparece no footer em `idle`/`search` quando há pedidos anteriores (via `hasPreviousOrder`)
- **Rastreio unificado**: dois botões (Histórico + Rastreio) unificados em "📦 Rastreio da entrega ▼" colapsável
- **Forma de pagamento**: label renomeado + exibe detalhe (chave Pix truncada ou `•••• 4242`)
- **Deploy Web**: workflow cobre `apps/web/**` completo (incluindo mobile) após remoção de exclusões

#### Arquivos alterados
`useChat.ts`, `ucp.ts`, `OrderTrackingCard.tsx`, `MobileIntentRenderer.tsx`, `MobileChatBubble.tsx`, `MobileChatWindow.tsx`, `MobileChatFooter.tsx`, `MobileUcpProgressBar.tsx`, `PixCard.tsx`, `StripeCard.tsx`, `apps/web/src/app/mobile/page.tsx`, `.github/workflows/deploy-web.yml`

## [v1.0.x] — V044 — Mobile-First UI + Fulfillment Domain (2026-10-01)

### ✨ Adicionado
- **`apps/mobile/`** — app Next.js independente porta 3003, container `comprai-mobile`, imagem `ghcr.io/josehelioaraujo/comprai-mobile`
- **Layout Mobile-First** — rota `/mobile` com shell 420px smartphone, stepper de progresso bottom nav sempre visível
- `MobileUcpHeader` — logo mark SVG emerald + wordmark + versão discreta no topo
- `MobileUcpProgressBar` — navbar bottom com 5 steps, badge de quantidade no Carrinho, clique em Entrega scrolla para tracking
- `MobileChatBubble`, `MobileChatWindow`, `MobileChatFooter` — chat mobile com atalhos contextuais por step
- `MobileProductCard` + `MobileProductCarousel` — cards w-36 + setas + drag mouse/touch
- `MobileIntentRenderer` — switch de intent para renderização mobile
- **Fulfillment Domain** — `FulfillmentAggregate` (Event Sourcing append-only), `FulfillmentSimulator` (IHostedService), `RedisFulfillmentRepository`, 10 status de entrega
- `OrderTrackingCard` — timeline fulfillment com 7 etapas, código de rastreio, histórico expansível
- **Atalhos contextuais** — sugestões no footer mudam por step: idle/search → Buscar; cart/checkout → Finalizar; order → Nova busca + Meus pedidos
- **Flag simulação** — `NEXT_PUBLIC_FULFILLMENT_SIMULATION` (frontend) + `Features__UsarFulfillmentSimulator` (backend)
- `deploy-mobile.yml` — workflow CI/CD independente, trigger `apps/mobile/**`, bump próprio
- Frete grátis acima de R$ 1.000 — `calcShipping()` retorna 0 automaticamente

### 🔧 Corrigido
- **Total zerado** no CheckoutCard/PixCard — `confirmedTotalRef` evita stale closure do `setSession` assíncrono
- **Mensagem duplicada** ao adicionar produto — `upsertBotMessage` com `intent: 'cart_add'`
- **Duplicação no carrinho** — `addingProductsRef` (Set) no `useChat` + estado `adding` local no `MobileProductCard`
- **IdempotencyMiddleware** — sempre ativo: Redis quando disponível, `ConcurrentDictionary` em memória como fallback (TTL 30min). Garantia de consistência independente de infraestrutura
- **Botão "Meus pedidos"** — não chama backend; scrolla para `OrderTrackingCard` existente na conversa
- **Drag carrossel** — `scrollSnapType` removido durante drag, restaurado no mouseup
- **CEP** — máscara `XXXXX-XXX`, campo em linha própria com label
- **Complemento** — `autocomplete="off"`, linha própria, hint "Opcional"
- **CheckoutCard** — botão X para fechar, bloqueio de pagamento duplo com estado `paying`
- **FulfillmentSimulator** — removida dependência circular de `UcpAgent.Application`; usa `IFulfillmentRepository` direto
- **Layout mobile** — `height: 100dvh` + `min-h-0` no chat para stepper e footer sempre visíveis

### 🚀 Deploy
- Porta `3003` — container `comprai-mobile` na VPS
- `deploy-mobile.yml` com `git pull --rebase` antes do bump para evitar race condition

---

## [v1.0.x] — V043 — Web Chat Front-end (2026-09-30)

### ✨ Adicionado
- **comprai-web** — front-end Next.js 15 App Router + Tailwind + TypeScript
- Fluxo UCP completo via Web Chat: Search → Cart → Checkout → Payment → Order
- `ProductCarousel` — cards com setas ‹ › de navegação, sem label de fonte
- `CartCard` — itens com +/- quantidade (mínimo 1), frete via radio button, subtotal/frete/total
- `CheckoutCard` — resumo do pedido + seleção de método de pagamento (Pix / Cartão)
- `PixCard` — QR Code + countdown 15min + cópia Pix Copia e Cola (fallback `execCommand` em HTTP)
- `StripeCard` — formulário inline de cartão de crédito
- `OrderTrackingCard` — timeline de status do pedido
- `UcpProgressBar` — barra horizontal com steps, badge de quantidade no 🛒, clique abre popup
- `Sidebar` — colapsável (‹ ›), logo clicável com modal sobre o UCP, dark/light mode toggle
- Dark/Light mode com tokens CSS (`var(--bg)`, `var(--text)`, etc.) persistido em `localStorage`
- Versão `v1.0.x` + short SHA do commit na sidebar (bump automático a cada deploy)
- Widget embeddável — botão flutuante 🛍️ com badge, abre painel de chat em qualquer site
- Popup do carrinho na sidebar — ícone 🛒 + badge quantidade + total + botão Fechar pedido
- Botão "🛒 Fechar pedido" fixo no input quando há itens no carrinho
- Painel de logs 🔍 — request/response com erros em vermelho
- Responsivo — sidebar oculta em mobile, header mobile com badge do carrinho

### 🔧 Corrigido
- Crash `CheckoutCard` — `.map()` em `items` undefined do backend
- Payload `createPayment` — `{ amount: "string", currency: "BRL", method: { provider, cardToken, pixKey } }`
- `amount` como string `toFixed(2)` para evitar problema de precisão float
- Cópia Pix em HTTP — fallback `execCommand` (clipboard API bloqueada sem HTTPS)
- `CartCard` sem botão duplicado — "Finalizar compra" só no form, "Fechar pedido" no input
- Sincronização +/- carrinho com sidebar via `onQuantityChange`
- Dockerfile: ARGs declarados antes do ENV para `NEXT_PUBLIC_GIT_SHA` chegar no build

### 🚀 Deploy
- GitHub Actions `deploy-web.yml` — bump automático de patch version + build + push GHCR + deploy VPS
- Porta `3002` (3000 ocupada pelo Docker proxy)
- Rede `comprai-network`

---

## [v1.0.x] — V042 — Web Chat Scaffold (2026-09-29)

### ✨ Adicionado
- Scaffold Next.js 15 com App Router, Tailwind v3, TypeScript
- `useChat` — estado central do fluxo UCP
- `lib/api.ts` — fetch com `X-Idempotency-Key`
- `lib/session.ts` — sessionId localStorage TTL 30min
- `IntentRenderer` — switch intent → componente

---

## [V041] — Health Checks + Kafka (2026-09-30)

### ✨ Adicionado
- `/health/live` e `/health/ready` com checks condicionais por feature flag
- Kafka ativado: `comprai-kafka:9094`, tópicos canônicos em `UcpTopics.cs`
- Events: `SearchQueryLoggedEvent`, `CartItemAddedEvent`, `OrderCreatedEvent`

### 🔧 Corrigido
- `RedisHealthCheck` sem exception sem Redis
- `OllamaHealthCheck` com timeout 2s
- TCP checks assíncronos (era `WaitOne` bloqueante)

---

## [V040] — OpsWatch New Relic APM Drill-down (2026-09-29)

### ✨ Adicionado
- Cards Apdex/Error%/Req/min/p95 clicáveis com breakdown interativo
- Gauge SVG Apdex com ponteiro dinâmico
- Sparkline com área gradiente e tooltip hover
- Seletor de janela temporal: 5m → 24h
- Bucket Frustrated: sub-tabelas Lentos e Erros

---

## [V039] — New Relic OTel Pipeline (2026-09-28)

### ✨ Adicionado
- Pipeline OTel: `comprai-api` → HttpProtobuf → `otel-collector` → New Relic
- 93 spans confirmados no APM & Services

---

## [V038 e anteriores]
Consulte o histórico de commits no GitHub.
