# Changelog

## [1.0.52] — V052 — 2026-10-06

### Feat — BD como source of truth no fluxo de compra

- **`CustomerDto`** expandido com campos individuais de endereço (`Cep`, `Street`, `Number`, `Complement`, `Neighborhood`, `City`, `State`, `Document`) — campo `Address` mantido como legado para compatibilidade
- **`Order.Id`** migrado de `"ORDER-XXXXXXXX"` para `Guid.NewGuid().ToString()` — compatível com `order.id UUID` no PostgreSQL
- **`OrderRepository.SaveAsync`** — persiste `shipping_zip/street/number/complement/city/state` no INSERT
- **`RedisCheckoutAdapter`** — UUID real + endereço snapshot + `SaveSessionOrderAsync` + enqueue `FulfillmentSimulator`
- **`PersistingPaymentAdapter`** — insere `fulfillment_event` (`payment_confirmed`) via `FulfillmentAggregate.Create` + enqueue simulator após confirmar pagamento
- **`OrdersQueryRepository.GetHistoryBySessionAsync`** — reescrito para ler tabela `order` diretamente (não `order_history`) com JOIN em `order_item` e `payment`; inclui `itemCount`, `shippingCity/State`
- **`OrdersEndpoints`** — `GET /api/orders?sessionId` mapeia response para shape esperado pelo front (`orderId`, `status`, `total`, `tracking`, etc.)
- **`MockCheckoutPort`** — injeta `IOrderPort` e persiste pedido em memória via `SaveAsync` + `SaveSessionOrderAsync`
- **`MockOrderPort`** — usa `ConcurrentDictionary` em memória; retorna qualquer `orderId` conhecido; fallback `Pending` para IDs desconhecidos
- **`PaymentRepository.ConfirmAsync`** — removido `::uuid` cast do `@orderId` (Npgsql resolve automaticamente)
- **Program.cs DI** — `PostgresFulfillmentRepository` (Persistence.Repositories) registrado como concreto para `PersistingPaymentAdapter`; `IFulfillmentRepository` (Fulfillment) registrado separadamente para `FulfillmentSimulator`
- **Smoke test** — body do checkout alinhado com `CustomerDto` (`name/email/phone` + campos de endereço individuais)

#### Commits V052
| Hash | Descrição |
|------|-----------|
| `1c02d2c9` | `[F1-F2-DB]` BD como source of truth — 7 arquivos |
| `80f8a306` | fix CS0104: alias `PostgresFulfillmentRepo` em `PersistingPaymentAdapter` |
| `fb4b3c67` | fix Program.cs: `PostgresFulfillmentRepository` + `FulfillmentSimulator` no registro |
| `709784de` | fix: `MockCheckoutPort` UUID puro; `PaymentRepository` remove `::uuid` cast |
| `d4fdcada` | fix: `GET /api/orders/{orderId}` restaurado em `OrdersEndpoints` |
| `e841ff00` | fix: remove `GET /{orderId}` duplicado (já existe em Program.cs — `AmbiguousMatchException`) |
| `e91b918b` | fix: `MockOrderPort` em memória; `MockCheckoutPort` injeta `IOrderPort` |
| `61f40e7f` | fix: smoke test body alinhado com `CustomerDto` |
| `71b4b403` | fix: `PostgresFulfillmentRepository` registrado como concreto + interface |
| `6d85366c` | fix: DI separado — `Persistence.Repositories.PostgresFulfillmentRepository` + `IFulfillmentRepository` |

---

## [1.0.51] — V051 — 2026-10-06

### Fix — Testes de Integração (CI verde)

- **`[FromServices]`** adicionado em `OrdersEndpoints.cs` nos dois `MapGet`
- **`RemoveAll(typeof(IPaymentPort))`** em `EfiPayFactory`
- **`using Microsoft.Extensions.DependencyInjection.Extensions`** em `EfiPaymentIntegrationTest`
- **`[Fact(Skip)]`** em `PollyResilienceIntegrationTest`

### Feat — F5: Fulfillment Polling Real + Orders API-First (Front)

- **`api.ts`** — `getFulfillmentTimeline(orderId)` → `GET /api/orders/{id}/fulfillment`; `getOrders` normalizado
- **`useChat.ts`** — `startFulfillmentPolling`: polling real a cada 5s; fallback simulação local
- **`handleViewOrders`** — API-first + mescla localStorage + fallback

### Feat — Opção A: Auto-dismiss PixCard e StripeCard pós-confirmação

- `IntentRenderer`, `ChatWindow`, `chat/page.tsx`, `CompraiWidget`, `MobileIntentRenderer` — auto-dismiss 800ms

#### Commits V051
| Hash | Descrição |
|------|-----------|
| `d8dddd93` | `[FromServices]`, `RemoveAll`, `[Fact(Skip)]` |
| `c3d92916` | `using DependencyInjection.Extensions` em `EfiPaymentIntegrationTest` |
| `73db5650` | F5: `getFulfillmentTimeline` + `getOrders` API-first + polling |
| `ec768773` | Opção A: auto-dismiss `IntentRenderer` + `ChatWindow` |
| `c530a425` | `chat/page.tsx` com `onDismissMessage` |

---

## [1.0.49] — V049/V050 — 2026-10-05

### Fix — CI e Testes de Integração

- `ResilienceExtensions.cs`: `ShouldHandle` restaurado (HTTP 5xx via `HandleResult`)
- `UseMiddleware<IdempotencyMiddleware>` duplicado removido
- `HealthApiFactory` com `ConfigureAppConfiguration`
- Testes de resiliência com `HttpRequestException` determinístico

---

## [1.0.48] — V048 — 2026-10-05

### Feat — Persistência PostgreSQL + Outbox Pattern (Dapper + DbUp)

- `comprai-postgres` no `docker-compose.yml` e `deploy/docker-compose.yml`
- `V001__initial_schema.sql` — 17 tabelas, índices, índices parciais outbox
- DbUp — migrations automáticas na startup
- `CustomerRepository`, `OrderRepository`, `PaymentRepository`, `PostgresFulfillmentRepository`, `OutboxRepository`
- `OrderOutboxWorker` / `PaymentOutboxWorker` → Kafka; `NotificationOutboxWorker` → RabbitMQ
- Backoff exponencial: 30s → 60s → 120s → 300s

---

## [V044–V047] — Mobile UX + Fulfillment (2026-10-01 a 2026-10-04)

- `apps/mobile/` — Next.js independente porta 3003
- `FulfillmentAggregate` (Event Sourcing append-only) + `FulfillmentSimulator` + pipeline 6 etapas
- `OrderTrackingCard` — timeline 7 etapas, rastreio colapsável

---

## [V042–V043] — Web Chat Frontend (2026-09-30)

- Next.js 15 App Router + Tailwind v4 + TypeScript
- Fluxo UCP completo: Search → Cart → Checkout → Pix/Cartão → Entrega
- `deploy-web.yml` — Docker build + push GHCR + deploy VPS

---

## [V038 e anteriores]
Consulte o histórico de commits no GitHub.
