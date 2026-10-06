# Changelog

## [1.0.51] — V051 — 2026-10-06

### Fix — Testes de Integração (CI verde)

- **`[FromServices]`** adicionado em `OrdersEndpoints.cs` nos dois `MapGet` (`GET /api/orders` e `GET /api/orders/{id}/fulfillment`) — corrige `InvalidOperationException: Body was inferred` em 5 testes
- **`RemoveAll(typeof(IPaymentPort))`** em `EfiPayFactory` no lugar de `SingleOrDefault` — corrige `InvalidOperationException: Sequence contains more than one matching element` quando há 2 registros de `IPaymentPort` (keyed + normal)
- **`using Microsoft.Extensions.DependencyInjection.Extensions`** adicionado em `EfiPaymentIntegrationTest` — `RemoveAll` é extension method, não está no using base
- **`[Fact(Skip)]`** em `PollyResilienceIntegrationTest` — requer rede externa (`dummyjson.com`) bloqueada no CI; teste preservado para execução local

### Feat — F5: Fulfillment Polling Real + Orders API-First (Front)

- **`api.ts`** — `getFulfillmentTimeline(orderId)` → `GET /api/orders/{id}/fulfillment`; `getOrders(sessionId)` normalizado (suporta array direto ou `{ orders: [] }`)
- **`useChat.ts`** — `startFulfillmentPolling`: polling real a cada 5s (máx 60 tentativas); fallback automático para simulação local quando API retorna vazio (`UsarPostgres=false`)
- **`handleViewOrders`** — API-first: `getOrders(sessionId)` → mescla dados de fulfillment do localStorage → fallback localStorage completo se API falhar

### Feat — Opção A: Auto-dismiss PixCard e StripeCard pós-confirmação

- `IntentRenderer.tsx` — recebe `onDismissMessage` e dispara `setTimeout(dismiss, 800ms)` após `onPaymentConfirmed` nos cases `payment_pix`, `payment_mock` e `payment_card`
- `ChatWindow.tsx` — repassa `onDismissMessage` para `IntentRenderer`
- `chat/page.tsx` — desestrutura `handleDismissMessage` do `useChat` e passa para `ChatWindow`
- `CompraiWidget.tsx` — alinhado com `onDismissMessage`
- `MobileIntentRenderer.tsx` — auto-dismiss Pix/Stripe via `onClose` com `setTimeout 800ms`

#### Commits V051
| Hash | Descrição |
|------|-----------|
| `d8dddd93` | `[FromServices]`, `RemoveAll`, `[Fact(Skip)]` nos testes |
| `c3d92916` | `using DependencyInjection.Extensions` em `EfiPaymentIntegrationTest` |
| `73db5650` | F5: `getFulfillmentTimeline` + `getOrders` API-first + `startFulfillmentPolling` |
| `ec768773` | Opção A: auto-dismiss `IntentRenderer` + `ChatWindow` |
| `c530a425` | `chat/page.tsx` com `onDismissMessage` |
| `a58a9eed` | `CompraiWidget` + `MobileIntentRenderer` alinhados |

---

## [1.0.49] — V049/V050 — 2026-10-05

### Fix — CI e Testes de Integração

- `ResilienceExtensions.cs`: `using Polly` mantido (ext methods); `using Polly.CircuitBreaker` removido; `ShouldHandle` restaurado (HTTP 5xx precisa de `HandleResult`)
- `UseMiddleware<IdempotencyMiddleware>` duplicado removido do `Program.cs`
- `HealthApiFactory` com `ConfigureAppConfiguration` — `UsarMockDados=true`, todas features infra `false`
- `using Microsoft.Extensions.Configuration` adicionado no `HealthStatusEndpointTests`
- `ResilienceTests` CB usa `HttpRequestException` (determinístico); `timeoutSeconds` default 30s
- `ci-cd.yml` limpo (removidos logs de diagnóstico)

---

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

#### Dapper + Factory
- **`IDbConnectionFactory`** / **`NpgsqlConnectionFactory`** — retorna `NpgsqlConnection` (implementa `IAsyncDisposable` — `await using` funciona)
- Dapper `2.1.35` + Npgsql `9.0.3`

#### Repositórios
- **`CustomerRepository`** — `UpsertAsync` com `ON CONFLICT (email) DO UPDATE`
- **`OrderRepository`** — `SaveAsync`: INSERT `order` + `order_item` + `order_outbox` em 1 transação atômica
- **`PaymentRepository`** — `ConfirmAsync`: INSERT `payment` + `payment_outbox` em 1 transação atômica
- **`PostgresFulfillmentRepository`** — implementa `IFulfillmentRepository`; append-only `fulfillment_event`
- **`OutboxRepository`** — polling das 3 outboxes com `FOR UPDATE SKIP LOCKED`

#### Outbox Workers
- **`OrderOutboxWorker`** / **`PaymentOutboxWorker`** → Kafka; **`NotificationOutboxWorker`** → RabbitMQ
- `PeriodicTimer(5s)` · Backoff exponencial: `30s → 60s → 120s → 300s (máx)`

---

## [1.0.47] — V047 — 2026-10-04

### Fix
- **paymentDetail propagado corretamente ao Order** — `IntentRenderer` e `ChatWindow` passam `detail?` de `PixCard`/`StripeCard` para `handlePaymentConfirmed`
- `useChat.handlePaymentConfirmed` recebe `paymentDetail?` e persiste em `newOrder.paymentDetail`

---

## [1.0.47] — V046/V047 — Mobile UX (2026-10-04)

### Fixes e Melhorias
- OrderList simplificado — lista nº pedido + valor; drilldown com `OrderTrackingCard`
- Bug carrinho 2ª compra corrigido — `clearSession + createSession` após pagamento
- Botão × universal em `OrderTrackingCard` e `OrderList`
- Stepper oculto em `idle`; dismiss `order_list` reseta step para `idle`
- `handleViewOrders` usa `upsertBotMessage` — sem duplicação de cards
- Rastreio unificado em accordion colapsável
- Forma de pagamento: label + detalhe (chave Pix truncada ou `•••• 4242`)

---

## [v1.0.x] — V044 — Mobile-First UI + Fulfillment Domain (2026-10-01)

### Adicionado
- `apps/mobile/` — app Next.js independente porta 3003
- Layout Mobile-First `/mobile` — shell 420px smartphone
- `MobileUcpHeader`, `MobileUcpProgressBar`, `MobileChatBubble/Window/Footer`
- `MobileProductCard` + `MobileProductCarousel`
- `MobileIntentRenderer`
- `FulfillmentAggregate` (Event Sourcing append-only) + `FulfillmentSimulator` + `RedisFulfillmentRepository`
- `OrderTrackingCard` — timeline 7 etapas + código de rastreio
- Frete grátis acima de R$ 1.000

---

## [v1.0.x] — V043 — Web Chat Front-end (2026-09-30)

### Adicionado
- `comprai-web` — Next.js 15 App Router + Tailwind + TypeScript
- Fluxo UCP completo: Search → Cart → Checkout → Payment → Order
- `ProductCarousel`, `CartCard`, `CheckoutCard`, `PixCard`, `StripeCard`, `OrderTrackingCard`
- `UcpProgressBar`, `Sidebar`, dark/light mode
- Widget embeddável 🛍️
- Painel de logs 🔍

---

## [V041] — Health Checks + Kafka (2026-09-30)
- `/health/live` e `/health/ready` com checks condicionais por feature flag
- Kafka ativado: `comprai-kafka:9094`, tópicos canônicos em `UcpTopics.cs`

---

## [V040] — OpsWatch New Relic APM Drill-down (2026-09-29)
- Cards Apdex/Error%/Req/min/p95 clicáveis com breakdown interativo
- Gauge SVG Apdex, sparkline com área gradiente e tooltip hover
- Seletor de janela temporal: 5m → 24h

---

## [V039] — New Relic OTel Pipeline (2026-09-28)
- Pipeline OTel: `comprai-api` → HttpProtobuf → `otel-collector` → New Relic
- 93 spans confirmados no APM & Services

---

## [V038 e anteriores]
Consulte o histórico de commits no GitHub.
