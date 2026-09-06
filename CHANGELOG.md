# Changelog

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
