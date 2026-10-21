#!/usr/bin/env npx tsx
/**
 * linear-populate.ts
 *
 * Usage:
 *   npx tsx scripts/linear-populate.ts backfill          # populate all V052–V073
 *   npx tsx scripts/linear-populate.ts sync <version>   # sync one version (e.g. V074)
 *
 * Env vars:
 *   LINEAR_API_KEY  — Personal API key from linear.app
 */

import { LinearClient } from "@linear/sdk";
import { readFileSync } from "fs";
import { join } from "path";

const LINEAR_API_KEY = process.env.LINEAR_API_KEY!;
if (!LINEAR_API_KEY) {
  console.error("❌  LINEAR_API_KEY env var is required");
  process.exit(1);
}

// Diagnostic: print partial key so we can confirm the secret is set
console.log(`🔑 API key: ${LINEAR_API_KEY.slice(0, 8)}...${LINEAR_API_KEY.slice(-4)} (length: ${LINEAR_API_KEY.length})`);

const client = new LinearClient({ apiKey: LINEAR_API_KEY });

// ─── Colours for labels ──────────────────────────────────────────────────────
const LABEL_COLORS: Record<string, string> = {
  fix:      "#E11D48", // rose-600
  feat:     "#7C3AED", // violet-600
  chore:    "#64748B", // slate-500
  infra:    "#0284C7", // sky-600
  test:     "#059669", // emerald-600
  security: "#DC2626", // red-600
  e2e:      "#D97706", // amber-600
};

// ─── Version data ────────────────────────────────────────────────────────────
interface VersionItem {
  title: string;
  detail: string;
}

interface Version {
  id: string;           // e.g. "V072"
  date: string;         // ISO date "2026-10-19"
  type: "feat" | "fix" | "chore";
  sectionTitle: string; // e.g. "Email, OTP e Configuração"
  items: VersionItem[];
  status: "done" | "in_progress";
}

const VERSIONS: Version[] = [
  {
    id: "V052", date: "2026-10-06", type: "feat",
    sectionTitle: "BD como source of truth no fluxo de compra",
    status: "done",
    items: [
      { title: "CustomerDto expandido", detail: "Campos individuais de endereço adicionados ao CustomerDto" },
      { title: "Order.Id migrado para UUID", detail: "Order.Id migrado de string sequencial para UUID puro" },
      { title: "OrderRepository.SaveAsync", detail: "Persiste shipping_zip/street/number/complement/city/state" },
      { title: "PersistingPaymentAdapter", detail: "Insere fulfillment_event (payment_confirmed) + enqueue simulator" },
    ],
  },
  {
    id: "V053", date: "2026-10-06", type: "feat",
    sectionTitle: "Frontend localStorage + Mobile standalone + CI fix",
    status: "done",
    items: [
      { title: "saveOrderToListFallback", detail: "apiPersistedRef; handlePaymentConfirmed → getOrders" },
      { title: "Componentes mobile separados", detail: "Componentes mobile removidos de apps/web — vivem em apps/mobile" },
      { title: "CI smoke-tests robustez", detail: "timeout-minutes:10 + Newman --timeout-request 15000" },
    ],
  },
  {
    id: "V054", date: "2026-10-06", type: "feat",
    sectionTitle: "order_history + session + search_log no BD",
    status: "done",
    items: [
      { title: "ISessionPort + SessionRepository", detail: "get-or-create por clientSessionId; TTL 2h" },
      { title: "IOrderHistoryPort + OrderHistoryRepository", detail: "INSERT ON CONFLICT DO NOTHING" },
      { title: "SearchLogRepository", detail: "results_count, sources TEXT[], session_id via ISessionPort" },
      { title: "/api/search lê X-Session-Id", detail: "Header X-Session-Id adicionado ao endpoint de busca" },
    ],
  },
  {
    id: "V055", date: "2026-10-06", type: "feat",
    sectionTitle: "cart_snapshot + webhook_event idempotência + CI fixes",
    status: "done",
    items: [
      { title: "ICartSnapshotPort + CartSnapshotRepository", detail: "upsert ON CONFLICT em cart_snapshot" },
      { title: "IWebhookEventPort + WebhookEventRepository", detail: "INSERT ON CONFLICT DO NOTHING" },
      { title: "Idempotência webhooks Stripe e EFI", detail: "POST /webhook/stripe e /webhook/efi com idempotência via webhook_event" },
      { title: "CI fixes + COVERAGE_THRESHOLD 80%", detail: "Stubs Null*Port; cobertura mínima definida em 80%" },
    ],
  },
  {
    id: "V056", date: "2026-10-06", type: "feat",
    sectionTitle: "ICartSnapshotService (Clean Architecture) + Frontend session tracking",
    status: "done",
    items: [
      { title: "ICartSnapshotService em SharedKernel.Ports", detail: "Interface de contrato para snapshot de carrinho" },
      { title: "CartSnapshotService em Application.Cart", detail: "Implementação do serviço de snapshot" },
      { title: "NullCartSnapshotService stub", detail: "Stub ExcludeFromCodeCoverage para UsarPostgres=false" },
      { title: "AddToCartHandler + RemoveFromCartCommandHandler", detail: "Injetam ICartSnapshotService?" },
      { title: "CartSnapshotServiceTests (7 testes)", detail: "Cobertura de todos os caminhos do serviço de snapshot" },
      { title: "Frontend session tracking", detail: "X-Session-Id no search; sessionId no payment; restore carrinho abandonado" },
    ],
  },
  {
    id: "V057", date: "2026-10-07", type: "feat",
    sectionTitle: "Autenticação completa — JWT, BCrypt, SSO multi-plataforma",
    status: "done",
    items: [
      { title: "Autenticação JWT HS256 + BCrypt", detail: "POST /api/auth/register|login|callback + GET /api/auth/me" },
      { title: "Vínculo order.customer_id", detail: "order.customer_id e session.customer_id vinculados ao usuário autenticado" },
      { title: "Telas /account e /profile", detail: "Dados da conta e histórico de pedidos + repetir pedido" },
      { title: "NextAuth.js com Google/GitHub/Microsoft", detail: "Tela login dark/laranja com SSO multi-provedor" },
      { title: "V002__auth_schema.sql", detail: "Colunas provider, provider_id, password_hash, avatar_url, email_verified" },
    ],
  },
  {
    id: "V058", date: "2026-10-07", type: "feat",
    sectionTitle: "Perfil editável + Endereço de entrega + Login redesign",
    status: "done",
    items: [
      { title: "PUT /api/auth/me", detail: "Editar nome, telefone e documento do cliente" },
      { title: "POST /api/auth/me/address", detail: "Salvar endereço de entrega padrão" },
      { title: "GET /api/auth/me com endereços", detail: "Retorna customer_address junto ao perfil" },
      { title: "Tela /account com formulário", detail: "Formulário de edição de perfil e endereço de entrega" },
      { title: "Login redesign minimalista", detail: "Fundo branco, botão roxo #7c3aed" },
      { title: "Next-auth no mobile", detail: "Google, GitHub, Microsoft, Credentials + secrets SSO no docker run" },
    ],
  },
  {
    id: "V059", date: "2026-10-07", type: "feat",
    sectionTitle: "Segurança F1 (Headers + Rate Limit) + F2 (Refresh Token)",
    status: "done",
    items: [
      { title: "SecurityHeadersMiddleware", detail: "CSP, HSTS, X-Frame-Options, X-Content-Type-Options, Referrer-Policy" },
      { title: "Rate limiting SlidingWindow 5 req/60s", detail: "Brute force prevention em /api/auth/login e /api/auth/register" },
      { title: "Migration V004__refresh_token.sql", detail: "Colunas refresh_token_hash + refresh_token_expires_at + índice" },
      { title: "Refresh token opaque SHA-256", detail: "JwtService.GenerateRefreshToken / HashRefreshToken" },
      { title: "POST /api/auth/refresh + /logout", detail: "Rotação de refresh token; revogação; access_token 15min, refresh 7 dias" },
    ],
  },
  {
    id: "V060", date: "2026-10-07", type: "feat",
    sectionTitle: "Segurança F1 (Verificação de Email) + F2 (2FA TOTP)",
    status: "done",
    items: [
      { title: "F1 — Verificação de email pós-cadastro", detail: "OTP via Resend API; POST /api/auth/verify-email; Migration V005__email_verification.sql" },
      { title: "F2 — 2FA TOTP via Otp.NET v1.4.0", detail: "TotpService: GenerateSecret, BuildOtpAuthUri, Verify (±1 step window)" },
      { title: "Migration V006__totp.sql", detail: "Colunas totp_secret + totp_enabled na tabela customer" },
      { title: "4 endpoints 2FA", detail: "POST /api/auth/2fa/setup, /enable, /verify, /disable" },
      { title: "Frontend /auth/2fa/setup e /verify", detail: "QR Code via api.qrserver.com; lê tempToken de ?t= ou sessionStorage" },
    ],
  },
  {
    id: "V061", date: "2026-10-08", type: "feat",
    sectionTitle: "Segurança F3 (Recuperação de Senha) + F4 (Logout-All)",
    status: "done",
    items: [
      { title: "Migration V007__password_reset.sql", detail: "Colunas password_reset_token_hash TEXT + password_reset_expires_at TIMESTAMPTZ + índice parcial" },
      { title: "POST /api/auth/forgot-password", detail: "Token UUID→SHA-256; e-mail best-effort; sempre responde 204 (anti-enumeração)" },
      { title: "POST /api/auth/reset-password", detail: "Valida hash; exige senha mín. 8 chars; BCrypt no Infrastructure layer" },
      { title: "Frontend forgot-password e reset-password", detail: "Formulário email + estado de sucesso; lê ?token= da URL" },
      { title: "POST /api/auth/logout-all", detail: "Revoga todos os refresh tokens do customer; RequireAuthorization" },
    ],
  },
  {
    id: "V063", date: "2026-10-08", type: "feat",
    sectionTitle: "E2E Testing Infrastructure (Fase 1)",
    status: "done",
    items: [
      { title: "global-setup.ts com retry automático", detail: "Login único antes de todos os testes; 4 tentativas com 65s de espera; salva storageState.json" },
      { title: "playwright.config.ts configurado", detail: "globalSetup + storageState; reutiliza sessão autenticada sem login individual por teste" },
      { title: "RateLimit.Auth configurável", detail: "AuthWindowOptions; override via RateLimit__Auth__PermitLimit=30 (E2E/dev)" },
    ],
  },
  {
    id: "V064", date: "2026-10-08", type: "fix",
    sectionTitle: "E2E Testing: 14 falhas → 0 falhas",
    status: "done",
    items: [
      { title: "Fix strict mode violation em text=Entrar", detail: "Substituição por page.getByRole('button', { name: 'Entrar' }).first() em 4 specs" },
      { title: "Fix auth.spec.ts test 7 redirect sem auth", detail: "test.use({ storageState: { cookies: [], origins: [] } }) para ignorar sessão" },
      { title: "purchase-flow.spec.ts reescrita completa", detail: "Fluxo híbrido API + UI; rate limit 429 → test.skip; tolerância a ECONNREFUSED" },
      { title: "Fix /api/search parâmetros de paginação", detail: "page e pageSize com defaults page=1, pageSize=10; resolve CS1737 no build" },
    ],
  },
  {
    id: "V065", date: "2026-10-08", type: "fix",
    sectionTitle: "Endpoint de Pagamento E2E + OpsWatch Drill-Down UX",
    status: "done",
    items: [
      { title: "Fix createPayment() endpoint", detail: "POST /api/payment/{orderId} (era sem orderId); body e headers corrigidos" },
      { title: "OpsWatch — execução de workflow drill-down", detail: "e2eRenderJobsDrilldown(): steps colapsáveis com ícone e cor por status" },
      { title: "OpsWatch — specs por suite clicáveis", detail: "Todos os specs com onclick; modal com título colorido por status" },
    ],
  },
  {
    id: "V066", date: "2026-10-09", type: "feat",
    sectionTitle: "E2E: Baixar Todos + Request/Response Evidence (HAR)",
    status: "done",
    items: [
      { title: "Botão 'Baixar Todos os Testes'", detail: "e2eDownloadTodos() no OpsWatch; dlTodos() no report.html" },
      { title: "Captura HAR por teste", detail: "playwright.config.ts: recordHar; Python norm_har_key() com Jaccard ≥ 0.5" },
      { title: "OpsWatch: req_headers + req_body + res_body", detail: "Seção '🌐 Requisições HTTP (N)' com details por entrada" },
      { title: "report.html melhorias", detail: "Drilldown + player HTML; chevron; filtro; timezone BRT; duração; download evidências" },
    ],
  },
  {
    id: "V067", date: "2026-10-10", type: "feat",
    sectionTitle: "HAR per-test Fix + Postman Collection Export",
    status: "done",
    items: [
      { title: "Fix HAR capture por teste", detail: "fixtures.ts com override de context; har-evidence/{safe-title}-chromium.har; causa raiz: path sem extensão criava FILE" },
      { title: "Botão global 'Baixar Collection Postman'", detail: "_e2eToPostmanCollection(specs, name); URL parsed; headers convertidos; savedResponses" },
      { title: "Ícones copy/postman por linha de teste", detail: "SVG 11×11px; clipboard feedback verde; e2eCopySpec(idx, btn) / e2eDownloadSpecPostman(idx)" },
    ],
  },
  {
    id: "V068", date: "2026-10-10", type: "feat",
    sectionTitle: "Varredura de Segurança Completa (OpsWatch + CI/CD)",
    status: "done",
    items: [
      { title: "Hardening de infra CI/CD", detail: "PII_ENCRYPTION_KEY via docker-compose; CodeQL corrigido; CORS sem wildcard; HSTS; Postgres sem superuser" },
      { title: "Workflow security-scan.yml", detail: "Gitleaks + dotnet-vulnerable + Trivy via docker run; Python consolida → score geral 0–100" },
      { title: "OpsWatch — Seção Varredura de Segurança", detail: "Botão ▶ Executar; cards KPI clicáveis; seções colapsáveis; drill-down por item; botão ⬇ PDF" },
      { title: "Modal glossário ℹ com 12 termos", detail: "CVE, SAST, Trivy, Gitleaks, CVSS, LGPD, severidades, Score Geral" },
    ],
  },
  {
    id: "V069", date: "2026-10-14", type: "fix",
    sectionTitle: "Dashboard Administrativo: Correções Dapper + Seed Data",
    status: "done",
    items: [
      { title: "Fix int → long nos DTOs AdminQueryRepository", detail: "PostgreSQL COUNT(*)/SUM() retornam bigint; corrigidos 19 campos em 7 DTOs" },
      { title: "Fix AdminOrderRow.OrderId string → Guid", detail: "PostgreSQL coluna uuid mapeada para System.Guid pelo Dapper" },
      { title: "Seed data seed.sql + clean.sql", detail: "20 clientes + 23 pedidos (delivered/shipped/processing/confirmed/pending/cancelled)" },
    ],
  },
  {
    id: "V069v2", date: "2026-10-14", type: "fix",
    sectionTitle: "Admin: Página Configurações retornava 401",
    status: "done",
    items: [
      { title: "fetchProxy<T>() utility", detail: "Redireciona para /login em qualquer 401; limpa admin_token + admin_user cookies" },
      { title: "Proxy /api/proxy/[...path]", detail: "Detecta 401 do backend; Set-Cookie maxAge=0 antes de retornar" },
      { title: "seed/set-admin-role.sql", detail: "Script para elevar usuário existente para role='admin' via UPDATE" },
    ],
  },
  {
    id: "V069v3", date: "2026-10-15", type: "fix",
    sectionTitle: "Admin Orders: HTTP 500 em /api/admin/orders",
    status: "done",
    items: [
      { title: "Fix Npgsql UUID/text type conflict", detail: "productId mantido como string?; @ProductId::text consistente em ambas as ocorrências" },
      { title: "admin-restart.yml opção logs", detail: "SSH → docker logs comprai-api --tail 100 para facilitar diagnóstico" },
    ],
  },
  {
    id: "V069v4", date: "2026-10-16", type: "fix",
    sectionTitle: "Fluxo Completo de Reset de Senha",
    status: "done",
    items: [
      { title: "Vars Brevo no docker-compose raiz", detail: "Brevo__ApiKey, Brevo__Login, Brevo__From adicionados ao serviço comprai-api" },
      { title: "Fix rota /reset-password no middleware", detail: "pathname.startsWith('/reset-password') adicionado nas rotas públicas do admin" },
      { title: "Fix proxy reset-password 200→400", detail: "Substituído status === 204 por backendRes.ok (200–299)" },
      { title: "Deploy desacoplado do SonarCloud", detail: "ci-cd.yml: job de deploy não depende mais do job SonarCloud" },
    ],
  },
  {
    id: "V070", date: "2026-10-16", type: "chore",
    sectionTitle: "Reorganização de Pastas + Estabilização CI/CD",
    status: "done",
    items: [
      { title: "Renomeações de pasta", detail: "apps/web→frontend/web; apps/mobile→frontend/mobile; apps/admin→frontend/admin; apps/e2e→tests/e2e; newman→tests/newman; k6→opswatch" },
      { title: "Fix Dockerfile opswatch path", detail: "COPY k6/ → COPY opswatch/" },
      { title: "Fix eslint-config-next e tailwindcss", detail: "eslint-config-next ^14→^15.5.27; tailwindcss ^4→^3.4.17 (stable)" },
      { title: "Fix Deploy Web health check", detail: "/api/healthz criado; sleep 8→20; curl / → curl /api/healthz" },
    ],
  },
  {
    id: "V071", date: "2026-10-17", type: "feat",
    sectionTitle: "Mobile UI Redesign (frontend/mobile-v2/, porta 3005)",
    status: "done",
    items: [
      { title: "Next.js 15 + React 19 + TypeScript strict", detail: "NextAuth v5; Tailwind CSS v3.4.17; porta 3005; visual Nixtio Skincare (branco/preto/roxo #7C3AED)" },
      { title: "Fluxo completo de compra", detail: "10 telas: Home/Search/Cart/Checkout/Account/Orders/OrderDetail/Login/Register/VerifyEmail" },
      { title: "18 componentes criados", detail: "Button, Input, Spinner, AppShell, BottomNav, SearchBar, ProductCard, CartItemRow, CartSummary, TrackingTimeline e outros" },
      { title: "Auth/session com pre-auth bypass", detail: "session.user.apiToken via NextAuth callbacks; register retorna token → signIn sem segunda chamada" },
      { title: "Infra Docker + CI/CD", detail: "Dockerfile multi-stage node:22-alpine; deploy-mobile-v2.yml; docker-compose porta 3005" },
    ],
  },
  {
    id: "V071.1", date: "2026-10-17", type: "fix",
    sectionTitle: "mobile-v2: estabilização pós-deploy",
    status: "done",
    items: [
      { title: "Dark/Light mode", detail: "Botão sol/lua no topo direito via next-themes; default light; suppressHydrationWarning" },
      { title: "Renomear 'Checkout' → 'Finalizar Pedido'", detail: "checkout/page.tsx + CartSummary.tsx" },
      { title: "Fix Docker port mapping", detail: "-p 3005:3000 → -p 3005:3005 + -e PORT=3005" },
      { title: "Fix Auth.js UntrustedHost", detail: "-e AUTH_TRUST_HOST=true adicionado ao docker run e workflow" },
      { title: "Fix CI network name", detail: "--network comprai-network → --network comprai_comprai-network" },
      { title: "Fix npm ci no Docker", detail: "package-lock.json não tinha next-themes; corrigido com npm install --package-lock-only" },
    ],
  },
  {
    id: "V072", date: "2026-10-19", type: "fix",
    sectionTitle: "Email, OTP e Configuração",
    status: "done",
    items: [
      { title: "Remetente Brevo corrigido no deploy", detail: "deploy/docker-compose.yml: Brevo__From: helioandrade@hotmail.com (era outlook freemail sem DMARC)" },
      { title: "Remetente Brevo na raiz e EmailService", detail: "docker-compose.yml e EmailService.cs atualizados com fallback helioandrade@hotmail.com" },
      { title: "OTP alfanumérico garantido", detail: "GenerateOtp() refatorado: mín. 2 dígitos + 4 letras com RandomNumberGenerator.Shuffle" },
      { title: "appsettings.json — seção Brevo", detail: "EmailService.cs lê config[\"Brevo:From\"] em vez de hardcode" },
      { title: "CI/CD — liberar porta 5020 via ufw", detail: "ufw allow ${{ env.API_PORT }}/tcp || true adicionado no script de deploy" },
    ],
  },
  {
    id: "V073", date: "2026-10-21", type: "fix",
    sectionTitle: "CI/CD Deploy Fix + Linear Integration",
    status: "done",
    items: [
      { title: "Fix CompraiDbInitializer bloqueava HTTP server", detail: "Movido de pré-app.Run() para ApplicationStarted + Task.Run background; /health/live responde imediatamente" },
      { title: "Fix App__WebUrl porta 3000 → 3005", detail: "deploy/docker-compose.yml: WEB_APP_URL default corrigido para porta 3005" },
      { title: "CI/CD healthcheck melhorado", detail: "12×10s→18×10s; container status check; diagnostics: docker ps -a, logs, curl -v" },
      { title: "Integração Linear (este script)", detail: "Workflows linear-backfill.yml e linear-sync.yml; population retroativa V052–V073" },
    ],
  },
  {
    id: "V074", date: "2026-10-21", type: "fix",
    sectionTitle: "Smoke Tests + Qualidade",
    status: "in_progress",
    items: [
      { title: "Fix smoke tests — Features__UsarRedis", detail: "docker-compose.yml: Features__UsarRedis 'false' → 'true'; produção estava usando MockCheckoutPort/MockOrderPort em vez de RedisCheckoutAdapter/RedisOrderAdapter" },
    ],
  },
];

// ─── Helpers ─────────────────────────────────────────────────────────────────

const sleep = (ms: number) => new Promise((r) => setTimeout(r, ms));

async function getOrCreateLabel(
  teamId: string,
  name: string,
  color: string
): Promise<string> {
  const labels = await client.issueLabels({ filter: { team: { id: { eq: teamId } } } });
  const existing = labels.nodes.find((l) => l.name === name);
  if (existing) {
    console.log(`  ↳ label "${name}" já existe`);
    return existing.id;
  }
  const payload = await client.createIssueLabel({ name, color, teamId });
  if (!payload.success || !payload.issueLabel) throw new Error(`Failed to create label ${name}`);
  const label = await payload.issueLabel;
  console.log(`  ✅ label "${name}" criado`);
  return label!.id;
}

async function getOrCreateProject(
  teamId: string,
  name: string
): Promise<string> {
  const projects = await client.projects({ filter: { name: { eq: name } } });
  const existing = projects.nodes[0];
  if (existing) {
    console.log(`↳ projeto "${name}" já existe`);
    return existing.id;
  }
  const payload = await client.createProject({
    name,
    teamIds: [teamId],
    description: "Plataforma de e-commerce com IA conversacional — compradores encontram e compram via chat",
    color: "#7C3AED",
  });
  if (!payload.success || !payload.project) throw new Error("Failed to create project");
  const project = await payload.project;
  console.log(`✅ projeto "${name}" criado`);
  return project!.id;
}

async function getOrCreateCycle(
  teamId: string,
  versionId: string,
  date: string
): Promise<string | undefined> {
  try {
    const cycles = await client.cycles({ filter: { team: { id: { eq: teamId } } } });
    const existing = cycles.nodes.find((c) => c.name === versionId);
    if (existing) {
      console.log(`  ↳ cycle "${versionId}" já existe`);
      return existing.id;
    }
    // Give each version a 1-hour window offset by version number to avoid overlapping
    const startsAt = new Date(date + "T00:00:00Z");
    const endsAt = new Date(date + "T23:59:59Z");
    const payload = await client.createCycle({
      teamId,
      name: versionId,
      startsAt,
      endsAt,
    });
    if (!payload.success || !payload.cycle) {
      console.warn(`  ⚠️  cycle "${versionId}" não criado — continuando sem cycle`);
      return undefined;
    }
    const cycle = await payload.cycle;
    console.log(`  ✅ cycle "${versionId}" criado`);
    return cycle!.id;
  } catch (e) {
    const msg = e instanceof Error ? e.message : String(e);
    console.warn(`  ⚠️  cycle "${versionId}" falhou (${msg}) — continuando sem cycle`);
    return undefined;
  }
}

async function findStateId(
  teamId: string,
  type: "completed" | "started" | "unstarted"
): Promise<string> {
  const states = await client.workflowStates({ filter: { team: { id: { eq: teamId } } } });
  const state = states.nodes.find((s) => s.type === type);
  if (!state) throw new Error(`No workflow state of type "${type}" found for team`);
  return state.id;
}

async function issueExists(teamId: string, title: string): Promise<string | null> {
  const issues = await client.issues({
    filter: {
      team: { id: { eq: teamId } },
      title: { eq: title },
    },
  });
  return issues.nodes[0]?.id ?? null;
}

// Kept for backward compat (alias)
const epicExists = issueExists;

async function deleteDuplicates(teamId: string, title: string): Promise<void> {
  const issues = await client.issues({
    filter: {
      team: { id: { eq: teamId } },
      title: { eq: title },
    },
  });
  // Keep only the first (oldest), delete the rest
  const dupes = issues.nodes.slice(1);
  for (const dupe of dupes) {
    await client.deleteIssue(dupe.id);
    console.log(`  🗑️  Duplicata removida: "${title}" (${dupe.id})`);
    await sleep(200);
  }
}

// ─── Main functions ───────────────────────────────────────────────────────────

async function backfill() {
  console.log("🚀 Linear backfill — V052 → V073\n");

  // 1. Discover team
  console.log("📡 Conectando à Linear API...");
  let teams;
  try {
    teams = await client.teams();
  } catch (e) {
    const err = e as Record<string, unknown>;
    console.error("❌ Falha ao buscar teams:");
    if (err["message"]) console.error(" message:", err["message"]);
    if (err["status"]) console.error(" status:", err["status"]);
    if (err["type"]) console.error(" type:", err["type"]);
    if (err["errors"]) console.error(" errors:", JSON.stringify(err["errors"]));
    throw e;
  }
  if (teams.nodes.length === 0) throw new Error("No Linear teams found");
  const team = teams.nodes[0];
  console.log(`👥 Team: ${team.name} (${team.id})\n`);

  // 2. Create/find project
  console.log("📁 Verificando projeto...");
  const projectId = await getOrCreateProject(team.id, "Comprai Platform");
  console.log();

  // 3. Create labels
  console.log("🏷️  Criando labels...");
  const labelIds: Record<string, string> = {};
  for (const [name, color] of Object.entries(LABEL_COLORS)) {
    labelIds[name] = await getOrCreateLabel(team.id, name, color);
    await sleep(300);
  }
  console.log();

  // 4. Find workflow states
  const doneStateId = await findStateId(team.id, "completed");
  const inProgressStateId = await findStateId(team.id, "started");
  console.log(`📊 States: Done=${doneStateId}, InProgress=${inProgressStateId}\n`);

  // 5. Process only "done" versions (in_progress = current sprint, handled by sync)
  const doneVersions = VERSIONS.filter((v) => v.status === "done");
  console.log(`📋 Versões a popular: ${doneVersions.map((v) => v.id).join(", ")}\n`);

  for (const version of doneVersions) {
    console.log(`\n─── ${version.id} — ${version.sectionTitle} ───`);

    // Create cycle
    const cycleId = await getOrCreateCycle(team.id, version.id, version.date);
    await sleep(400);

    const stateId = version.status === "done" ? doneStateId : inProgressStateId;
    const labelId = labelIds[version.type] ?? labelIds["feat"];

    // Create epic (parent issue)
    const epicTitle = `${version.id} — ${version.sectionTitle}`;
    const existingEpicId = await epicExists(team.id, epicTitle);

    let epicId: string;
    if (existingEpicId) {
      console.log(`  ↳ épico "${epicTitle}" já existe`);
      epicId = existingEpicId;
    } else {
      const epicPayload = await client.createIssue({
        title: epicTitle,
        teamId: team.id,
        projectId,
        labelIds: [labelId],
        stateId,
        ...(cycleId ? { cycleId } : {}),
        description: `**Versão:** ${version.id}\n**Data:** ${version.date}\n**Tipo:** ${version.type}\n\n${version.items.map((i) => `- **${i.title}**: ${i.detail}`).join("\n")}`,
      });
      if (!epicPayload.success || !epicPayload.issue) {
        console.error(`  ❌ Falha ao criar épico "${epicTitle}"`);
        continue;
      }
      const epic = await epicPayload.issue;
      epicId = epic!.id;
      console.log(`  ✅ épico criado: "${epicTitle}"`);
      await sleep(400);
    }

    // Create sub-issues (idempotent: skip if already exists)
    for (const item of version.items) {
      const subTitle = `${version.id} — ${item.title}`;
      await sleep(300);

      const existingSubId = await issueExists(team.id, subTitle);
      if (existingSubId) {
        console.log(`    ↳ sub-issue já existe: "${subTitle}"`);
        continue;
      }

      const subPayload = await client.createIssue({
        title: subTitle,
        teamId: team.id,
        projectId,
        labelIds: [labelId],
        stateId,
        ...(cycleId ? { cycleId } : {}),
        parentId: epicId,
        description: item.detail,
      });
      if (!subPayload.success) {
        console.error(`    ❌ Falha ao criar sub-issue "${subTitle}"`);
        continue;
      }
      console.log(`    ✅ "${subTitle}"`);
    }
  }

  console.log("\n\n✅ Backfill concluído!");
}

async function syncVersion(versionId: string) {
  // If versionId not provided, read from CHANGELOG
  let version: Version | undefined;

  if (versionId) {
    version = VERSIONS.find((v) => v.id === versionId);
  }

  if (!version) {
    // Try to parse from CHANGELOG
    const changelogPath = join(process.cwd(), "CHANGELOG.md");
    const changelog = readFileSync(changelogPath, "utf-8");
    const match = changelog.match(/^## \[([^\]]+)\] — (\d{4}-\d{2}-\d{2})/m);
    if (!match) throw new Error("Could not parse version from CHANGELOG.md");

    const parsedId = match[1];
    const parsedDate = match[2];

    // Check if it's already in our list
    version = VERSIONS.find((v) => v.id === parsedId);
    if (!version) {
      // Create a minimal entry from CHANGELOG
      const sectionMatch = changelog.match(/### (?:Fixed|Added|Changed) — (.+)/);
      version = {
        id: parsedId,
        date: parsedDate,
        type: "feat",
        sectionTitle: sectionMatch?.[1] ?? parsedId,
        status: "in_progress",
        items: [{ title: "Ver CHANGELOG.md", detail: `Versão ${parsedId} — ${parsedDate}` }],
      };
      console.log(`⚠️  Versão ${parsedId} não encontrada na lista embutida — usando dados do CHANGELOG`);
    }
  }

  console.log(`🔄 Sync: ${version.id} — ${version.sectionTitle}\n`);

  const teams = await client.teams();
  const team = teams.nodes[0];
  console.log(`👥 Team: ${team.name} (${team.id})`);

  const projects = await client.projects({ filter: { name: { eq: "Comprai Platform" } } });
  const projectId = projects.nodes[0]?.id;
  if (!projectId) throw new Error("Projeto 'Comprai Platform' não encontrado — execute o backfill primeiro");

  const labelIds: Record<string, string> = {};
  const labels = await client.issueLabels({ filter: { team: { id: { eq: team.id } } } });
  for (const l of labels.nodes) {
    if (l.name in LABEL_COLORS) labelIds[l.name] = l.id;
  }

  const doneStateId = await findStateId(team.id, "completed");
  const inProgressStateId = await findStateId(team.id, "started");

  const stateId = version.status === "in_progress" ? inProgressStateId : doneStateId;
  const labelId = labelIds[version.type] ?? labelIds["feat"] ?? Object.values(labelIds)[0];

  // Close previous version (mark as done)
  const prevVersion = VERSIONS[VERSIONS.findIndex((v) => v.id === version!.id) - 1];
  if (prevVersion) {
    const prevTitle = `${prevVersion.id} — ${prevVersion.sectionTitle}`;
    const prevIssues = await client.issues({
      filter: { team: { id: { eq: team.id } }, title: { startsWith: `${prevVersion.id} —` } },
    });
    for (const issue of prevIssues.nodes) {
      await client.updateIssue(issue.id, { stateId: doneStateId });
      console.log(`  ✅ ${prevVersion.id} marcado como Done: "${issue.title}"`);
      await sleep(200);
    }
  }

  // Create cycle + issues for current version
  const cycleId = await getOrCreateCycle(team.id, version.id, version.date);
  await sleep(400);

  const epicTitle = `${version.id} — ${version.sectionTitle}`;
  const existingEpicId = await epicExists(team.id, epicTitle);

  if (existingEpicId) {
    console.log(`↳ Épico "${epicTitle}" já existe — atualizando estado...`);
    await client.updateIssue(existingEpicId, { stateId });
    // Atualiza sub-issues existentes para o mesmo estado
    const subIssues = await client.issues({
      filter: { team: { id: { eq: team.id } }, parent: { id: { eq: existingEpicId } } },
    });
    for (const sub of subIssues.nodes) {
      await client.updateIssue(sub.id, { stateId });
      console.log(`  ✅ ${version.id} marcado como ${version.status === "done" ? "Done" : "In Progress"}: "${sub.title}"`);
      await sleep(200);
    }
  } else {
    const epicPayload = await client.createIssue({
      title: epicTitle,
      teamId: team.id,
      projectId,
      labelIds: [labelId],
      stateId,
      ...(cycleId ? { cycleId } : {}),
      description: `**Versão:** ${version.id}\n**Data:** ${version.date}\n**Tipo:** ${version.type}\n\n${version.items.map((i) => `- **${i.title}**: ${i.detail}`).join("\n")}`,
    });
    if (!epicPayload.success || !epicPayload.issue) throw new Error("Falha ao criar épico");
    const epic = await epicPayload.issue;
    const epicId = epic!.id;
    console.log(`✅ Épico criado: "${epicTitle}"`);

    for (const item of version.items) {
      const subTitle = `${version.id} — ${item.title}`;
      await sleep(300);
      const subPayload = await client.createIssue({
        title: subTitle,
        teamId: team.id,
        projectId,
        labelIds: [labelId],
        stateId,
        ...(cycleId ? { cycleId } : {}),
        parentId: epicId,
        description: item.detail,
      });
      if (!subPayload.success) {
        console.error(`  ❌ Falha: "${subTitle}"`);
        continue;
      }
      console.log(`  ✅ "${subTitle}"`);
    }
  }

  console.log(`\n✅ Sync ${version.id} concluído!`);
}

// ─── Entry point ──────────────────────────────────────────────────────────────
const [, , command, arg] = process.argv;

function handleError(e: unknown): never {
  if (e instanceof Error) {
    console.error("❌ ERROR:", e.message);
    if (e.stack) console.error("Stack:", e.stack);
    // Linear SDK errors have extra fields
    const le = e as Record<string, unknown>;
    if (le["type"]) console.error("Type:", le["type"]);
    if (le["status"]) console.error("HTTP Status:", le["status"]);
    if (le["errors"]) console.error("GraphQL errors:", JSON.stringify(le["errors"], null, 2));
    if (le["raw"]) console.error("Raw:", le["raw"]);
  } else {
    console.error("❌ UNKNOWN ERROR:", JSON.stringify(e, null, 2));
  }
  process.exit(1);
}

async function cleanupVersion(versionId: string) {
  console.log(`🧹 Limpando duplicatas de ${versionId}...\n`);
  const teams = await client.teams();
  const team = teams.nodes[0];
  console.log(`👥 Team: ${team.name} (${team.id})`);

  // Find all issues with this version prefix
  const issues = await client.issues({
    filter: {
      team: { id: { eq: team.id } },
      title: { startsWith: `${versionId} —` },
    },
  });

  console.log(`  Encontradas ${issues.nodes.length} issues com prefixo "${versionId} —"`);

  // Group by title, keep oldest (lowest createdAt), delete the rest
  const byTitle = new Map<string, typeof issues.nodes>();
  for (const issue of issues.nodes) {
    const list = byTitle.get(issue.title) ?? [];
    list.push(issue);
    byTitle.set(issue.title, list);
  }

  let deleted = 0;
  for (const [title, dupes] of byTitle.entries()) {
    if (dupes.length <= 1) continue;
    // Sort by createdAt ascending, keep first, delete rest
    dupes.sort((a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime());
    for (const dupe of dupes.slice(1)) {
      await client.deleteIssue(dupe.id);
      console.log(`  🗑️  Removida duplicata: "${title}" (${dupe.identifier})`);
      deleted++;
      await sleep(200);
    }
  }

  console.log(`\n✅ Limpeza concluída — ${deleted} duplicata(s) removida(s).`);
}

if (command === "backfill") {
  backfill().catch(handleError);
} else if (command === "sync") {
  syncVersion(arg ?? "").catch(handleError);
} else if (command === "cleanup") {
  if (!arg) { console.error("Usage: cleanup <version>  ex: cleanup V073"); process.exit(1); }
  cleanupVersion(arg).catch(handleError);
} else {
  console.error("Usage: npx tsx scripts/linear-populate.ts <backfill|sync|cleanup> [version]");
  process.exit(1);
}
