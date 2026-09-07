# Changelog

## [1.6.0] - 2026-09-06

### Added
- **Fase 19 - Plugin MercadoLivre Orders (OAuth + Webhook)**
  - UcpAgent.Catalog.MercadoLivreOrders  novo plugin de pedidos ML
  - MlTokenService  OAuth flow completo: authorization code, refresh token automatico, cache em memoria
  - MlOrdersService  lista pedidos do vendedor, busca pedido por ID via API ML
  - MlWebhookService  processa notificacoes de status de pedidos do ML
  - Endpoint GET /api/ml/auth  redireciona para autorizacao ML
  - Endpoint GET /callback  recebe code OAuth e salva token
  - Endpoint GET /api/ml/orders  lista pedidos do vendedor autenticado
  - Endpoint GET /api/ml/orders/{id}  busca pedido especifico
  - Endpoint POST /webhook/ml  recebe e processa notificacoes ML
  - App ML criada: ucp-compra.mercadolivre (Client ID: 2786639248653015)
  - ngrok instalado na VPS para tunnel HTTPS em POC


## [1.5.0] - 2026-09-06

### Added
- **Fase 18 - CI/CD (GitHub Actions + Deploy automatico na VPS)**
  - Workflow ci-cd.yml com 3 jobs: Build e Testes, Deploy VPS, Smoke Tests
  - Deploy automatico via SSH (appleboy/ssh-action@v1.2.0) para root@2.25.122.11
  - Rebuild condicional: so reconstroi imagens quando Dockerfile/csproj/Program.cs mudam
  - Feature flags via workflow_dispatch: broker, usar_mock, usar_redis, force_rebuild
  - Smoke test de busca condicional - roda apenas quando usar_mock=false
  - paths-ignore para .md, .txt, docs/ - push de docs nao dispara pipeline
  - Secret VPS_HOSTINGER_SSH_KEY configurado no repositorio


## [1.4.0] - 2026-09-06

### Added
- **Fase 17 - Observabilidade (OpenTelemetry + Jaeger + Grafana + Loki)**
  - Pacotes OTel adicionados em UcpAgent.Api e UcpAgent.McpServer
  - OtelExtensions.cs com AddObservabilidade() - traces OTLP exportados para Jaeger
  - appsettings.json com secao Otel (Endpoint + ServiceName)
  - observability/prometheus/prometheus.yml - scrape comprai-api e comprai-mcp
  - observability/loki/loki.yml - armazenamento de logs
  - observability/promtail/promtail.yml - coleta logs JSON dos containers via docker.sock
  - observability/grafana/datasources/datasources.yml - Prometheus + Loki + Jaeger
  - observability/grafana/dashboards/dashboards.yml - provider pasta Comprai
  - docker-compose.yml atualizado com profile monitoring (Prometheus, Grafana, Jaeger, Loki, Promtail)
  - .env com feature flags

### Endpoints VPS
  - Jaeger    : http://2.25.122.11:16687
  - Grafana   : http://2.25.122.11:3001
  - Prometheus: http://2.25.122.11:9091


Todas as alterações relevantes do projeto **Comprai** são documentadas aqui.

Formato baseado em [Keep a Changelog](https://keepachangelog.com/pt-BR/1.0.0/).
Versionamento segue [Semantic Versioning](https://semver.org/lang/pt-BR/).

---

## [Unreleased]

### Planejado
- Fase 1: Domain + SharedKernel
- Fase 2: Plugin Mercado Livre
- Fase 3: Plugin VTEX Catalog
- Fase 4: Plugin VTEX Intelligent Search
- Fase 5: Plugin Open Food Facts
- Fase 6: Feature Search
- Fase 7: Feature Cart
- Fase 8: Feature Checkout
- Fase 9: Feature Order
- Fase 10: Mensageria Kafka
- Fase 11: Mensageria RabbitMQ
- Fase 12: Intent Router (linguagem natural)
- Fase 13: MCP Server
- Fase 14: Canal Web (Next.js 15)
- Fase 15: Canal Teams
- Fase 16: Canal WhatsApp
- Fase 17: Observabilidade (OpenTelemetry + Jaeger + Grafana + Loki)
- Fase 18: K3s + CI/CD
- Fase 19: Plugin VTEX Orders (autenticado)
- Fase 20: Segundo plugin catálogo (WooCommerce / Shopify)

---

## [0.1.0] — Infraestrutura Base

### Adicionado
- Estrutura de solução `comprai.sln` com projetos separados por responsabilidade
- `docker-compose.yml` com perfis: `monitoring`, `kafka`, `rabbitmq`, `tools`
- `.github/workflows/ci-cd.yml` com pipeline: test → sonar → deploy SSH → newman
- Redis como cache principal (HybridCache .NET 10)
- Integração com SonarCloud para análise de qualidade
- Smoke tests E2E via Newman com relatório publicado no GitHub Pages
- Deploy automatizado via SSH na VPS Hostinger

### Portas configuradas
| Serviço         | Porta  |
|----------------|--------|
| comprai-api     | 5020   |
| Redis           | 6379   |
| RabbitMQ        | 5673 / 15673 |
| Kafka           | 9094 / 9095  |
| Kafka UI        | 8083   |
| Prometheus      | 9091   |
| Grafana         | 3001   |
| Jaeger UI       | 16687  |
| Loki            | 3101   |
| Redis Commander | 8084   |

---

<!-- Links de comparação -->
[Unreleased]: https://github.com/josehelioaraujo/comprai/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/josehelioaraujo/comprai/releases/tag/v0.1.0
