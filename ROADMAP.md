# 🗺️ Roadmap — Comprai

## 🔜 V052 — Próxima versão

### 🔴 Alta Prioridade
- [ ] **apps/mobile standalone** (porta 3003) — separar do `apps/web`; `deploy-mobile.yml` independente com trigger `apps/mobile/**`; reverter `deploy-web.yml` para excluir paths mobile

### 🟡 Média Prioridade
- [ ] **Integrar CustomerRepository + OrderRepository no CheckoutService** — `INSERT customer + order + order_item + order_outbox` em 1 transação quando checkout é confirmado (hoje salva só no Redis)
- [ ] **Integrar PaymentRepository no PaymentService** — `INSERT payment + payment_outbox` quando pagamento é confirmado (Stripe/Efi/Mock)
- [ ] **Integrar PostgresFulfillmentRepository no FulfillmentSimulator** — cada etapa do pipeline persiste `fulfillment_event` no BD
- [ ] **INSERT order_history no status final** — FulfillmentService grava snapshot consolidado quando `delivered|cancelled|returned`

### 🟢 Melhorias
- [ ] **session no BD** — criar/recuperar `session` no PostgreSQL na chegada de cada requisição
- [ ] **search_log no BD** — `SearchProductsHandler` persiste cada busca em `search_log`
- [ ] **cart_snapshot no BD** — snapshot do carrinho para recuperação de abandono
- [ ] **webhook_event idempotência via BD** — Stripe/Efi webhook verifica `webhook_event` antes de processar

---

## ✅ Concluído

### V051 — Testes de Integração + F5 Polling + Auto-dismiss (2026-10-06)
- [x] `[FromServices]` em `OrdersEndpoints.cs` — corrige 5 testes com `InvalidOperationException: Body was inferred`
- [x] `RemoveAll(typeof(IPaymentPort))` em `EfiPayFactory` — corrige `Sequence contains more than one matching element`
- [x] `using DependencyInjection.Extensions` em `EfiPaymentIntegrationTest`
- [x] `[Fact(Skip)]` em `PollyResilienceIntegrationTest` — rede externa bloqueada no CI
- [x] `getFulfillmentTimeline(orderId)` → `GET /api/orders/{id}/fulfillment` em `api.ts`
- [x] `startFulfillmentPolling` — polling real a cada 5s; fallback simulação local (`UsarPostgres=false`)
- [x] `handleViewOrders` — API-first + mescla localStorage + fallback
- [x] Opção A: auto-dismiss `PixCard`/`StripeCard` 800ms após confirmação em todos os consumers (`IntentRenderer`, `ChatWindow`, `CompraiWidget`, `MobileIntentRenderer`)

### V049/V050 — Fix CI e Testes (2026-10-05)
- [x] `ResilienceExtensions.cs`: `ShouldHandle` restaurado (HTTP 5xx via `HandleResult`)
- [x] `UseMiddleware<IdempotencyMiddleware>` duplicado removido
- [x] `HealthApiFactory` com `ConfigureAppConfiguration`
- [x] Testes de resiliência com `HttpRequestException` determinístico

### V048 — Persistência PostgreSQL + Outbox Pattern (2026-10-05)
- [x] `comprai-postgres` no `docker-compose.yml` e `deploy/docker-compose.yml`
- [x] `POSTGRES_PASSWORD` via GitHub Secret + `.env.example`
- [x] `db-migrate.yml` — workflow manual `dry_run` + `target`
- [x] `V001__initial_schema.sql` — 17 tabelas, índices, índices parciais outbox
- [x] DbUp — migrations automáticas na startup
- [x] `IDbConnectionFactory` / `NpgsqlConnectionFactory`
- [x] `CustomerRepository`, `OrderRepository`, `PaymentRepository`, `PostgresFulfillmentRepository`, `OutboxRepository`
- [x] `OrderOutboxWorker` / `PaymentOutboxWorker` → Kafka; `NotificationOutboxWorker` → RabbitMQ
- [x] Backoff exponencial: 30s → 60s → 120s → 300s

### V047 — Fix paymentDetail + CI verde (2026-10-04)
- [x] `paymentDetail` propagado via `IntentRenderer` → `onPaymentConfirmed` → `newOrder`

### V044–V047 — Mobile UX + Fulfillment (2026-10-01 a 2026-10-04)
- [x] `apps/mobile/` — Next.js independente porta 3003
- [x] Layout Mobile-First `/mobile` — shell 420px smartphone, stepper bottom nav
- [x] `MobileIntentRenderer`, `MobileProductCarousel`, `MobileChatBubble/Window/Footer`
- [x] `FulfillmentAggregate` (Event Sourcing append-only) + `FulfillmentSimulator` + pipeline 6 etapas
- [x] `OrderTrackingCard` — timeline 7 etapas, rastreio colapsável
- [x] OrderList + drilldown completo
- [x] Bug carrinho 2ª compra corrigido

### V042–V043 — Web Chat Frontend (2026-09-29 a 2026-09-30)
- [x] Next.js 15 App Router + Tailwind v4 + TypeScript
- [x] Fluxo UCP completo: Search → Cart → Checkout → Pix/Cartão → Entrega
- [x] `ProductCarousel`, `CartCard`, `CheckoutCard`, `PixCard`, `StripeCard`, `OrderTrackingCard`
- [x] `UcpProgressBar`, `Sidebar`, dark/light mode
- [x] Widget embeddável, painel de logs 🔍
- [x] `deploy-web.yml` — Docker build + push GHCR + deploy VPS

### Backend + Observabilidade (V033–V041)
- [x] Fluxo UCP completo: Search → Cart → Checkout → Payment → Order
- [x] Plugins: DummyJSON, Mock, Shopify, MercadoLivre, VTEX, OpenFoodFacts
- [x] Pagamento: Mock, Stripe, Efi/Pix
- [x] Redis cache + HybridCache .NET 10
- [x] Kafka: tópicos canônicos + domain events
- [x] OTel pipeline: `comprai-api` → OTel Collector → Datadog + New Relic simultaneamente
- [x] OpsWatch: QA, observabilidade, K6, New Relic APM drill-down
- [x] SonarCloud Quality Gate Passed, cobertura 97%+
- [x] CI/CD: unit-tests → integration-tests → sonar → deploy → smoke-tests

---

## Roadmap Futuro

### AI/LLM
- [ ] **LLM_DIAGNOSTICO** — `/api/ai/analyze` → Ollama + fallback Claude; botão "Analisar" no OpsWatch
- [ ] **GROQ_MODEL_SELECTOR** — seletor de modelo no `workflow_dispatch` do code-review
- [ ] **HERMES_ORCHESTRATOR** — NousResearch Hermes-3 via Ollama; agente autônomo com tools
- [ ] **Ollama na VPS** — `docker run ollama/ollama` + pull `gemma3:latest`

### Infraestrutura
- [ ] **K3s + Helm + Argo CD** — migração Docker Compose → K3s na VPS; repo `comprai-infra`
- [ ] **StressForge** — gerador agnóstico de stress tests a partir de Swagger/OpenAPI
- [ ] **StatusForge** — status page self-hosted

### Canais
- [ ] **WhatsApp Bot** — Meta Cloud API + mesmo Hub SignalR + `IChannelPort`; reutiliza 100% do core UCP
- [ ] **Extensão Chrome** — Price Watcher + Universal Cart + Intent Bar
- [ ] **Canal Teams** — Bot Framework SDK

### Mensageria e Resiliência
- [ ] **Polly retry + DLQ** — backoff jitter no `EmailNotificationWorker` + Dead Letter Queue no RabbitMQ
- [ ] **ucp.order.updated** — PATCH `/api/orders/{orderId}/status` + `PublishAsync`
- [ ] **Notificações push** — Service Worker + Web Push por mudança de status
