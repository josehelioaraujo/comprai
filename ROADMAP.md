# 🗺️ Roadmap — Comprai

## V049 — Integração BD no Fluxo de Compra

### 🔴 Alta Prioridade
- [ ] **Integrar CustomerRepository + OrderRepository no CheckoutService** — `INSERT customer + order + order_item + order_outbox` em 1 transação quando checkout é confirmado (hoje salva só no Redis)
- [ ] **Integrar PaymentRepository no PaymentService** — `INSERT payment + payment_outbox` quando pagamento é confirmado (Stripe/Efi/Mock)
- [ ] **Integrar PostgresFulfillmentRepository no FulfillmentSimulator** — cada etapa do pipeline persiste `fulfillment_event` no BD

### 🟡 Média Prioridade
- [ ] **INSERT order_history no status final** — FulfillmentService grava snapshot consolidado quando `delivered|cancelled|returned`
- [ ] **GET /api/orders?sessionId** — lê `order_history` no BD; substitui localStorage no front ("Meus pedidos")
- [ ] **GET /api/orders/{id}/fulfillment** — lê `fulfillment_event` append-only; base do polling no OrderTrackingCard (timeline ao vivo)
- [ ] **apps/mobile standalone** (porta 3003) — separar do apps/web; reverter `deploy-web.yml` para excluir paths mobile
- [ ] **PixCard/StripeCard sem onClose** — decidir se permite fechar após confirmação de pagamento

### 🟢 Melhorias
- [ ] **session no BD** — criar/recuperar `session` no PostgreSQL na chegada de cada requisição
- [ ] **search_log no BD** — `SearchProductsHandler` persiste cada busca em `search_log` para analytics
- [ ] **cart_snapshot no BD** — snapshot do carrinho para recuperação de abandono
- [ ] **webhook_event idempotência via BD** — Stripe/Efi webhook verifica `webhook_event` antes de processar
- [ ] **Polly retry + DLQ** — retry exponencial com jitter no `EmailNotificationWorker` + Dead Letter Queue no RabbitMQ
- [ ] **ucp.order.updated** — PATCH `/api/orders/{orderId}/status` + `PublishAsync` (nunca implementado)
- [ ] **Notificações push** — Service Worker + Web Push quando pedido mudar de status

---

## ✅ Concluído

### V048 — Persistência PostgreSQL + Outbox Pattern (2026-10-05)
- [x] `comprai-postgres` no `docker-compose.yml` e `deploy/docker-compose.yml`
- [x] `POSTGRES_PASSWORD` via GitHub Secret + `.env.example` + injeção VPS no `ci-cd.yml`
- [x] `db-migrate.yml` — workflow manual com `dry_run` e `target`
- [x] `V001__initial_schema.sql` — 17 tabelas + índices + índices parciais outbox (`TIMESTAMPTZ`, `IdempotencyKey UNIQUE`)
- [x] DbUp (`dbup-postgresql`) — migrations automáticas na startup + versionamento `V00X__descricao.sql`
- [x] `IDbConnectionFactory` / `NpgsqlConnectionFactory` — retorna `NpgsqlConnection` (`await using` seguro)
- [x] Dapper `2.1.35` + Npgsql `9.0.3`
- [x] `CustomerRepository` — upsert por email
- [x] `OrderRepository` — INSERT `order + order_item + order_outbox` em 1 transação atômica
- [x] `PaymentRepository` — INSERT `payment + payment_outbox` em 1 transação atômica
- [x] `PostgresFulfillmentRepository` — append-only, implementa `IFulfillmentRepository`
- [x] `OutboxRepository` — polling `FOR UPDATE SKIP LOCKED` nas 3 outboxes
- [x] `OrderOutboxWorker` / `PaymentOutboxWorker` → Kafka; `NotificationOutboxWorker` → RabbitMQ
- [x] Backoff exponencial: 30s → 60s → 120s → 300s (máx)
- [x] `Program.cs` — feature flag `UsarPostgres` + DI completo + `initializer.Initialize()` na startup

### V047 — Fix paymentDetail + CI verde (2026-10-04)
- [x] `IntentRenderer` propaga `detail` para `onPaymentConfirmed(orderId, detail)`
- [x] `useChat.handlePaymentConfirmed` persiste `paymentDetail` no `newOrder`
- [x] `ChatWindow.Props` TypeScript alinhado

### Backend + Observabilidade (V039–V041)
- [x] Fluxo UCP completo: Search → Cart → Checkout → Payment → Order
- [x] Plugins de catálogo: DummyJSON, Mock, Shopify, MercadoLivre
- [x] Pagamento: Mock, Stripe (cartão teste), Efi/Pix (aguardando cert produção)
- [x] Redis cache + HybridCache .NET 10
- [x] Kafka: tópicos canônicos + events de domínio
- [x] OTel pipeline: comprai-api → collector → Datadog + New Relic
- [x] OpsWatch: dashboard QA, observabilidade, pentest OWASP ZAP, K6, New Relic APM drill-down
- [x] Health checks condicionais por feature flag
- [x] SonarCloud: Quality Gate Passed, cobertura 97%+
- [x] CI/CD: unit-tests → integration-tests → sonar → deploy → smoke-tests

### 🌐 Web Chat Frontend (V042–V046)
- [x] Next.js 15 App Router + Tailwind + TypeScript
- [x] Fluxo UCP completo via chat: Search → Cart → Checkout → Pix/Cartão → Entrega
- [x] Layout Mobile-First `/mobile` — shell 420px smartphone
- [x] OrderList + drilldown completo (produtos, frete, total, pagamento, endereço, rastreio)
- [x] Bug carrinho 2ª compra corrigido — `clearSession + createSession` após pagamento
- [x] Stepper oculto em idle; badge carrinho; botão "Meus pedidos" contextual
- [x] FulfillmentAggregate + pipeline 6 etapas + 19 testes
- [x] localStorage com persistência entre sessões (order_history + fulfillment + tracking)

---

## Roadmap Futuro

### Mensageria e Resiliência
- [ ] Polly retry com jitter backoff (KafkaEventPublisher + EmailNotificationWorker)
- [ ] Dead Letter Queue (DLQ): `notifications.dlq` no RabbitMQ
- [ ] DLQ worker: consome, loga, alerta, reprocessa manualmente

### Canal Plugável
- [ ] WhatsApp Bot: Meta Cloud API + mesmo Hub SignalR + `IChannelPort`
- [ ] `WebChatChannelAdapter` + `WhatsAppChannelAdapter` via `[FromKeyedServices(ChannelType)]`

### Infraestrutura
- [ ] K3s + Helm + Argo CD — migração Docker Compose → K3s na VPS
- [ ] StressForge — gerador agnóstico de stress tests a partir de Swagger/OpenAPI

### AI/LLM
- [ ] GROQ_MODEL_SELECTOR — seletor de modelo no workflow code-review
- [ ] LLM_DIAGNOSTICO — `/api/ai/analyze` → Ollama + fallback Claude no OpsWatch
- [ ] HERMES_ORCHESTRATOR — NousResearch Hermes-3 via Ollama, agente autônomo com tools
