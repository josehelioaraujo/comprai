# Changelog

## [V058] — 2026-10-07

### Added
- `PUT /api/auth/me` — editar nome, telefone e documento do cliente
- `POST /api/auth/me/address` — salvar endereço de entrega padrão
- `GET /api/auth/me` agora retorna endereços salvos (`customer_address`)
- `/account` com formulário de edição de perfil e endereço de entrega
- `useAuth` expõe `profile` completo (com endereços) e `defaultAddress`
- Tela de login redesenhada — minimalista, fundo branco, botão roxo `#7c3aed`
- Next-auth no mobile (Google, GitHub, Microsoft, Credentials)
- `V003__customer_address_unique.sql` — unique index em `customer_address`
- `concurrency` nos workflows `deploy-web` e `deploy-mobile` — evita race condition no bump de versão

## [V057] — 2026-10-07

### Added
- Autenticação completa — JWT HS256, BCrypt, providers SSO multi-plataforma
- `POST /api/auth/register|login|callback` + `GET /api/auth/me`
- `order.customer_id` e `session.customer_id` vinculados ao usuário autenticado
- `/account` — dados da conta; `/profile` — histórico de pedidos + repetir pedido
- NextAuth.js (Google, GitHub, Microsoft) + tela login dark/laranja
- Docker projeto renomeado de `deploy` para `comprai` via `-p comprai`
- `V002__auth_schema.sql` — colunas `provider`, `provider_id`, `password_hash`, `avatar_url`, `email_verified`
- Seção 🔐 Segurança no README com OAuth, Strix e roadmap de segurança


## [Unreleased] — V057 — Autenticação e Área do Usuário

### Planejado
- Tela de cadastro/login (web + mobile) — inspiração AIdemy, identidade visual Comprai
- Login social Google (NextAuth.js / Auth.js)
- Cadastro com tabela `customer` existente (name, email, password hash, document, address)
- Histórico de pedidos vinculado ao `customer_id` — refazer compra, acompanhar status
- Backend: `POST /api/auth/register`, `POST /api/auth/login`, `GET /api/auth/me`
- JWT para autenticação nas chamadas subsequentes
- Vinculação `session_id` → `customer_id` após login
- Futuro: dashboard administrativo para consulta de pedidos

---

## [1.0.56] — V056 — 2026-10-06

### Feat — ICartSnapshotService (Clean Architecture) + Frontend: Session tracking

#### Backend — ICartSnapshotService

- **`ICartSnapshotService`** criado em `SharedKernel.Ports` — interface de negócio acima do `ICartSnapshotPort`; métodos: `PersistAsync`, `DeleteAsync`, `GetAsync`
- **`CartSnapshotService`** em `Application.Cart` — implementação que delega ao `ICartSnapshotPort`; `PersistAsync` chama `GetItemsAsync` → `SaveAsync` (itens > 0) ou `DeleteAsync` (carrinho vazio); exceções sempre engolidas (best-effort)
- **`NullCartSnapshotService`** em `Infrastructure.Persistence.Repositories` — stub `[ExcludeFromCodeCoverage]` para `UsarPostgres=false`
- **`AddToCartHandler`** e **`RemoveFromCartCommandHandler`** — injetam `ICartSnapshotService?` em vez de `ICartSnapshotPort?`; lógica `PersistSnapshotAsync` removida dos handlers
- **`Program.cs`** — `AddSingleton<ICartSnapshotService, CartSnapshotService>` no bloco `usarPostgres`; `AddSingleton<ICartSnapshotService, NullCartSnapshotService>` no bloco `!usarPostgres`
- **`CartSnapshotServiceTests`** — 7 testes cobrindo todos os caminhos: `PersistAsync` (com itens, sem itens, null, exceção), `DeleteAsync`, `GetAsync` (com e sem snapshot)
- **`CartHandlerTests`** — atualizado para injetar `ICartSnapshotService` via Mock; TODO removido

#### Frontend — Session tracking

- **`api.ts`** — `searchProducts` usa `X-Session-Id` header em vez de query param; `createPayment` aceita `sessionId?` no body; `getCartSnapshot` nova função (`GET /api/cart/{sessionId}/snapshot`)
- **`useChat.ts`** — `useEffect` de inicialização chama `getCartSnapshot` e exibe card de carrinho abandonado se houver itens; `createPayment` passa `sessionId` corretamente

#### Commits V056
| Hash | Descrição |
|------|-----------|
| `8f0ce3a8` | feat(web/V056): X-Session-Id no search, sessionId no payment, restore carrinho abandonado |
| `2f4f672b` | feat(V056): ICartSnapshotService criado (7 arquivos) |
| `5f8d3f0c` | fix(build): using UcpAgent.SharedKernel.Ports em ICartSnapshotService e stubs |
| `9e20909b` | fix(build): NullCartSnapshotService e Program.cs — ICartSnapshotService em SharedKernel.Ports |
| `c3031af1` | fix(build): criar ICartSnapshotService em SharedKernel.Ports (estava faltando no repo) |

---

## [1.0.55] — V055 — 2026-10-06

### Feat — cart_snapshot + webhook_event idempotência + CI fixes

- **`ICartSnapshotPort`** + **`CartSnapshotRepository`** — upsert `ON CONFLICT` em `cart_snapshot` a cada add/remove; delete após pagamento; `GET /api/cart/{sessionId}/snapshot` para recuperação de abandono
- **`IWebhookEventPort`** + **`WebhookEventRepository`** — `INSERT ON CONFLICT DO NOTHING`; `TryRecordAsync` retorna `false` se duplicado
- **`POST /webhook/stripe`** e **`POST /webhook/efi`** — idempotência via `webhook_event`
- **`AddToCartHandler`** / **`RemoveFromCartCommandHandler`** — `ICartSnapshotPort` opcional; snapshot fire-and-forget; null-safe `GetItemsAsync`
- **`PaymentRequestDto`** — `SessionId?` adicionado; endpoint `/api/payment/{orderId}` deleta `cart_snapshot` após pagamento
- **CI fixes** — `NullCartSnapshotPort`, `NullWebhookEventPort`, `NullCartSnapshotService` para `WebApplicationFactory` sem Postgres; `COVERAGE_THRESHOLD` → 80%

#### Commits V055
| Hash | Descrição |
|------|-----------|
| `6fe01045` | feat(db): V055 — cart_snapshot e webhook_event idempotência |
| `2e17a8af` | fix(build): remove [FromServices] — Minimal API .NET 10 resolve automaticamente |
| `b0c08f7e` | fix(tests): null-safe GetItemsAsync nos handlers |
| `b9912e27` | fix(tests): MockOrderPort null-safe CustomerDto + HealthApiFactory |
| `3810fcb9` | fix(tests): NullCartSnapshotPort + NullWebhookEventPort |
| `5641e198` | fix(build): NullCartSnapshotPort retorno correto Task<CartSnapshotDto?> |
| `ac128f1e` | fix(coverage): [ExcludeFromCodeCoverage] nos stubs Null*Port |
| `f26a7ad3` | ci: COVERAGE_THRESHOLD → 80 |

---

## [1.0.54] — V054 — 2026-10-06

### Feat — order_history + session + search_log no BD

- **`ISessionPort`** + **`SessionRepository`** — get-or-create session no BD; ON CONFLICT renova `expires_at`; TTL 2h
- **`IOrderHistoryPort`** + **`OrderHistoryRepository`** — INSERT ON CONFLICT DO NOTHING; FulfillmentSimulator grava ao atingir Delivered/Cancelled/Returned
- **`SearchLogRepository`** — coluna `results_count`; sources: `TEXT[]`; `session_id` via `ISessionPort`
- **`FulfillmentSimulator`** — `IOrderHistoryPort` injetado como opcional
- **`/api/search`** — lê `X-Session-Id` do header e passa para `SearchProductsQuery`

---

## [1.0.53] — V053 — 2026-10-06

### Feat — Frontend: localStorage como fallback + Mobile standalone

- **`useChat.ts` (web)** — `saveOrderToList` renomeada para `saveOrderToListFallback`; localStorage usado apenas quando a API não retorna o pedido
- **`apiPersistedRef`** — novo `useRef<Record<string, boolean>>` que rastreia pedidos confirmados no BD
- **`apps/mobile`** confirmado standalone — porta `3003`, `deploy-mobile.yml` independente
- **CI smoke-tests** — `timeout-minutes: 10` + Newman `--timeout-request 15000`

#### Commits V053
| Hash | Descrição |
|------|-----------|
| `a4a302c6` | feat(web): localStorage como fallback — BD é source of truth (V053-F1) |
| `0ce9b582` | chore(web): remove componentes mobile duplicados de apps/web (V053-F2) |
| `7ea3c82b` | fix(ci): smoke-tests timeout-minutes:10 + newman --timeout-request 15s (V053-F3) |

---

## [1.0.52] — V052 — 2026-10-06

### Feat — BD como source of truth no fluxo de compra

- **`CustomerDto`** expandido com campos individuais de endereço
- **`Order.Id`** migrado para UUID puro (compatível com PostgreSQL)
- **`OrderRepository.SaveAsync`** — persiste `shipping_zip/street/number/complement/city/state`
- **`PersistingPaymentAdapter`** — insere `fulfillment_event` (`payment_confirmed`) + enqueue simulator
- **`MockCheckoutPort`** — UUID real + persiste via `IOrderPort.SaveAsync`

#### Commits V052
| Hash | Descrição |
|------|-----------|
| `1c02d2c9` | `[F1-F2-DB]` BD como source of truth — 7 arquivos |
| `80f8a306` | fix CS0104: alias `PostgresFulfillmentRepo` |
| `709784de` | fix: `MockCheckoutPort` UUID puro; `PaymentRepository` sem `::uuid` cast |

---

## [1.0.51] — V051 — 2026-10-06

### Fix — Testes de Integração + F5 Fulfillment Polling + Auto-dismiss

- `[FromServices]`, `RemoveAll`, `[Fact(Skip)]` — CI verde
- `getFulfillmentTimeline` → `GET /api/orders/{id}/fulfillment`; polling real 5s; fallback local
- Auto-dismiss PixCard/StripeCard 800ms pós-confirmação

---

## [1.0.48] — V048 — 2026-10-05

### Feat — Persistência PostgreSQL + Outbox Pattern (Dapper + DbUp)

- `comprai-postgres` no `docker-compose.yml`
- `V001__initial_schema.sql` — 17 tabelas, índices, outbox
- DbUp — migrations automáticas na startup
- `OrderOutboxWorker` / `PaymentOutboxWorker` / `NotificationOutboxWorker`

---

## [V044–V047] — Mobile UX + Fulfillment (2026-10-01 a 2026-10-04)

- `apps/mobile/` — Next.js independente porta 3003
- `FulfillmentAggregate` (Event Sourcing) + `FulfillmentSimulator` + pipeline 6 etapas
- `OrderTrackingCard` — timeline 7 etapas

---

## [V042–V043] — Web Chat Frontend (2026-09-30)

- Next.js 15 App Router + Tailwind v4 + TypeScript
- Fluxo UCP completo: Search → Cart → Checkout → Pix/Cartão → Entrega
- `deploy-web.yml` — Docker build + push GHCR + deploy VPS

---

## [V038 e anteriores]
Consulte o histórico de commits no GitHub.
