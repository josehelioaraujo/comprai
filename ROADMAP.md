# 🗺️ Roadmap — Comprai

> Última atualização: 2026-09-30 · Versão atual: v4.7.0 (V041)

## ✅ Concluído

### Infraestrutura & CI/CD
- Pipeline CI/CD completo (unit → integration → sonar → deploy → smoke)
- SonarCloud Quality Gate (Coverage 97%+, Mutation Score 84%+)
- OWASP ZAP Pentest automatizado
- Versionamento SemVer automático via `github.run_number`
- Code Review automático em PRs via Groq AI (PT-BR)
- Trees API para commits atômicos sem push bloqueado por proxy

### Observabilidade
- OTel pipeline dual: Datadog + New Relic via `comprai-otel-collector`
- 17 instrumentos `UcpMetrics` (Counter + Histogram) no SharedKernel
- Proxy `/api/observability/*` → Prometheus / Loki / Jaeger
- Access Log via Loki com filtros, paginação e drill-down
- Health Map SVG interativo com Container Stats e dependências reais

### OpsWatch Dashboard
- Gauge SVG do Apdex com zonas e ponteiro dinâmico
- Drill-down New Relic APM: Satisfied/Tolerating/Frustrated com rotas, p50/p90/p99, Req/s, Avg
- Sparkline interativo com tooltip hover e área preenchida
- Seletor de janela de tempo 5m → 24h
- Chevron colapsável universal em todas as seções
- Cards Req/min: peak/avg do TIMESERIES + sparkline throughput
- Cards p95: p50/p75/p95/p99 + sparkline latência
- Fix: NerdGraph retorna percentis como objeto aninhado `{"50": 1808.0}`
- Quality Score gauge + drill-down por dimensão
- Complexidade Ciclomática + Dependências Ca/Ce (Robert C. Martin)
- Monitor de Containers com ações Start/Stop/Restart
- Painel K6 com gráfico ao vivo durante stress test
- Painel Pentest OWASP ZAP com findings por severidade e PDF download

---

## ✅ V041 — Health Checks, Mensageria, Observabilidade, CI/CD (2026-09-30)

| Item | Status |
|------|--------|
| Health checks reais (`/health/live`, `/health/ready`, `/api/health/status`) | ✅ |
| Kafka ativado — eventos de domínio UCP | ✅ |
| RabbitMQ ativado — notificações ao usuário | ✅ |
| Email real via Resend SDK | ✅ |
| IdempotencyMiddleware (Redis TTL 24h) | ✅ |
| OTel Logs → Datadog + New Relic | ✅ |
| OpsWatch seção Mensageria (Kafka + RabbitMQ) | ✅ |
| Build imagem no CI → ghcr.io (Buildx + cache GHA) | ✅ |
| Deploy seguro pull-before-down | ✅ |
| Feature flags reais no CI (Redis/Kafka/RabbitMQ) | ✅ |

---

## 🔜 V042 — Resiliência & Outbox

**Palavra mágica: V042_UCP_COMPRAS**

| Item | Descrição | Prioridade |
|------|-----------|------------|
| **Outbox Pattern** | Tabela outbox → OutboxWorker → Kafka → consumer verifica Redis TTL 24h | 🔴 Alta |
| **DLQ + Retry** | Dead Letter Queue `notifications.dlq` com jitter backoff no EmailWorker | 🔴 Alta |
| **`PATCH /api/orders/{id}/status`** | `ucp.order.updated` nunca publicado — falta endpoint de atualização | 🟡 Média |
| **Circuit Breaker Ollama** | `/api/search` avg 12110ms — timeout agressivo + fallback | 🟡 Média |
| **Cache drill New Relic** | `/api/observability/newrelic/drill` p99 ~1863ms — cache 30s | 🟡 Média |

---

## 🔜 V041 — Gargalos & Performance

**Prioridade: Alta** | Investigar gap p50 (3ms) vs p95 (2016ms)

| Item | Descrição |
|------|-----------|
| 🔴 `/api/health/status` | avg 2843ms no Frustrated — health check com dependências síncronas (Redis/Kafka/DB). Separar `/health/live` (só processo) de `/health/ready` (dependências) |
| 🔴 `/api/search` | avg 12110ms no Frustrated — Ollama/LLM sem timeout agressivo. Implementar timeout + circuit breaker com fallback |
| 🟡 Tolerating → Frustrated | `/api/observability/newrelic/drill` com p99 ~1863ms — próprias queries NR lentas. Cache de 30s nos resultados drill |
| 🟡 OpsWatch hint | Texto `< 500ms (T)` nos cards bucket melhorar visibilidade |

---

## 🔜 V041–V042 — LLM & Inteligência

| Item | Descrição | Impacto |
|------|-----------|---------|
| **LLM Diagnóstico** | `/api/ai/analyze` — Ollama analisa p95 + logs + mutation score → diagnóstico PT-BR; botão "Analisar" no OpsWatch | Alto |
| **Notificações Slack/Teams** | Webhook com mensagem gerada por LLM ao detectar degradação (p95 > threshold) | Alto |
| **HERMES_ORCHESTRATOR** | NousResearch Hermes-3 via Ollama, agente autônomo com tools (search/cart/checkout) | Médio |
| **Ollama na VPS** | `docker run ollama/ollama` + `pull gemma3:latest` — pendente desde V033 | Médio |

---

## 🔜 V042 — Demo Web Chat + WhatsApp Bot (UCP Conversacional)

**Palavra mágica: V042_UCP_COMPRAS**

### Arquitetura — Canal como Porta Plugável

```
IChannelPort (Domain)
├── WebChatChannelAdapter  → SignalR Hub (IHubContext<ChatHub>)
├── WhatsAppChannelAdapter → Meta Cloud API / Baileys (dev)
└── ZapChannelAdapter      → plugin futuro (src/plugins/)
```

- `SendMessageAsync`, `SendProductListAsync`, `SendCartSummaryAsync`, `SendOrderConfirmationAsync`, `SendPaymentLinkAsync`
- `AddKeyedScoped<IChannelPort, WebChatChannelAdapter>(ChannelType.WebChat)` no Program.cs
- Adicionar canal novo = criar adaptador + registrar — zero mudança no core UCP
- Sessão: Redis TTL 30min por `connectionId` (web) ou número de telefone (WhatsApp)

### Fases

| Fase | Descrição | Canal |
|------|-----------|-------|
| **V_DEMO_001** | Shell Next.js 15 + Tailwind + shadcn/ui + Hub SignalR + `IChannelPort` | Web |
| **V_DEMO_002** | Fluxo UCP completo via chat: Search → Cart → Checkout → Payment → Order | Web |
| **V_DEMO_003** | UX polish + PWA + push notification de status de pedido | Web |
| **V_WA_001** | Conexão Meta Cloud API + intent detection + sessão Redis | WhatsApp |
| **V_WA_002** | Fluxo UCP completo + link de pagamento Stripe/Efi via WA | WhatsApp |
| **V_WA_003** | Botões interativos + notificações proativas + painel OpsWatch WA | WhatsApp |

### Decisões Técnicas

| Item | Decisão | Motivo |
|------|---------|--------|
| Protocol web | **SignalR** (não SSE) | Bidirecional — push de pagamento confirmado sem polling |
| WhatsApp dev | **Baileys** | Sem aprovação Meta; prod usa Meta Cloud API oficial |
| Deploy web | **Vercel** | Edge, grátis, zero config |
| Sessão | **Redis TTL 30min** | Já existe na infra; `connectionId` (web) ou número (WA) |
| Reutilização | **100% core UCP** | Mesmos Handlers MediatR, mesmos Ports — só o adaptador muda |

---

## 🔜 V042–V043 — Infraestrutura & Escala

| Item | Descrição | Impacto |
|------|-----------|---------|
| **K3s + Helm + Argo CD** | Migração Docker Compose → K3s na VPS (~7h); repo `comprai-infra` | Alto |
| **Demo Pública** | Next.js 15 + Tailwind + shadcn/ui — interface conversacional para portfólio | Alto |
| **Graphify** | Grafo arquitetura `.csproj` → Mermaid (README) + HTML interativo + JSON para IA | Médio |
| **StressForge / StatusForge** | Produtos irmãos .NET 10 + SQLite — gerador agnóstico de stress tests | Médio |

---

## 🔜 V043+ — Qualidade & Testes

| Item | Descrição |
|------|-----------|
| **Locust** | Segunda engine de carga; seletor K6\|Locust no OpsWatch; `ILoadTestEngine` Plugin Pattern |
| **GROQ_MODEL_SELECTOR** | Seletor de modelo no `workflow_dispatch` do code-review |
| **Botão Cancelar run** | Implementado mas nunca validado em produção — validar e corrigir se necessário |
| **Required metrics APM** | Convenções semânticas OTel para eliminar "Required metrics are missing" no New Relic APM |

---


---

## 🛍️ Demo Pública Web — Chat de Compras (WebSocket + SignalR)


### Arquitetura Hexagonal — Canal como Porta Plugável

> Mesmo padrão dos plugins de catálogo — canal (Web/WhatsApp/ZAP) é adaptador de entrada, core UCP imutável.

```
Domain / Application (core UCP — imutável)
         │
         ▼
   IChannelPort  ←── interface do canal de saída
         │
    ┌────┴────────────────┐
    │                     │
WebChatAdapter       WhatsAppAdapter
(SignalR Hub)        (Meta Webhook / Baileys)
```

**Porta de domínio:** `UcpAgent.Domain/Ports/IChannelPort.cs`
- `SendMessageAsync`, `SendProductListAsync`, `SendCartSummaryAsync`, `SendOrderConfirmationAsync`, `SendPaymentLinkAsync`
- `ChannelType` enum: `WebChat | WhatsApp | Zap`

**Adaptadores:** `UcpAgent.Api/Adapters/`
- `WebChatChannelAdapter` — `IHubContext<ChatHub>` → `SendAsync` ao grupo SignalR
- `WhatsAppChannelAdapter` — `IWhatsAppClient` → texto/lista/botão via Meta API
- `ZapChannelAdapter` — futuro, em `src/plugins/UcpAgent.Zap/`

**Handler:** `SendMessageHandler` resolve `IChannelPort` por `[FromKeyedServices(ChannelType)]`
— não sabe se está no Web ou WhatsApp, só chama a porta certa.

**Adicionar canal novo:** criar adaptador + registrar em `Program.cs` — zero mudança no core.


**Stack:** Next.js 15 + Tailwind + shadcn/ui + SignalR (WebSocket bidirecional)

> WebSocket via SignalR (não SSE) — bidirecional: servidor dá push de eventos (pagamento confirmado, status pedido) sem o cliente pedir. `IHubContext<T>` injetável em qualquer serviço, inclusive webhooks Stripe/Efi.

### Fase 1 — Shell & SignalR Hub (V_DEMO_001)
- SignalR Hub no Comprai API (`ChatHub`) — reconexão automática, fallback SSE/long-polling
- Next.js 15 com `@microsoft/signalr` — `useEffect` para conexão + handlers de eventos
- Layout: sidebar histórico + área central chat + painel carrinho flutuante
- Bubble usuário vs agente, typing indicator animado
- Sessão por `connectionId` SignalR (Redis TTL 30min)

### Fase 2 — Fluxo UCP Integrado (V_DEMO_002)
- `Search` → cards de produto (imagem, preço, botão "Adicionar") enviados via `hub.SendAsync`
- `Cart` → painel lateral atualizado em tempo real via push do servidor
- `Checkout` → formulário endereço inline step-by-step no chat
- `Payment` → Stripe/Pix — webhook confirma pagamento → `IHubContext` faz push imediato ao chat
- `Order` → confirmação com número + status em tempo real

### Fase 3 — UX & Polish (V_DEMO_003)
- Chips de sugestão rápida: "Ver carrinho", "Finalizar compra", "Rastrear pedido"
- Histórico de sessão (localStorage)
- Modo escuro/claro, PWA (manifest + service worker)
- Deploy: Vercel (edge) ou VPS Nginx

---

## 📱 WhatsApp Bot — Compras via Chat (V_WA_001 → V_WA_003)

**Stack:** Node.js + Meta Cloud API oficial (produção) ou Baileys (dev/test)
**Sessão:** Redis por número de telefone (TTL 30min) — já existe na infra

> Mesmo SignalR Hub do Web Chat — webhook WhatsApp → HTTP → Hub → UCP handlers. Reutiliza 100% do core.

### Fase 1 — Conexão & Intent (V_WA_001)
- Webhook recebe mensagem WhatsApp → `/api/intent` → resposta texto simples
- Sessão por número no Redis, estado da conversa persistido
- Fluxo básico: pesquisa em lista numerada, responde número para adicionar

### Fase 2 — Fluxo UCP Completo (V_WA_002)
- `Search` → lista numerada máx 5 produtos com emoji e preço
- `Cart` → resumo com *finalizar* para continuar
- `Checkout` → coleta endereço em etapas (CEP → confirma → confirma)
- `Payment` → link Stripe ou chave Pix gerada e enviada
- `Order` → confirmação com número e previsão de entrega

### Fase 3 — UX Avançado (V_WA_003)
- Botões interativos (List Messages + Reply Buttons) via Meta API oficial
- Notificações proativas: pedido saiu para entrega 🚚
- Handoff para atendente: `/falar com atendente`
- Painel OpsWatch: pedidos via WA por dia

### Decisões de Arquitetura

| Decisão | Escolha | Motivo |
|---------|---------|--------|
| Comunicação Web | **WebSocket (SignalR)** | Bidirecional — push de pagamento/status sem polling |
| WhatsApp API | **Meta Cloud API** (prod) / Baileys (dev) | Meta é oficial e suporta botões interativos |
| Auth Web | Sem auth (demo) → magic link futuro | Foco no fluxo UCP |
| Deploy Web | **Vercel** (grátis, edge) | CDN global, zero config Next.js |
| Sessão | **Redis** (já existe) | TTL 30min por connectionId/telefone |


---

## 📌 Itens Backlog (sem versão definida)

- MercadoPago: bloqueado por loop de SMS no token
- Efi (Pix BR): certificado `efi-homologacao.p12` na VPS — integração pendente de validação
- Stripe Webhooks: implementar `HandleWebhook` completo
- OpsWatch segregado em repo próprio
