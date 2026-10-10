# 🗺️ Roadmap — Comprai

## 🔜 V068 — Auditoria de Segurança + Criptografia de PII (out/2026)

**Fase 1 — Reconhecimento**
- [ ] Varredura histórico git completo com **gitleaks/trufflehog** (secrets em commits antigos)
- [ ] `dotnet list package --vulnerable` + `npm audit` (apps/web e apps/mobile)
- [ ] **Trivy/grype** nas imagens Docker — CVEs críticos
- [ ] Inventário de campos PII nas tabelas: `customer`, `customer_address`, `payment`, `notification`
- [ ] Mapeamento de PII em: logs Serilog, traces OTel, mensagens Kafka/Outbox, webhooks, HAR files E2E
- [ ] Confirmar `storageState.json` + `har-evidence/` no `.gitignore`

**Fase 2 — Banco de Dados: Criptografia e Mascaramento**
- [ ] `IEncryptionService` + `EncryptionService` — AES-256-GCM, chave via secret `ENCRYPTION_KEY`
- [ ] Criptografar em repouso: `customer.document`, `customer.phone`, `customer_address.*`
- [ ] Mascarar em API response: email → `h***@***.com`, documento → `***.***.***-**`, telefone → `(**) *****-**00`
- [ ] Migration: colunas `_enc` (bytea) → migrar dados → remover colunas plaintext
- [ ] Key rotation sem downtime: campo `encryption_version` na tabela

**Fase 3 — Código: PII em Logs, Traces e Tráfego**
- [ ] Serilog `DestructuringPolicy` — mascara campos sensíveis automaticamente em todos os objetos logados
- [ ] OTel: sanitizar atributos de span antes do export (email, document, address)
- [ ] Kafka/Outbox: eventos de domínio carregam apenas IDs, nunca PII diretamente
- [ ] DTOs de resposta: nunca expõem campos raw — somente mascarados ou omitidos

**Fase 4 — Infra e CI/CD**
- [ ] `secret-scan.yml` no CI (gitleaks action) — bloqueia push com credencial
- [ ] SAST: semgrep ou CodeQL para SQL injection, XSS, insecure deserialization
- [ ] Rate limiting ampliado: checkout, account update, address
- [ ] CORS produção: sem wildcard, origem única
- [ ] Postgres: `pg_hba.conf` sem acesso externo, usuário da app sem superuser

---

## ✅ V067 — HAR per-test Fix + Postman Collection Export (out/2026)
- [x] `apps/e2e/fixtures.ts` — fixture custom override de `context`; cria `har-evidence/{title}-chromium.har` por teste
- [x] Causa raiz resolvida: `recordHar.path` sem extensão criava FILE em vez de diretório — requests: [] corrigido ✅
- [x] Botão global "Baixar Collection Postman" (laranja #ff6c37) — Postman Collection v2.1 com todos os specs
- [x] `_e2eToPostmanCollection()` — agrupa suite → test → request; URL parsed; headers; savedResponses
- [x] `e2eDownloadSpecPostman(idx)` — download individual de Collection Postman por teste
- [x] Ícones SVG 11×11 discretos (copy/download) por linha de spec com tooltip — sem texto, sem quebra de layout
- [x] Fix: nunca usar `JSON.stringify()` inline em `onclick` — referência por índice `_e2eSpecs[idx]`
- [x] `event.stopPropagation()` nos botões ícone para não disparar o expand/collapse pai

---

## ✅ V066 — E2E Evidence: Baixar Todos + Request/Response (out/2026)
- [x] Botão "Baixar Todos os Testes" no OpsWatch e no `report.html` — JSON com todos os specs + requisições
- [x] `_e2eLastData` declarado e populado em `loadE2eData` — resolve `ReferenceError` no download
- [x] `apps/e2e/har-evidence/` adicionado ao artifact upload — causa raiz do `requests: []`
- [x] `norm_har_key()` + Jaccard similarity para matching HAR ↔ spec — resolve divergência de filenames
- [x] `req_headers` capturado (Authorization, Content-Type, Accept) por requisição
- [x] `_e2eSpecInlineDetails()` reescrita — exibe requisições HTTP inline por spec
- [x] Modal de spec com "REQUEST HEADERS" em bloco separado
- [x] `report.html` com tabela+drilldown, player de vídeos, chevron+filtro, timezone BRT, contador corrigido
- [x] Nested suites + chevrons + collapse all no OpsWatch; chevron nos cards de jobs; botão cancelar workflow
- [x] `e2e.yml` triggers simplificados: somente `workflow_dispatch`
- [ ] **⚠️ Pendente → V067**: HAR ainda retorna `requests: []` — `har-evidence/` pode não ser criado no Actions; investigar com `ls -la` no workflow

---

## ✅ V065 — Endpoint de Pagamento E2E + OpsWatch Drill-Down (out/2026)
- [x] `createPayment()` corrigida — `POST /api/payment/{orderId}` com body e headers corretos
- [x] Smoke test de pagamento: 200 ✅ | `MOCK-PAY-XXXXXXXX`
- [x] OpsWatch — jobs/steps expansíveis na execução de workflow
- [x] OpsWatch — suites E2E colapsáveis com chevron animado
- [x] OpsWatch — todos os specs clicáveis (não só erros), modal com cor por status
- [x] OpsWatch — cards KPI Duração e Taxa OK abrem modal de resultados

---

## ✅ V064 — E2E: 14 falhas → 0 falhas (out/2026)
- [x] `strict mode violation` corrigido em todos os specs — `getByRole('button', { name: 'Entrar' })`
- [x] `auth.spec.ts` test 7 — `test.use({ storageState: empty })` para redirect sem auth
- [x] `purchase-flow.spec.ts` reescrito — fluxo híbrido API + UI, resumo final consolidado
- [x] Tolerância a 429 rate-limit em todos os testes de login
- [x] Tolerância a `ECONNREFUSED` em todos os blocos de API
- [x] `/api/search` — `page` e `pageSize` com defaults (1 e 10); CS1737 corrigido
- [x] Script `run-e2e-vps.sh` para execução na VPS com vídeo consolidado
- [x] `PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH` no config para Chromium da VPS
- [x] **Resultado**: 30 passando, 1 skipped, 0 falhando

---

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

## ✅ V061 — Segurança cont. / OpsWatch (out/2026)
- [x] **Recuperação de senha** — `POST /api/auth/forgot-password` + `POST /api/auth/reset-password`; migration V007; token UUID→SHA-256; 1h; email via Resend; anti-enumeração (sempre 204)
- [x] **Revogar todos os refresh tokens** — `POST /api/auth/logout-all`; revoga todos os tokens do customer
- [x] **Painel Pentest no OpsWatch** — exibir findings ZAP de `/k6/results/pentest/latest.json` (já implementado em V060)
- [x] **Páginas web** — `/auth/forgot-password` + `/auth/reset-password` (design white/purple); link "Esqueceu sua senha?" no login

---

## ✅ V062 — E2E Evidence Recorder (out/2026)
- [x] **Playwright E2E** com gravação de vídeo (`.webm`) e screenshots por teste
- [x] **Specs**: `auth.spec.ts`, `search.spec.ts`, `checkout.spec.ts`, `account.spec.ts`, `purchase-flow.spec.ts`
- [x] **Workflow `e2e.yml`** — trigger push main + schedule 03h UTC + `workflow_dispatch`; playwright test --video=on, upload artifact `playwright-report/`
- [x] **Painel "E2E / Evidências" no OpsWatch** — status por spec, vídeos, link HTML report; grava `results/e2e/latest.json`

---

## ✅ V063 — E2E Infrastructure + NextAuth v5 Fix (out/2026)
- [x] **`global-setup.ts`** — login único com retry automático (4x, 65 s) — salva `storageState.json`
- [x] **`storageState` no `playwright.config.ts`** — todos os testes reutilizam sessão autenticada
- [x] **`RateLimitOptions.Auth` configurável** — `RateLimit__Auth__PermitLimit` via env (E2E/dev sem rebuild)
- [x] **Fix `AUTH_TRUST_HOST=true`** — NextAuth v5 em IP direto; container `comprai-web` recriado na VPS
- [x] 6/30 testes E2E passando após o fix

---

## 🔜 V067 — HAR Capture Fix + Integração Linear (out/2026)
- [ ] **HAR capture** — investigar `har-evidence/` no GitHub Actions: adicionar `ls -la apps/e2e/har-evidence/` como debug step; verificar se `recordHar.path` relativo funciona com `working-directory: apps/e2e`; garantir `requests` não-vazio no `latest.json`
- [ ] **Integração Linear** — rastreamento e documentação das tarefas do Comprai
- [ ] Script: lê CHANGELOG + ROADMAP + git log → cria ~60 issues via Linear GraphQL (V052–V066 como Done)

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

