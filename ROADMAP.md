# 🗺️ Roadmap — Comprai

## ✅ V058 — Cadastro de Cliente Completo (out/2026)
- [x] `PUT /api/auth/me` — editar perfil (nome, telefone, documento)
- [x] `POST /api/auth/me/address` — endereço de entrega persistido
- [x] `/account` com formulário de edição
- [x] `useAuth` com perfil completo e `defaultAddress`
- [x] Redesign tela de login — minimalista branco/roxo
- [x] Next-auth no mobile com todos os secrets SSO (`NEXTAUTH_URL_MOBILE`, Google, GitHub, Microsoft)
- [x] Concurrency nos workflows deploy-web e deploy-mobile
- [x] `CheckoutCard` pré-preenchido com dados do usuário autenticado

---

## ✅ V057 — Autenticação e Área do Usuário (out/2026)
- [x] JWT HS256 + BCrypt + endpoints `/api/auth/*`
- [x] SSO multi-provider (Google, GitHub, Microsoft) via NextAuth.js
- [x] `order.customer_id` + `session.customer_id` vinculados
- [x] `/account` + `/profile` (histórico + repetir pedido)
- [x] Middleware protege `/chat/*`, `/account/*`, `/profile/*`
- [x] Docker projeto renomeado para `comprai`

---

## ✅ V056 — ICartSnapshotService + Frontend Session tracking (out/2026)
- [x] `ICartSnapshotService` em `SharedKernel.Ports`
- [x] `CartSnapshotService` em `Application.Cart`
- [x] `NullCartSnapshotService` — stub no-op para `UsarPostgres=false`
- [x] Handlers migrados: `AddToCartHandler` + `RemoveFromCartCommandHandler`
- [x] 7 testes unitários `CartSnapshotServiceTests`
- [x] Front: `X-Session-Id` header no search, `sessionId` no payment, restore carrinho abandonado

---

## ✅ V055 — cart_snapshot + webhook_event idempotência (out/2026)
- [x] `ICartSnapshotPort` + `CartSnapshotRepository`
- [x] `IWebhookEventPort` + `WebhookEventRepository`
- [x] Handlers com snapshot fire-and-forget + null-safe
- [x] Stubs Null*Port para WebApplicationFactory
- [x] `COVERAGE_THRESHOLD` → 80%

---

## ✅ V054 — order_history + session + search_log no BD (out/2026)
- [x] `ISessionPort` + `SessionRepository`
- [x] `IOrderHistoryPort` + `OrderHistoryRepository`
- [x] `SearchLogRepository`
- [x] `X-Session-Id` no endpoint `/api/search`

---

## ✅ V053 — Frontend localStorage fallback + Mobile standalone (out/2026)
- [x] `saveOrderToListFallback`; `apiPersistedRef`
- [x] Componentes mobile removidos de `apps/web`
- [x] CI smoke-tests: timeout fix

---

## ✅ V052 — BD como source of truth (out/2026)
- [x] UUID order, endereço snapshot, `fulfillment_event`, DI corrigido

---

## ✅ V059 — Segurança (out/2026)
- [x] `SecurityHeadersMiddleware` — CSP, HSTS, X-Frame-Options, X-Content-Type-Options, Referrer-Policy, Permissions-Policy
- [x] Rate limiting SlidingWindow 5 req/60s por IP em login/register (brute force prevention)
- [x] `pentest.yml` OWASP ZAP melhorado — JWT autenticado, rate limit check, security headers check, artifact HTML
- [x] Migration V004 — `refresh_token_hash` + `refresh_token_expires_at`
- [x] Refresh token opaque (SHA-256) com rotação — `POST /api/auth/refresh` + `POST /api/auth/logout`
- [x] `NullAuthPort` atualizado com métodos de refresh token

---

## ✅ V060 — Segurança cont. (out/2026)
- [x] **Verificação de email pós-cadastro** — Resend OTP; migration V005; `POST /api/auth/verify-email`
- [x] **2FA TOTP** — `Otp.NET v1.4.0`; migration V006; QR Code + manual secret; `POST /api/auth/2fa/{setup,enable,verify,disable}`
- [x] **Login step-up** — `LoginPage` detecta `requires2fa`, redireciona para `/auth/2fa/verify`; next-auth caminho pré-autenticado `{token, customer}`
- [x] **Job Pentest Strix removido** do `ci-cd.yml`

---

## 🔜 V061 — Segurança cont. / OpsWatch (próxima)

### 🟡 Média Prioridade
- [ ] **Painel Pentest no OpsWatch** — exibir findings ZAP de `/k6/results/pentest/latest.json`
- [ ] **Recuperação de senha** — `POST /api/auth/forgot-password` + `POST /api/auth/reset-password`; email via Resend; token UUID 1h
- [ ] **Revogar todos os refresh tokens** — `POST /api/auth/logout-all` (invalida todos os tokens do usuário)

---

## 🔭 Roadmap Futuro

### 🏭 OMS + WMS + Carrier (plugins hexagonais extensíveis)
- [ ] `UcpAgent.Oms.Core` + `UcpAgent.Oms.Simulated` + `UcpAgent.Oms.Vtex`
- [ ] `UcpAgent.Wms.Core` + `UcpAgent.Wms.Simulated` + `UcpAgent.Wms.Totvs`
- [ ] `UcpAgent.Carrier.Core` + `UcpAgent.Carrier.Simulated` + `UcpAgent.Carrier.Correios/Jadlog`
- [ ] Portas: `IOrderManagementPort`, `IWarehousePort`, `ICarrierPort`
- [ ] `V002__oms_wms_schema.sql`: stock, stock_reservation, invoice, shipment, shipment_event, return_request, cancellation

### 🤖 AI/LLM
- [ ] **LLM_DIAGNOSTICO** — `/api/ai/analyze` → Ollama + fallback Claude; botão "Analisar" no OpsWatch
- [ ] **GROQ_MODEL_SELECTOR** — seletor de modelo no `workflow_dispatch`
- [ ] **Ollama na VPS** — `docker run ollama/ollama` + pull `gemma3:latest`

### ☁️ Infraestrutura
- [ ] **K3s + Helm + Argo CD** — migração Docker Compose → K3s na VPS com GitOps
- [ ] **StressForge** — gerador agnóstico de stress tests a partir de Swagger/OpenAPI
- [ ] **StatusForge** — status page self-hosted

### 📱 Canais
- [ ] **WhatsApp Bot** — Meta Cloud API + Hub SignalR + `IChannelPort`; reutiliza 100% do core UCP
- [ ] **Extensão Chrome** — Price Watcher + Universal Cart + Intent Bar

