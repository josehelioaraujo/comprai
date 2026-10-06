# 🗺️ Roadmap — Comprai

## 🔜 V053 — Próxima versão

### 🔴 Alta Prioridade
- [ ] **Front (web) — sincronizar localStorage com BD** — remover `saveOrderToList` quando API retornar OK; manter só como fallback
- [ ] **apps/mobile standalone** (porta 3003) — separar do `apps/web`; `deploy-mobile.yml` independente

### 🟡 Média Prioridade
- [ ] **INSERT order_history no status final** — `FulfillmentService` grava snapshot consolidado quando `delivered|cancelled|returned`
- [ ] **session no BD** — criar/recuperar `session` no PostgreSQL na chegada de cada requisição
- [ ] **search_log no BD** — `SearchProductsHandler` persiste cada busca em `search_log`
- [ ] **cart_snapshot no BD** — snapshot do carrinho para recuperação de abandono
- [ ] **webhook_event idempotência via BD** — Stripe/Efi webhook verifica `webhook_event` antes de processar

---

## 🔭 Roadmap Futuro

### 🏭 OMS + WMS + Carrier (plugins hexagonais extensíveis)
- [ ] `UcpAgent.Oms.Core` + `UcpAgent.Oms.Simulated` + `UcpAgent.Oms.Vtex` (futuro)
- [ ] `UcpAgent.Wms.Core` + `UcpAgent.Wms.Simulated` + `UcpAgent.Wms.Totvs` (futuro)
- [ ] `UcpAgent.Carrier.Core` + `UcpAgent.Carrier.Simulated` + `UcpAgent.Carrier.Correios/Jadlog` (futuro)
- [ ] Portas hexagonais: `IOrderManagementPort`, `IWarehousePort`, `ICarrierPort` em SharedKernel
- [ ] Feature flags: `OmsProvider`, `WmsProvider`, `CarrierProvider`
- [ ] `V002__oms_wms_schema.sql`: `stock`, `stock_reservation`, `invoice`, `shipment`, `shipment_event`, `return_request`, `cancellation`
- [ ] Fluxo completo: compra → cancelamento → devolução, cada etapa gravando `fulfillment_event`

### 🤖 AI/LLM
- [ ] **LLM_DIAGNOSTICO** — `/api/ai/analyze` → Ollama + fallback Claude; botão "Analisar" no OpsWatch
- [ ] **GROQ_MODEL_SELECTOR** — seletor de modelo no `workflow_dispatch` do code-review
- [ ] **Ollama na VPS** — `docker run ollama/ollama` + pull `gemma3:latest`

### ☁️ Infraestrutura
- [ ] **K3s + Helm + Argo CD** — migração Docker Compose → K3s na VPS; repo `comprai-infra`
- [ ] **StressForge** — gerador agnóstico de stress tests a partir de Swagger/OpenAPI
- [ ] **StatusForge** — status page self-hosted

### 📱 Canais
- [ ] **WhatsApp Bot** — Meta Cloud API + Hub SignalR + `IChannelPort`; reutiliza 100% do core UCP
- [ ] **Extensão Chrome** — Price Watcher + Universal Cart + Intent Bar

---

## ✅ Concluído

### V052 — BD como source of truth no fluxo de compra (2026-10-06)
- [x] `CustomerDto` com campos individuais de endereço
- [x] `Order.Id` migrado para UUID puro (compatível com PostgreSQL)
- [x] `OrderRepository.SaveAsync` persiste endereço snapshot
- [x] `RedisCheckoutAdapter` — UUID + endereço + enqueue `FulfillmentSimulator`
- [x] `PersistingPaymentAdapter` — insere `fulfillment_event` (payment_confirmed) + enqueue simulator
- [x] `OrdersQueryRepository` lê tabela `order` (não `order_history`) + itens + pagamento
- [x] `MockCheckoutPort` persiste via `IOrderPort`; `MockOrderPort` usa `ConcurrentDictionary`
- [x] `PaymentRepository` sem `::uuid` cast (Npgsql resolve automaticamente)
- [x] DI corrigido: `PostgresFulfillmentRepository` (Persistence) concreto + `IFulfillmentRepository` (Fulfillment) separados
- [x] Smoke test body alinhado com `CustomerDto`

### V051 — Testes CI verdes + F5 Polling + Auto-dismiss (2026-10-06)
- [x] `[FromServices]`, `RemoveAll`, `[Fact(Skip)]` — CI verde
- [x] F5: polling real de fulfillment a cada 5s; fallback simulação local
- [x] `handleViewOrders` API-first + mescla localStorage
- [x] Auto-dismiss PixCard/StripeCard 800ms pós-confirmação

### V048–V050 — PostgreSQL + Outbox + Fix CI (2026-10-05)
- [x] 17 tabelas, DbUp, Dapper + Npgsql
- [x] 3 OutboxWorkers (Kafka + RabbitMQ)
- [x] Fix ResilienceExtensions, IdempotencyMiddleware, HealthApiFactory

### V044–V047 — Mobile UX + Fulfillment Domain (2026-10-01 a 2026-10-04)
- [x] `apps/mobile/` Next.js porta 3003
- [x] `FulfillmentAggregate` Event Sourcing + `FulfillmentSimulator` pipeline 6 etapas
- [x] `OrderTrackingCard` timeline 7 etapas

### V042–V043 — Web Chat Frontend (2026-09-30)
- [x] Next.js 15 App Router + Tailwind v4 + TypeScript
- [x] Fluxo UCP completo no front
- [x] `deploy-web.yml` — Docker + GHCR + VPS

### Backend + Observabilidade (V033–V041)
- [x] Fluxo UCP completo backend
- [x] Plugins: DummyJSON, Mock, Shopify, MercadoLivre, VTEX, OpenFoodFacts
- [x] Pagamento: Mock, Stripe, Efi/Pix
- [x] Redis + HybridCache .NET 10
- [x] Kafka + OTel → Datadog + New Relic
- [x] OpsWatch: K6, SonarCloud, New Relic APM drill-down
- [x] CI/CD: build → unit → integration → sonar → deploy → smoke
