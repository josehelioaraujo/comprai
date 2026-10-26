# 🗺️ Roadmap — Comprai

## ✅ V079 — Player funcional: oncanplaythrough + AbortError retry + unified ALL[] (concluído)

### Fixed
- [x] `vid.src` → `vid.load()` → `oncanplaythrough` → `vid.play()` — vídeos tocam corretamente
- [x] AbortError por background throttle — retry 800ms + fallback avança sequência
- [x] `filterCat` não interrompe modo Todos — guard `if(!autoAdv)`
- [x] `#vid{flex:1}` — vídeo visível no layout flex
- [x] Array unificado `ALL[]` — API tests (painel resultado) + vídeos na mesma sequência

### Commits
- `ff9c4bf` fix: display vídeo + remove autoplay init
- `b5f3b69` fix: filterCat não interrompe modo Todos
- `ffee208` fix: retry AbortError background throttle
- `224b8b8` fix: aguardar oncanplaythrough antes de vid.play()

---

## 🔜 V080 — Fix clique individual + Total PIX + CLAUDE.md (próxima)

### 🎬 Fix Player de Vídeos — Itens clicáveis (P0)
- [ ] Itens da sidebar devem ter `onclick` explícito para selecionar e exibir o vídeo ao clicar (atualmente só o botão ▶ é clicável)
- [ ] Highlight do item ativo na sidebar quando o vídeo é selecionado

### 🎬 Fix Player de Vídeos — Erros 400 em testes de API (P0)
- [ ] Testes de API (T01–T31 sem gravação visual) retornam 400 ao tentar carregar `.webm`
- [ ] Para esses testes: exibir resultado (`status`, `erro`, `duração`) em vez de tentar carregar vídeo
- [ ] Separar claramente itens "com vídeo" de itens "só resultado" na sidebar

### 🃏 Player de Vídeos — Cards de categoria no topo (P1)
- [ ] Adicionar cards de categoria no topo do player (igual ao `report.html`) — Healthcheck, Cadastro, Autenticação, Busca, Carrinho, Checkout, Pagamento, Pedidos, Fluxo E2E
- [ ] Clicar em card de categoria filtra itens da sidebar

### 🐛 Fix Valor PIX zerado (P2)
- [ ] Investigar por que `Total` aparece R$ 0,00 na tela `/orders/[id]` após pagamento PIX
- [ ] Verificar `RedisOrderRepository` — campo `Total` pode estar sendo descartado na serialização

### 🔥 Fix Deploy VPS SSH (P3)
- [ ] Acessar Hostinger hPanel → VPS → Firewall → verificar se porta 22 está liberada para `0.0.0.0/0`
- [ ] Se bloqueada: adicionar regra `Allow TCP 22 from anywhere`

### 🔗 Link direto no email OTP (P4)
- [ ] Email de verificação deve incluir link direto `/auth/verify?email=<email>` para pré-preencher o campo

### 🎨 Web UI Redesign (P5 — após fixes)
- [ ] Redesign completo da interface web (`frontend/web/`) — referência visual: Nixtio Marketplace Web UI

### 📦 Dependabot (P6 — futura)
- [ ] `.github/dependabot.yml` cobrindo npm, nuget e github-actions quando esteira estiver estável

---

## ✅ V078 — Fix Checkout 500 + Report erros duplicados + MIME webm (concluído)

### Fixed
- [x] `Results.Problem(result.Error)` → `Results.BadRequest(new { error })` no checkout — T18/T19/T20/T28/T30 agora recebem 400 em vez de 500
- [x] Deduplicação de `s_errors` no gerador de `report.html` — erros de retries não aparecem mais repetidos
- [x] MIME type `video/webm` adicionado ao nginx via step SSH — corrige 404/tipo inválido nos `.webm`

### Added
- [x] Player: categorias na sidebar agrupadas em seções colapsáveis com chevrons (`toggleCat()`)
- [x] Player: botões **▶ Todos** e **▶ Um por um** no topo; **▶▶ Próximo** no modo sequencial
- [x] Player: `autoAdv` flag para avanço automático entre vídeos; classe `on` no item ativo

### Commits
- `96ca9cb` feat: player com chevrons e botões de reprodução
- `5f4652b` fix: checkout 400, erros deduplicados, MIME webm nginx

---

## ✅ V077 — OpsWatch Categorias E2E + Player de Vídeos (concluído)

### Added
- [x] OpsWatch: 9 cards de categoria com filtro AND (status + categoria) para E2E v2
- [x] OpsWatch: `e2eV2GetCat()`, `e2eV2FilterCat()`, `_e2eApplyV2Filters()` adicionados
- [x] OpsWatch: `data-e2e-cat` em cada row do v2 para suporte ao filtro combinado
- [x] Player de vídeos: seção "📋 Testes de API" com todos os 31 testes (T01–T31) na sidebar
- [x] Player de vídeos: botão de download por cena visual (⬇ `.webm`)
- [x] Player de vídeos: placeholder `#novid` para vídeos não encontrados (404)

### Known Issue (pendente V078)
- [ ] Player na VPS ainda mostra versão antiga — regenerar com próxima execução do workflow

---

## ✅ V076 — Fix Deploy SSH + E2E Checkout + Total PIX + Dashboard Botões

### 🔥 Fix Deploy VPS SSH (P0)
- [ ] Acessar Hostinger hPanel → VPS → Firewall → verificar se porta 22 está liberada para `0.0.0.0/0`
- [ ] Se bloqueada: adicionar regra `Allow TCP 22 from anywhere`
- [ ] Re-trigger deploy após liberar o firewall
- [ ] UFW inativo, fail2ban não instalado — o bloqueio é 100% no firewall da Hostinger

### 🎬 Fix Dashboard OpsWatch — Restaurar 4 botões (P0)
- [ ] Restaurar botão "Ver Relatório HTML" → link para artefato GitHub Actions (report.html dentro do artefato)
- [ ] Restaurar botão "Baixar Relatório" → download direto do artefato
- [ ] Restaurar botão "Ver Vídeo" → link para artefato GitHub Actions (vídeos .webm dentro do artefato)
- [ ] Restaurar botão "Baixar Vídeo" → download do artefato de vídeo
- [ ] Causa: remoção de `report_html` e `videos_zip` do `latest.json` tornou os botões condicionais invisíveis
- [ ] Estratégia: usar `report_url` (já presente) para todos os 4 botões — GitHub Actions run tem acesso a todos artefatos

### 🐛 Fix E2E Checkout 500 (P1)
- [ ] T18: `POST /api/checkout/{sessionId}` retornando HTTP 500 — investigar `CheckoutEndpoint` e `ICheckoutPort`
- [ ] T19, T30 e outros falhando — provavelmente cascata do checkout 500
- [ ] `E2E_USER_EMAIL` e `E2E_USER_PASSWORD` devem vir de GitHub Secrets — nunca hardcoded

### 🐛 Fix Valor PIX zerado (P2)
- [ ] Investigar por que `Total` aparece R$ 0,00 na tela `/orders/[id]` após pagamento PIX
- [ ] Verificar `RedisOrderRepository` — campo `Total` pode estar sendo descartado na serialização

### 🔗 Link direto no email OTP (P3)
- [ ] Email de verificação deve incluir link direto `/auth/verify?email=<email>` para pré-preencher o campo

### 🎨 Web UI Redesign (P4 — após fixes)
- [ ] Redesign completo da interface web (`frontend/web/`) — referência visual: Nixtio Marketplace Web UI

### 📦 Dependabot (P5 — futura)
- [ ] `.github/dependabot.yml` cobrindo npm, nuget e github-actions quando esteira estiver estável

---

## ✅ V075 — Fix Rate Limiter Integration Tests (concluído)

### Fixed
- [x] `Search_AoExcederPermitLimit_Retorna429` — agora passa ✅
- [x] `Search_QuandoRejeitado_DeveRetornarHeaderRetryAfter` — agora passa ✅
- [x] Config lida lazily DENTRO do lambda `AddRateLimiter` em `RateLimitExtensions.cs`
- [x] Diagnóstico deploy SSH: VPS vivo, UFW inativo, bloqueio é no firewall Hostinger

---

## ✅ V074 — Fix Smoke Tests + Total PIX + E2E (próxima)

### 🔥 Fix Smoke Tests (P1)
- [ ] Testar `curl http://2.25.122.11:5020/health/live` localmente para confirmar se a porta está acessível externamente
  - Se sim → problema é IP dos runners GitHub Actions após migração ubuntu-latest → Ubuntu 26 (19 out/2026)
  - Se não → abrir porta 5020 no painel Hostinger (Firewall → Add rule → TCP 5020)
- [ ] Opção paliativa: adicionar `continue-on-error: true` no job `smoke-tests` para não bloquear o deploy

### 🐛 Fix Valor PIX zerado (P2)
- [ ] Investigar por que `Total` aparece R$ 0,00 na tela `/orders/[id]` após pagamento PIX
  - Suspeita: `OrderStatusDto.Total` não persistido/recuperado corretamente no `IOrderPort.SaveAsync` → `GetAsync`
  - Verificar `RedisOrderRepository` — campo `Total` pode estar sendo descartado na serialização
- [ ] Cobrir com teste E2E Playwright: `total > 0` na tela de confirmação do pedido
- [ ] Smoke tests Newman: adicionar asserção `total > 0` no response de `/orders/{id}`

### 🔗 Link direto no email OTP (P3)
- [ ] Email de verificação deve incluir link direto `/auth/verify?email=<email>` para pré-preencher o campo na tela

### 🧪 E2E Fluxo Completo (P4)
- [ ] Cadastro de novo usuário → recebe email com OTP alfanumérico (Brevo) → verifica email
- [ ] Login → obter JWT
- [ ] Busca de produto → adicionar ao carrinho
- [ ] Checkout → pagamento (mock ou EfiPay PIX)
- [ ] Confirmar total > 0 na tela de pedido (`/orders/[id]`)
- [ ] Rastreio do pedido via `/api/orders/{id}`

### 🎨 Web UI Redesign (P5 — após fixes)
- [ ] Redesign completo da interface web (`frontend/web/`) — referência visual: Nixtio Marketplace Web UI
- [ ] Consistência visual com o mobile-v2 (paleta #7C3AED, tipografia preta, fundo branco)
- [ ] Layout desktop responsivo com sidebar de categorias
- [ ] Header com busca global e mini-carrinho
- [ ] Deploy independente na porta 3002

### 🤖 CI/CD — Dependabot (P6 — quando esteira estável)
- [ ] Configurar `.github/dependabot.yml` com três ecossistemas: npm (raiz), nuget (`src/`), github-actions

---

## ✅ V073 — Linear Backfill V052–V072 + Scripts Idempotentes (out/2026)
- [x] `scripts/linear-populate.ts`: suporte a 3 comandos — `backfill`, `sync`, `cleanup`
- [x] `getOrCreateCycle()`: retorna `string | undefined`, captura erros de overlap de datas no Linear
- [x] Sub-issues idempotentes: `issueExists()` evita duplicatas em re-execuções
- [x] Backfill restrito a `status === "done"` (V052–V072) — V073+ ignorados
- [x] `linear-backfill.yml`: workflow com confirmação + upload de logs em falha
- [x] `linear-cleanup.yml`: novo workflow para remover duplicatas por versão
- [x] `package.json`: pinado `@linear/sdk@7.0.0`, `tsx@4.19.1`, `typescript@5.5.4`
- [x] Run `39536384671`: backfill V052–V072 concluído ✅
- [x] Run `39537574444`: cleanup de duplicatas V073 concluído ✅
- [x] Commits: `374f281` · `a9cc9d3`

---

## ✅ V072 — Fix Email + OTP Alfanumérico (out/2026)
- [x] `deploy/docker-compose.yml`: `Brevo__From` corrigido para `helioandrade@hotmail.com` (era freemail Outlook sem DMARC)
- [x] `docker-compose.yml` raiz + `EmailService.cs`: remetente padronizado com fallback `helioandrade@hotmail.com`
- [x] `appsettings.json`: seção `Brevo.From` adicionada — lida por `EmailService.cs` via `config["Brevo:From"]`
- [x] OTP alfanumérico garantido: `GenerateOtp()` garante mínimo 2 dígitos + 4 letras + shuffle (CSPRNG)
- [x] Charset OTP: exclui 0, 1, I, O para evitar ambiguidade visual
- [x] CI/CD: `ufw allow ${{ env.API_PORT }}/tcp || true` adicionado no script de deploy
- [x] Email de verificação confirmado funcionando ✅ (código recebido com letras + dígitos)
- [x] Commits: `b66280f` · `cab4562` · `bd9bb99` · `7afb383` · `887ed1a`

---

## ✅ V071 — Mobile UI Redesign — `frontend/mobile-v2/` (out/2026)
- [x] Nova pasta `frontend/mobile-v2/` — porta 3005 (mobile original em 3003 intacto)
- [x] Next.js 15 + React 19 + TypeScript strict + Tailwind CSS v3 + NextAuth v5
- [x] Visual Nixtio Skincare — fundo branco, tipografia preta, acento roxo #7C3AED
- [x] Bottom nav: Home (chat IA) / Search / Cart / Account
- [x] Fluxo completo: busca → carrinho → checkout → pagamento → pedidos → rastreio
- [x] Cadastro + auto-login pós-registro (pre-auth bypass no NextAuth)
- [x] Verificação de email via Brevo (OTP 6 dígitos)
- [x] 18 componentes + 10 páginas — `npx tsc --noEmit` → 0 erros ✅
- [x] Dockerfile multi-stage + CI/CD `deploy-mobile-v2.yml` → GHCR
- [x] Commit: `9b88fdf`

---

## ✅ V070 — Reorganização de Pastas + Estabilização CI/CD (out/2026)
- [x] `apps/web/` → `frontend/web/` | `apps/mobile/` → `frontend/mobile/` | `apps/admin/` → `frontend/admin/`
- [x] `apps/e2e/` → `tests/e2e/` | `newman/` → `tests/newman/` | `k6/` → `opswatch/`
- [x] 153 arquivos renomeados em commit único (commit `2d2308d`)
- [x] Todos os GitHub Actions workflows atualizados para os novos paths
- [x] Fix CI/CD: `opswatch/dashboard/Dockerfile` corrigido (`COPY k6/` → `COPY opswatch/`)
- [x] Fix build web/mobile: `eslint-config-next ^14` → `^15.5.27`
- [x] Fix build web/mobile: `tailwindcss ^4` (alpha) → `^3.4.17` (stable)
- [x] Fix build web/mobile: `next/font/google` removido → `<link>` tag no `layout.tsx`
- [x] Fix Deploy Web: endpoint `/api/healthz` criado (`force-dynamic`, 200 sem auth, sem redirect)
- [x] Fix Deploy Web: health check atualizado (`curl /` → `curl /api/healthz`, `sleep 8→20`)
- [x] Resultado: todos os 4 workflows ✅ (CI/CD #1055, Deploy Web #103, CodeQL #76, Secret Scan #76)
- [x] Commits: `2d2308d` · `afb653e` · `d72108b` · `4fb3db4`

---

## ✅ V069v4 — Fix Fluxo Completo de Reset de Senha (out/2026)
- [x] Vars Brevo (`Brevo__ApiKey`, `Brevo__Login`, `Brevo__From`) adicionadas ao `docker-compose.yml` raiz — sem elas o container usava `NullEmailService`
- [x] Sender `helioaraujoandrade@outlook.com` verificado no Brevo
- [x] Rota `/reset-password` liberada no middleware Next.js (rotas públicas)
- [x] Proxy `reset-password/route.ts` corrigido: `status === 204` → `backendRes.ok` — backend retorna 200 OK, não 204
- [x] CI/CD: deploy desacoplado do SonarCloud
- [x] Commits: `02c153a` · `c704747` · `6259bda` · `cfab6f6`

---

## ✅ V069v3 — Fix Admin Orders HTTP 500 (out/2026)
- [x] Causa raiz real: `order_item.product_id` é `VARCHAR(200)` — parâmetro como `Guid?` (UUID) causava erro de operador PostgreSQL `=(varchar,uuid)` ao planejar o query, mesmo sem filtros
- [x] Fix: revertido para `string? productId` com `@ProductId::text IS NULL` em ambos os queries (COUNT + rows) — tipo text consistente para Npgsql, compatível com VARCHAR no PostgreSQL
- [x] `admin-restart.yml`: opção `logs` adicionada (SSH + `docker logs comprai-api --tail 100`) para diagnóstico rápido via GitHub Actions
- [x] Commits: `cf49acd` (intermediário) · `1883e1e` (ops/logs) · `4bada16` (fix definitivo)

---

## ✅ V069v2 — Fix Configurações 401 (out/2026)
- [x] `apps/admin/src/lib/fetch-proxy.ts` — wrapper `fetchProxy<T>()` com redirect automático para `/login` em 401
- [x] Proxy `/api/proxy/[...path]`: detecta 401 do backend, limpa `admin_token` + `admin_user` (maxAge=0)
- [x] `settings/page.tsx` e `dashboard/page.tsx` usam `fetchProxy` em vez de `fetch` direto
- [x] `seed/set-admin-role.sql` — script para elevar usuário existente para `role='admin'`

---

## ✅ V069 — Dashboard Administrativo (out/2026)
- [x] Stack: Next.js 15 App Router + Tailwind v4 | porta `3004` | `deploy-admin.yml`
- [x] Autenticação via JWT do backend — rota protegida por `role=admin`
- [x] Sidebar fixa com KPI cards: Pedidos Hoje, Receita Hoje, Taxa Conversão, Ticket Médio
- [x] Seção Vendas: gráfico de receita por dia, top produtos
- [x] Seção Pagamentos: distribuição por status (confirmado/pendente/falhou) e provider (pix/credit_card/debit_card)
- [x] Seção Entregas: em separação / despachados / entregues / atrasados
- [x] Seção Clientes: novos cadastros, verificados, com 2FA
- [x] Tabela de pedidos com modal de detalhe (timeline do pedido)
- [x] Endpoints: `GET /api/admin/stats/summary|orders|payments|fulfillment|customers|sales|top-products`
- [x] Fix: int → long em 19 campos de 7 DTOs (PostgreSQL `bigint` × Dapper posicional)
- [x] Fix: `AdminOrderRow.OrderId` string → `Guid` (PostgreSQL `uuid` → `System.Guid`)
- [x] `seed/seed.sql` — 20 clientes + 23 pedidos + itens + pagamentos (dados realistas BR)
- [x] `seed/clean.sql` — `TRUNCATE CASCADE` em todas as tabelas (dev/homolog)
- [x] Workaround documentado: `comprai-admin` não sobe automaticamente após deploy CI/CD
  - `docker compose up -d --no-deps comprai-admin`

---

## ✅ V068 — Varredura de Segurança Completa (out/2026)
- [x] `PII_ENCRYPTION_KEY` propagado via docker-compose ao container
- [x] CodeQL corrigido no `ci-cd.yml`
- [x] `security-scan.yml`: Gitleaks + dotnet --vulnerable + Trivy (`docker run aquasec/trivy:latest`) → `latest.json` com score 0–100 + badge secure/warning/critical
- [x] Fix trivy-action substituído por docker run direto (versão inexistente da action)
- [x] OpsWatch: cards KPI clicáveis (scrollTo + expand na seção correspondente)
- [x] Seções colapsáveis com chevron individual por item (Secrets, NuGet, CVEs Trivy, LGPD Checklist)
- [x] Drill-down por CVE/pacote/secret com detalhes expandíveis
- [x] Botão ⧉ copy por item (data-cp + navigator.clipboard, feedback ✓)
- [x] Botão ⬇ PDF — relatório HTML estruturado com print automático
- [x] Painel live de execução colapsável com status de jobs em tempo real
- [x] Modal glossário ℹ — 12 termos (CVE, SAST, Trivy, Gitleaks, CVSS, LGPD, severidades)
- [x] Fix `</script>` dentro de template literal (concatenação); fix `}}` duplicado; fix SyntaxError nos botões copy

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

