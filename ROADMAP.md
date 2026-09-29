# 🗺️ Roadmap — Comprai

> Última atualização: 2026-09-29 · Versão atual: v4.6.0 (V040)

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

## 📌 Itens Backlog (sem versão definida)

- MercadoPago: bloqueado por loop de SMS no token
- Efi (Pix BR): certificado `efi-homologacao.p12` na VPS — integração pendente de validação
- Stripe Webhooks: implementar `HandleWebhook` completo
- OpsWatch segregado em repo próprio
