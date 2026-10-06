# 🗺️ Roadmap — Comprai

## 🔜 V057 — Autenticação e Área do Usuário (próxima)

### 🔴 Alta Prioridade
- [ ] **Tela de cadastro/login** — web + mobile; design inspirado no template AIdemy (dark, CTA laranja, Google SSO) com identidade visual Comprai
- [ ] **Login social Google** — NextAuth.js / Auth.js
- [ ] **Cadastro com `customer`** — name, email, password hash, document (CPF opcional), address
- [ ] **Backend auth** — `POST /api/auth/register`, `POST /api/auth/login`, `GET /api/auth/me`
- [ ] **JWT** — token para autenticação nas chamadas subsequentes
- [ ] **Vinculação session → customer** — `session_id` → `customer_id` após login

### 🟡 Média Prioridade
- [ ] **Histórico do usuário** — pedidos vinculados ao `customer_id`; refazer compra anterior; acompanhar status
- [ ] **Área do usuário** — página "Meus Pedidos" com listagem, status e rastreio

### 🟢 Futuro
- [ ] **Dashboard administrativo** — consulta de pedidos por admin; filtros, exportação

---

## ✅ V056 — Concluído (2026-10-06)

### ICartSnapshotService + Frontend Session tracking
- [x] `ICartSnapshotService` em `SharedKernel.Ports` — interface de negócio acima do port
- [x] `CartSnapshotService` em `Application.Cart` — `PersistAsync` orquestra GetItems → Save/Delete
- [x] `NullCartSnapshotService` — stub no-op para `UsarPostgres=false`
- [x] Handlers migrados: `AddToCartHandler` + `RemoveFromCartCommandHandler` injetam `ICartSnapshotService?`
- [x] 7 testes unitários `CartSnapshotServiceTests` cobrindo todos os caminhos
- [x] Front: `X-Session-Id` header no search, `sessionId` no payment body, restore carrinho abandonado

---

## ✅ V055 — Concluído (2026-10-06)

### cart_snapshot + webhook_event idempotência
- [x] `ICartSnapshotPort` + `CartSnapshotRepository` — upsert / get / delete
- [x] `IWebhookEventPort` + `WebhookEventRepository` — idempotência Stripe/Efi
- [x] Handlers com snapshot fire-and-forget + null-safe
- [x] Stubs Null*Port e Null*Service para WebApplicationFactory
- [x] `COVERAGE_THRESHOLD` → 80%

---

## ✅ V054 — Concluído (2026-10-06)

### order_history + session + search_log no BD
- [x] `ISessionPort` + `SessionRepository`
- [x] `IOrderHistoryPort` + `OrderHistoryRepository`
- [x] `SearchLogRepository`
- [x] `X-Session-Id` no endpoint `/api/search`

---

## ✅ V053 — Concluído (2026-10-06)

### Frontend localStorage como fallback + Mobile standalone + CI fix
- [x] `saveOrderToListFallback`; `apiPersistedRef`
- [x] Componentes mobile removidos de `apps/web`
- [x] CI smoke-tests: timeout fix

---

## ✅ V052 — Concluído (2026-10-06)

### BD como source of truth
- [x] UUID order, endereço snapshot, `fulfillment_event`, DI corrigido

---

## 🔭 Roadmap Futuro

### 🏭 OMS + WMS + Carrier (plugins hexagonais extensíveis)
- [ ] `UcpAgent.Oms.Core` + `UcpAgent.Oms.Simulated` + `UcpAgent.Oms.Vtex`
- [ ] `UcpAgent.Wms.Core` + `UcpAgent.Wms.Simulated` + `UcpAgent.Wms.Totvs`
- [ ] `UcpAgent.Carrier.Core` + `UcpAgent.Carrier.Simulated` + `UcpAgent.Carrier.Correios/Jadlog`
- [ ] Portas: `IOrderManagementPort`, `IWarehousePort`, `ICarrierPort`
- [ ] `V002__oms_wms_schema.sql`: stock, stock_reservation, invoice, shipment, shipment_event, return_request, cancellation

### 🤖 AI/LLM
- [ ] **LLM_DIAGNOSTICO** — `/api/ai/analyze` → Ollama + fallback Claude
- [ ] **GROQ_MODEL_SELECTOR** — seletor de modelo no `workflow_dispatch`
- [ ] **Ollama na VPS** — `docker run ollama/ollama` + pull `gemma3:latest`

### ☁️ Infraestrutura
- [ ] **K3s + Helm + Argo CD** — migração Docker Compose → K3s na VPS
- [ ] **StressForge** — gerador agnóstico de stress tests a partir de Swagger/OpenAPI
- [ ] **StatusForge** — status page self-hosted

### 📱 Canais
- [ ] **WhatsApp Bot** — Meta Cloud API + Hub SignalR + `IChannelPort`
- [ ] **Extensão Chrome** — Price Watcher + Universal Cart + Intent Bar
