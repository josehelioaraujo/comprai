# Changelog

Todas as alterações relevantes do projeto **Comprai** são documentadas aqui.

Formato baseado em [Keep a Changelog](https://keepachangelog.com/pt-BR/1.0.0/).
Versionamento segue [Semantic Versioning](https://semver.org/lang/pt-BR/).

---

## [Unreleased]

### Planejado
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

## [0.9.0] — Fases 8-9: Checkout e Order com Redis

### Adicionado
- **RedisCheckoutAdapter**: valida carrinho não vazio, gera `ORDER-XXXXXXXX`, persiste pedido em Redis (TTL 30 dias), limpa carrinho após checkout
- **RedisOrderAdapter**: persiste e recupera `OrderStatusDto` em Redis (`order:{orderId}`), TTL 30 dias, método `SaveAsync` interno
- **UcpAgent.Infrastructure**: novo projeto com `Cart/`, `Checkout/` e `Orders/` — camada de adapters reais sem dependência do Domain
- `OrderStatusDto` atualizado: campos `Total`, `Customer`, `ItemsJson` adicionados
- `CheckoutResultDto` atualizado: `bool Success` + `string? Error` no lugar de `Status`/`CheckoutUrl`
- Endpoints adicionais: `GET /api/cart/{sessionId}` e `DELETE /api/cart/{sessionId}/items/{itemId}`
- Registro condicional: Redis habilitado (`Features:UsarRedis=true`) usa adapters reais; senão usa InMemory/Mock

---

## [0.7.0] — Fase 7: Feature Cart com Redis

### Adicionado
- **RedisCartAdapter**: carrinho persistido em Redis, TTL 72h renovado a cada operação, merge automático de itens duplicados (mesmo `Id+Source`), thread-safe via operações atômicas Redis
- `IConnectionMultiplexer` registrado via `ConnectionMultiplexer.Connect()` no DI
- Registro condicional: `UsarRedis=true` usa `RedisCartAdapter`; senão mantém `InMemoryCartPort`

---

## [0.6.0] — Fase 6: Feature Search agregada

### Adicionado
- `SearchProductsHandler` refatorado: fan-out paralelo com tratamento de falhas individuais por plugin, deduplicação por `Source:Id`, ranking (disponíveis primeiro → menor preço)
- Cache `HybridCache` no endpoint `/api/search`: TTL 5 min (distribuído) + 1 min (local L1), chave baseada em todos os parâmetros da busca
- `SearchProductsQuery` retorna `Result<SearchResult>` único agregado (antes retornava lista por source)

---

## [0.5.0] — Fases 3-5: Plugins VTEX e Open Food Facts

### Adicionado
- **VtexCatalogPlugin**: busca via `catalog_system/pub/products/search`, filtro de preço, mapeamento de SKUs e imagens
- **VtexSearchPlugin**: busca via VTEX Intelligent Search `/_v/api/intelligent-search/product_search`, suporte a categoria e paginação
- **OpenFoodFactsPlugin**: busca via `world.openfoodfacts.org`, produtos alimentícios com imagem e categoria
- Configuração `VtexCatalog:AccountName` e `VtexSearch:AccountName` em `appsettings.json`
- Registro automático de todos os plugins com `HttpClient` isolado por plugin

---

## [0.4.0] — Fase 2: Plugin Mercado Livre

### Adicionado
- **MercadoLivrePlugin**: busca via API pública MLB (`/sites/MLB/search`), filtro de categoria e faixa de preço, paginação por offset
- `MercadoLivreResponse` — DTOs internos para deserialização
- Pacote `OpenTelemetry.Instrumentation.Http` para rastreamento de chamadas HTTP

---

## [0.2.0] — Fase 1: Domain + SharedKernel + API Bootstrap

### Adicionado
- **SharedKernel**: `Result<T>`, ports, models, `CatalogPluginAttribute`
- **Domain**: entidades `Product`, `Cart`, `CartItem`, `Order`, `OrderItem`, enum `OrderStatus`
- **Application**: handlers MediatR para search, cart, checkout, order
- **Api**: Minimal API, endpoints principais, HybridCache+Redis, OpenTelemetry, OpenAPI+Scalar
- **Mocks**: `MockCatalogPlugin`, `InMemoryCartPort`, `MockCheckoutPort`, `MockOrderPort`
- **Feature flag** `Features:UsarMockDados`
- **Testes**: `CartTests` — 5 testes unitários xunit

---

## [0.1.0] — Infraestrutura Base

### Adicionado
- Estrutura de solução `comprai.sln` com projetos separados por responsabilidade
- `docker-compose.yml` com perfis: `monitoring`, `kafka`, `rabbitmq`, `tools`
- `.github/workflows/ci-cd.yml`: test → sonar → deploy SSH → newman
- Redis como cache principal (HybridCache .NET 10)
- Integração SonarCloud, smoke tests Newman, deploy SSH Hostinger

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
[Unreleased]: https://github.com/josehelioaraujo/comprai/compare/v0.9.0...HEAD
[0.9.0]: https://github.com/josehelioaraujo/comprai/compare/v0.7.0...v0.9.0
[0.7.0]: https://github.com/josehelioaraujo/comprai/compare/v0.6.0...v0.7.0
[0.6.0]: https://github.com/josehelioaraujo/comprai/compare/v0.5.0...v0.6.0
[0.5.0]: https://github.com/josehelioaraujo/comprai/compare/v0.4.0...v0.5.0
[0.4.0]: https://github.com/josehelioaraujo/comprai/compare/v0.2.0...v0.4.0
[0.2.0]: https://github.com/josehelioaraujo/comprai/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/josehelioaraujo/comprai/releases/tag/v0.1.0
