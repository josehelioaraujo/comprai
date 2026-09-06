# Changelog

Todas as alterações relevantes do projeto **Comprai** são documentadas aqui.

Formato baseado em [Keep a Changelog](https://keepachangelog.com/pt-BR/1.0.0/).
Versionamento segue [Semantic Versioning](https://semver.org/lang/pt-BR/).

---

## [Unreleased]

### Planejado
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

## [0.5.0] — Fases 3-5: Plugins VTEX e Open Food Facts

### Adicionado
- **VtexCatalogPlugin**: busca via API pública `catalog_system/pub/products/search`, suporte a filtro de preço, mapeamento de SKUs e imagens
- **VtexSearchPlugin**: busca via VTEX Intelligent Search `/_v/api/intelligent-search/product_search`, suporte a categoria e paginação
- **OpenFoodFactsPlugin**: busca via `world.openfoodfacts.org`, retorna produtos alimentícios com imagem e categoria
- Configuração `VtexCatalog:AccountName` e `VtexSearch:AccountName` em `appsettings.json`
- Registro automático de todos os plugins com `HttpClient` isolado por plugin quando `UsarMockDados = false`

---

## [0.4.0] — Fase 2: Plugin Mercado Livre

### Adicionado
- **MercadoLivrePlugin**: busca via API pública MLB (`/sites/MLB/search`), suporte a filtro de categoria e faixa de preço, paginação por offset
- `MercadoLivreResponse` — DTOs internos para deserialização da API
- Registro via `AddHttpClient<MercadoLivrePlugin>()` + `IProductCatalogPort` quando `UsarMockDados = false`
- Referência ao projeto plugin no `UcpAgent.Api.csproj`
- Pacote `OpenTelemetry.Instrumentation.Http` adicionado para rastreamento de chamadas HTTP

---

## [0.2.0] — Fase 1: Domain + SharedKernel + API Bootstrap

### Adicionado
- **SharedKernel**: `Result<T>` / `Result`, ports (`IProductCatalogPort`, `ICartPort`, `ICheckoutPort`, `IOrderPort`, `IChannelPort`), models (`ProductDto`, `SearchRequest`, `SearchResult`, `CartItemDto`, `CustomerDto`, `CheckoutResultDto`, `OrderStatusDto`), `CatalogPluginAttribute`
- **Domain**: entidades `Product`, `Cart`, `CartItem`, `Order`, `OrderItem`, enum `OrderStatus` — lógica de negócio sem dependências externas
- **Application**: handlers MediatR para `SearchProductsQuery`, `AddToCartCommand`, `CheckoutCommand`, `GetOrderQuery`
- **Api**: Minimal API ASP.NET Core, endpoints `/api/search`, `/api/cart`, `/api/checkout`, `/api/orders`, health checks `/health/live` e `/health/ready`, OpenAPI + Scalar, HybridCache + Redis, OpenTelemetry
- **Mocks** (`UsarMockDados = true`): `MockCatalogPlugin` (10 produtos BR), `InMemoryCartPort` (thread-safe), `MockCheckoutPort` (retorna `MOCK-XXXXXXXX`), `MockOrderPort` (status aleatório)
- **Feature flag** `Features:UsarMockDados` — alterna entre adapters mock e reais sem alterar código
- **Plugin stubs**: MercadoLivre, VtexCatalog, VtexSearch, OpenFoodFacts (estrutura pronta para Fase 2+)
- **Testes**: `CartTests` — 5 testes unitários xunit cobrindo `Cart` (add, merge, remove, clear, validação)

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
[Unreleased]: https://github.com/josehelioaraujo/comprai/compare/v0.5.0...HEAD
[0.5.0]: https://github.com/josehelioaraujo/comprai/compare/v0.4.0...v0.5.0
[0.4.0]: https://github.com/josehelioaraujo/comprai/compare/v0.2.0...v0.4.0
[0.2.0]: https://github.com/josehelioaraujo/comprai/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/josehelioaraujo/comprai/releases/tag/v0.1.0
