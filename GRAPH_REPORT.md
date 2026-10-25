# Graph Report - comprai  (2026-10-25)

## Corpus Check
- cluster-only mode — file stats not available

## Summary
- 4348 nodes · 9087 edges · 245 communities (213 shown, 32 thin omitted)
- Extraction: 93% EXTRACTED · 7% INFERRED · 0% AMBIGUOUS · INFERRED: 678 edges (avg confidence: 0.83)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `8193ffc9`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- CancellationToken
- .CreateAsync
- Task
- FulfillmentAggregate
- ref_next
- Fact
- Product
- with
- AppShell
- with
- UcpAgent.Infrastructure.Persistence.Repositories
- Guid
- UcpAgent.SharedKernel.Ports
- with
- with
- with
- with
- mobile/src/hooks/useChat.ts
- otel-collector-config.yaml
- .ProcessAsync
- mobile-v2/src/types/index.ts
- EmailNotificationWorker
- UcpAgent.Api/Program.cs
- with
- Order
- environment
- system_diagnostics_codeanalysis
- steps
- chat/page.tsx
- web/src/hooks/useChat.ts
- ref_react
- steps
- env
- xunit
- EfiPaymentAdapterTests.cs
- Dictionary
- system_text_json
- dashboard/page.tsx
- CompraApiFactory
- scrape_configs
- IntentResult
- env
- with
- parse-zap-report.py
- steps
- PriceWatcherService
- environment
- with
- Cart
- UcpMetrics
- ref_next_auth
- DateTime
- IConfiguration
- env
- with
- steps
- SearchResult
- AuthEndpoints
- JsonElement
- CartItemDto
- Result
- HttpClient
- MlTokenService
- comprai.sln
- mobile/src/app/page.tsx
- target
- steps
- .Create
- CheckoutCommand
- ShopifyPlugin.cs
- mobile-v2/src/lib/api.ts
- IntentRenderer.tsx
- web/src/types/ucp.ts
- EfiPaymentAdapter
- environment
- IntentRouterService
- steps
- populate
- test_type
- comprai-mobile-v2
- checkout/page.tsx
- comprai-web
- PaymentMethodDto
- UcpAgent.Api
- OrderListCard.tsx
- compilerOptions
- compilerOptions
- compilerOptions
- .MapHealthStatusEndpoint
- ResilienceOptions
- compilerOptions
- broker
- with
- linear-populate.ts
- .Handle
- RateLimitOptions
- List
- environment
- mobile/src/types/ucp.ts
- .BuildClient
- SearchProductsQuery
- ProductDto
- comprai-api
- comprai-admin
- rabbitmq
- MobileIntentRenderer.tsx
- web/package.json
- jobs
- with
- with
- EfiPayOptions
- mobile/package.json
- mobile-v2/package.json
- Product
- web/src/components/ucp/CartCard.tsx
- UcpAgent.McpServer
- ICartPort
- RabbitMqNotificationPublisher
- environment
- mobile/src/components/ucp/CartCard.tsx
- mobile/src/components/ucp/PixCard.tsx
- (user)/account/page.tsx
- web/src/components/ucp/PixCard.tsx
- web/src/lib/api.ts
- env
- HealthCheckExtensionsTests
- DummyProduct
- comprai-admin
- comprai-postgres
- comprai-postgres
- admin/package.json
- stress.js
- SearchProductsHandlerTests.cs
- services
- comprai-kafka-ui
- services
- devDependencies
- typescript
- steps
- jobs
- env
- load.js
- smoke.js
- soak.js
- spike.js
- OrderTests
- CustomerRow
- TestPriorityAttribute.cs
- comprai-otel-collector
- env
- api_url
- env
- net10.0
- UcpAgent.Application.Tests
- ShopifyIntegrationTest
- @playwright/test
- AddressRow
- e2e/package.json
- comprai-grafana
- comprai-rabbitmq
- kafka-ui
- grafana
- devDependencies
- devDependencies
- app/login/page.tsx
- deploy
- UcpAgent.Infrastructure
- .SearchAsync
- purchase-flow.spec.ts
- comprai-api
- healthcheck
- comprai-loki
- loki
- devDependencies
- mobile/src/app/auth/login/LoginPage.tsx
- web/src/app/auth/login/LoginPage.tsx
- scrape_configs
- HealthCheckExtensions.cs
- comprai-jaeger
- comprai-kafka
- comprai-prometheus
- comprai-promtail
- jaeger
- prometheus
- promtail
- volumes
- mobile/src/lib/auth.ts
- app/proxy/[...path]/route.ts
- web/src/lib/auth.ts
- steps
- steps
- IdempotencyMiddleware
- AesGcmEncryptionService
- options
- comprai-redis
- comprai-redis-commander
- e2e-tests
- e2e-v2
- api_url
- fixtures.ts
- CompraiDbInitializer.cs
- dashboard/layout.tsx
- mobile-v2/src/types/next-auth.d.ts
- smoke-tests
- publicar-e2e
- load
- soak
- spike
- stress
- summary
- CacheStrategy
- Result
- MlSearchResponse
- depends_on
- scripts
- scripts
- web/src/app/layout.tsx
- sonar
- publicar-pages
- UcpAgent.PriceWatcher
- OpenFoodFactsResponse.cs
- tailwindcss
- @types/node
- UcpAgent.Catalog.MercadoLivreOrders
- UcpAgent.Catalog.Shopify
- .TryRecordAsync
- full-flow-v2.spec.ts
- tailwind.config.ts
- IChannelPort
- UcpAgent.Api.Infrastructure
- postcss.config.mjs
- setup-web-vps.sh
- run-e2e-vps.sh
- { GET, POST }
- { GET, POST }
- { GET, POST }

## God Nodes (most connected - your core abstractions)
1. `UcpAgent.SharedKernel.Ports` - 100 edges
2. `Result` - 49 edges
3. `FulfillmentAggregate` - 43 edges
4. `UcpAgent.SharedKernel` - 38 edges
5. `environment` - 36 edges
6. `UcpAgent.Api` - 36 edges
7. `AuthCustomerDto` - 34 edges
8. `env` - 34 edges
9. `with` - 34 edges
10. `UcpAgent.SharedKernel.Models` - 32 edges

## Surprising Connections (you probably didn't know these)
- `typescript@^5` --declares--> `typescript`  [EXTRACTED]
  frontend/admin/package.json → package.json
- `typescript@^5` --declares--> `typescript`  [EXTRACTED]
  frontend/mobile/package.json → package.json
- `CartHandlerTests` --references--> `IEventPublisher`  [EXTRACTED]
  tests/UcpAgent.Application.Tests/Cart/CartHandlerTests.cs → src/UcpAgent.SharedKernel/Ports/IEventPublisher.cs
- `SearchProductsHandlerTests` --references--> `UcpMetrics`  [EXTRACTED]
  tests/UcpAgent.Application.Tests/Search/SearchProductsHandlerTests.cs → src/UcpAgent.SharedKernel/UcpMetrics.cs
- `CartHandlerTests` --references--> `UcpMetrics`  [EXTRACTED]
  tests/UcpAgent.Application.Tests/Cart/CartHandlerTests.cs → src/UcpAgent.SharedKernel/UcpMetrics.cs

## Import Cycles
- None detected.

## Communities (245 total, 32 thin omitted)

### Community 0 - "CancellationToken"
Cohesion: 0.08
Nodes (10): TotpService, CustomerAuthRepository, NullAuthPort, AuthCustomerDto, CustomerAddressDto, IAuthPort, RegisterRequest, SaveAddressRequest (+2 more)

### Community 1 - ".CreateAsync"
Cohesion: 0.07
Nodes (26): OrdersEndpoints, FulfillmentEvent, PostgresFulfillmentRepository, DbResiliencePolicy, IDbConnectionFactory, NpgsqlConnectionFactory, AdminCustomersStatsDto, AdminFulfillmentStatsDto (+18 more)

### Community 2 - "Task"
Cohesion: 0.08
Nodes (10): MlTokenResponse, PriceAlertMessage, CartSnapshotService, IOrderService, NullCartSnapshotPort, NullCartSnapshotService, CartSnapshotDto, ICartSnapshotPort (+2 more)

### Community 3 - "FulfillmentAggregate"
Cohesion: 0.07
Nodes (27): FulfillmentService, IFulfillmentService, FulfillmentAggregate, CarrierCode, CreatedAt, CurrentStatus, Events, OrderId (+19 more)

### Community 4 - "ref_next"
Cohesion: 0.04
Nodes (18): nextConfig, DELETE, dynamic, GET, PATCH, POST, PUT, metadata (+10 more)

### Community 5 - "Fact"
Cohesion: 0.09
Nodes (3): HealthStatusEndpointTests, IntentRouterServiceTests, ResultTests

### Community 6 - "Product"
Cohesion: 0.08
Nodes (20): CartItem, ItemId, Product, Quantity, Subtotal, Product, AvailableQuantity, Category (+12 more)

### Community 7 - "with"
Cohesion: 0.04
Nodes (47): default, description, required, env, DOTNET_NOLOGO, DOTNET_SKIP_FIRST_TIME_EXPERIENCE, GITHUB_RUN_NUMBER, GITHUB_SHA (+39 more)

### Community 8 - "AppShell"
Cohesion: 0.08
Nodes (36): ProfilePage(), dynamic, RegisterPage(), fmt(), OrderDetailPage(), Props, OrdersPage(), fmt() (+28 more)

### Community 9 - "with"
Cohesion: 0.04
Nodes (45): env, API_HOST, API_PORT, ref, target_url, jobs, pentest, name (+37 more)

### Community 10 - "UcpAgent.Infrastructure.Persistence.Repositories"
Cohesion: 0.09
Nodes (8): UcpAgent.Infrastructure.Payment, UcpAgent.Infrastructure.Persistence.Repositories, UcpAgent.Infrastructure.Persistence, UcpAgent.Application.Fulfillment, UcpAgent.Infrastructure.Persistence.Workers, UcpAgent.Infrastructure.Fulfillment, UcpAgent.Infrastructure.Orders, UcpAgent.Domain.Fulfillment

### Community 11 - "Guid"
Cohesion: 0.09
Nodes (13): UcpCacheService, Strategy, NullSessionPort, OutboxMessage, OutboxRepository, NotificationOutboxWorker, OrderOutboxWorker, RawOutboxMessage (+5 more)

### Community 12 - "UcpAgent.SharedKernel.Ports"
Cohesion: 0.12
Nodes (9): UcpAgent.Application.Payment, UcpAgent.Application.Checkout, UcpAgent.Application.Tests.Checkout, UcpAgent.Application.Cart, UcpAgent.SharedKernel, UcpAgent.Application.Order, UcpAgent.SharedKernel.Ports, UcpAgent.Application.Orders (+1 more)

### Community 13 - "with"
Cohesion: 0.05
Nodes (43): concurrency, cancel-in-progress, group, name, permissions, runs-on, steps, env (+35 more)

### Community 14 - "with"
Cohesion: 0.05
Nodes (43): concurrency, cancel-in-progress, group, name, permissions, runs-on, steps, env (+35 more)

### Community 15 - "with"
Cohesion: 0.05
Nodes (43): concurrency, cancel-in-progress, group, name, permissions, runs-on, steps, env (+35 more)

### Community 16 - "with"
Cohesion: 0.05
Nodes (43): concurrency, cancel-in-progress, group, name, permissions, runs-on, steps, env (+35 more)

### Community 17 - "mobile/src/hooks/useChat.ts"
Cohesion: 0.11
Nodes (34): calcShipping(), FULFILLMENT_PIPELINE, getOrdersFromLocalStorage(), INITIAL_SESSION, makeId(), saveOrderToLocalStorage(), useChat(), WELCOME (+26 more)

### Community 18 - "otel-collector-config.yaml"
Cohesion: 0.05
Nodes (41): key, site, action, key, value, send_batch_size, timeout, api (+33 more)

### Community 19 - ".ProcessAsync"
Cohesion: 0.08
Nodes (14): RedisCheckoutAdapter, FulfillmentSimulator, NullNotificationPublisher, RedisOrderAdapter, Db, Ttl, PersistingPaymentAdapter, OrderHistoryRepository (+6 more)

### Community 20 - "mobile-v2/src/types/index.ts"
Cohesion: 0.13
Nodes (32): RFC-4122, CartPage(), handleDecrease(), handleIncrease(), handleRemove(), refresh(), HomePage(), handleAddToCart() (+24 more)

### Community 21 - "EmailNotificationWorker"
Cohesion: 0.11
Nodes (7): BrevoEmailService, IEmailService, NullEmailService, ResendEmailService, EmailNotificationWorker, OrderConfirmationNotification, OrderStatusNotification

### Community 22 - "UcpAgent.Api/Program.cs"
Cohesion: 0.07
Nodes (12): UcpAgent.Infrastructure.Security, UcpAgent.Catalog.DummyJSON, UcpAgent.Infrastructure.Checkout, UcpAgent.Api.Middleware, UcpAgent.Api, UcpAgent.Api.Workers, UcpAgent.Api.Cache, UcpAgent.Api.Auth (+4 more)

### Community 23 - "with"
Cohesion: 0.05
Nodes (37): default, description, options, required, type, env, DRY_RUN, POSTGRES_PASSWORD (+29 more)

### Community 24 - "Order"
Cohesion: 0.06
Nodes (33): Order, CreatedAt, CustomerEmail, CustomerId, CustomerName, Id, Items, SessionId (+25 more)

### Community 25 - "environment"
Cohesion: 0.06
Nodes (36): environment, Admin__RestartPassword, App__AdminUrl, App__WebUrl, ASPNETCORE_ENVIRONMENT, ASPNETCORE_URLS, Brevo__ApiKey, Brevo__From (+28 more)

### Community 26 - "system_diagnostics_codeanalysis"
Cohesion: 0.08
Nodes (7): UcpAgent.SharedKernel.Events, UcpAgent.Api.RateLimit, UcpAgent.Api.Endpoints, UcpAgent.Application.Tests.RateLimit, AdminEndpoints, NotificationQueues, UcpTopics

### Community 27 - "steps"
Cohesion: 0.06
Nodes (34): env, DOTNET_VERSION, RUN_NUMBER, RUN_SHA, VPS_HOST, VPS_SSH_KEY, VPS_USER, jobs (+26 more)

### Community 28 - "chat/page.tsx"
Cohesion: 0.11
Nodes (25): ChatPage(), WidgetPage(), ChatInput(), handleKey(), handleSend(), Props, SUGGESTIONS, ChatWindow() (+17 more)

### Community 29 - "web/src/hooks/useChat.ts"
Cohesion: 0.14
Nodes (28): cartItemToProduct(), ProfilePage(), handleReorder(), STATUS_COLOR, STATUS_LABEL, calcShipping(), INITIAL_SESSION, loadOrdersList() (+20 more)

### Community 30 - "ref_react"
Cohesion: 0.13
Nodes (21): AccountPage(), ForgotPasswordPage(), dynamic, LoginPage(), ResetForm(), ResetPasswordPage(), ProfileCard(), Props (+13 more)

### Community 31 - "steps"
Cohesion: 0.06
Nodes (32): continue-on-error, name, runs-on, steps, env, DOTNET_VERSION, RUN_NUMBER, RUN_SHA (+24 more)

### Community 32 - "env"
Cohesion: 0.06
Nodes (31): env, DOTNET_VERSION, Features__UsarKafka, Features__UsarMockDados, Features__UsarRabbitMQ, Features__UsarRedis, MercadoLivre__AccessToken, ML_ACCESS_TOKEN (+23 more)

### Community 33 - "xunit"
Cohesion: 0.15
Nodes (8): UcpAgent.Application.Tests.Fulfillment, UcpAgent.Domain.Tests, UcpAgent.Application.Tests.Cart, UcpAgent.Application.Tests.SharedKernel, UcpAgent.Domain.Entities, UcpAgent.Application.Tests.Orders, UcpAgent.Application.Tests.IntentRouter, UcpAgent.Domain.Enums

### Community 34 - "EfiPaymentAdapterTests.cs"
Cohesion: 0.08
Nodes (6): UcpAgent.Application.Tests.Payment, UcpAgent.Api.Adapters, UcpAgent.Api.Resilience, UcpAgent.Application.Tests.Resilience, UcpAgent.Catalog.MercadoLivreOrders, EfiPaymentExtensions

### Community 35 - "Dictionary"
Cohesion: 0.09
Nodes (13): UcpAgent.FakeCatalog, UcpAgent.FakeCatalog.Exporters, ICatalogExporter, Format, ShopifyCsvExporter, Format, UcpJsonExporter, Format (+5 more)

### Community 36 - "system_text_json"
Cohesion: 0.12
Nodes (7): UcpAgent.SharedKernel.Attributes, UcpAgent.Infrastructure.Cart, UcpAgent.Integration.Tests, UcpAgent.Api.Mocks, UcpAgent.SharedKernel.Models, MockCheckoutPort, McpServerIntegrationTest

### Community 37 - "dashboard/page.tsx"
Cohesion: 0.12
Nodes (24): DashboardPage(), KpiCard(), OrderModal(), handleCopy(), StatusBadge(), MeResponse, SettingsPage(), handleSubmit() (+16 more)

### Community 38 - "CompraApiFactory"
Cohesion: 0.10
Nodes (7): HealthApiFactory, CompraApiFactory, IntentRouterIntegrationTest, MlOrdersIntegrationTest, RateLimiterIntegrationTest, RateLimitTestFactory, UcpFlowIntegrationTest

### Community 39 - "scrape_configs"
Cohesion: 0.06
Nodes (30): clients, url, filters, host, refresh_interval, output, stream, name (+22 more)

### Community 40 - "IntentResult"
Cohesion: 0.10
Nodes (15): UcpAgent.Api.Models, UcpAgent.Application.IntentRouter, IntentEndpoints, IntentRequest, IntentResponse, IIntentRouterService, IntentResult, IntentType (+7 more)

### Community 41 - "env"
Cohesion: 0.07
Nodes (30): ADMIN_RESTART_PASSWORD, BREVO_SMTP_KEY, DATADOG_API_KEY, Features__UsarKafka, Features__UsarMockDados, Features__UsarPostgres, Features__UsarRabbitMQ, Features__UsarRedis (+22 more)

### Community 42 - "with"
Cohesion: 0.07
Nodes (29): name, permissions, runs-on, steps, env, DOTNET_VERSION, jobs, analyze (+21 more)

### Community 43 - "parse-zap-report.py"
Cohesion: 0.10
Nodes (6): format_k6_results(), get_metric(), generate_dashboard_html(), get_status(), parse(), strip_html()

### Community 44 - "steps"
Cohesion: 0.07
Nodes (28): LINEAR_API_KEY, status, version, jobs, sync, name, default, description (+20 more)

### Community 45 - "PriceWatcherService"
Cohesion: 0.11
Nodes (11): UcpAgent.PriceWatcher, UcpAgent.PriceWatcher.Channels, UcpAgent.PriceWatcher.Models, UcpAgent.PriceWatcher.Hubs, NullAlertChannel, PriceHub, IPriceAlertChannel, PriceWatch (+3 more)

### Community 46 - "environment"
Cohesion: 0.08
Nodes (28): environment, environment, App__AdminUrl, App__WebUrl, ASPNETCORE_ENVIRONMENT, ASPNETCORE_URLS, Brevo__ApiKey, Brevo__From (+20 more)

### Community 47 - "with"
Cohesion: 0.07
Nodes (28): with, cache-from, cache-to, command_timeout, context, distribution, dotnet-version, envs (+20 more)

### Community 48 - "Cart"
Cohesion: 0.16
Nodes (10): Cart, CreatedAt, IsEmpty, ItemCount, Items, SessionId, Total, UpdatedAt (+2 more)

### Community 49 - "UcpMetrics"
Cohesion: 0.07
Nodes (20): KafkaEventPublisher, UcpMetrics, CacheHitTotal, CacheMissTotal, CartAddTotal, CheckoutFailureTotal, CheckoutSuccessTotal, CheckoutTotal (+12 more)

### Community 50 - "ref_next_auth"
Cohesion: 0.08
Nodes (11): metadata, metadata, RootLayout(), viewport, Providers(), TotpSetupPage(), TotpVerifyContent(), TotpVerifyPage() (+3 more)

### Community 51 - "DateTime"
Cohesion: 0.10
Nodes (13): FulfillmentEventRow, CartSnapshotRow, OrderSummary, FulfillmentEventSummary, OrderHistorySummary, FulfillmentEventRow, SearchLogRepository, CartItemAddedEvent (+5 more)

### Community 52 - "IConfiguration"
Cohesion: 0.20
Nodes (5): HealthCheckExtensions, OllamaHealthCheck, RedisHealthCheck, OtelExtensions, RateLimitExtensions

### Community 53 - "env"
Cohesion: 0.08
Nodes (25): GH_TOKEN, GROQ_API_KEY, PR_BRANCH, PR_NUMBER, PR_TITLE, REPO, jobs, review (+17 more)

### Community 54 - "with"
Cohesion: 0.08
Nodes (25): name, runs-on, steps, jobs, generate-graph, name, permissions, contents (+17 more)

### Community 55 - "steps"
Cohesion: 0.08
Nodes (25): if, name, runs-on, steps, default, description, required, LINEAR_API_KEY (+17 more)

### Community 56 - "SearchResult"
Cohesion: 0.17
Nodes (14): MercadoLivrePlugin, SourceName, OpenFoodFactsPlugin, SourceName, VtexCatalogPlugin, SourceName, VtexSearchPlugin, SourceName (+6 more)

### Community 57 - "AuthEndpoints"
Cohesion: 0.10
Nodes (12): JwtService, AdminRegisterRequest, AuthEndpoints, AuthResponse, ForgotPasswordRequest, LoginRequest, RefreshRequest, ResetPasswordRequest (+4 more)

### Community 58 - "JsonElement"
Cohesion: 0.16
Nodes (6): UcpAgent.McpServer.Tools, CartTool, CheckoutTool, IntentTool, OrderTool, SearchProductsTool

### Community 59 - "CartItemDto"
Cohesion: 0.22
Nodes (8): InMemoryCartPort, GetCartQuery, GetCartQueryHandler, RedisCartAdapter, Db, CartItemDto, GetCartQueryHandlerTests, ProductDto

### Community 60 - "Result"
Cohesion: 0.22
Nodes (9): GetOrderHandler, GetOrderQuery, OrderService, OrderStatusDto, Result, Error, IsSuccess, GetOrderHandlerTests (+1 more)

### Community 62 - "MlTokenService"
Cohesion: 0.14
Nodes (15): MlBuyer, MlItem, MlMeResponse, MlOrderDto, MlOrderItem, MlOrdersResponse, MlOrdersService, MlPaging (+7 more)

### Community 63 - "comprai.sln"
Cohesion: 0.14
Nodes (19): System.Text.Json (9.0.5), UcpAgent.Catalog.DummyJSON, Microsoft.Extensions.Http (10.0.0), UcpAgent.Catalog.MercadoLivre, UcpAgent.Catalog.OpenFoodFacts, UcpAgent.Catalog.VtexCatalog, Microsoft.Extensions.Configuration.Abstractions (10.0.0), UcpAgent.Catalog.VtexSearch (+11 more)

### Community 64 - "mobile/src/app/page.tsx"
Cohesion: 0.13
Nodes (15): MobilePage(), MobileChatFooter(), handleKey(), handleSend(), Props, SUGGESTIONS_BY_STEP, MobileChatShell(), Props (+7 more)

### Community 65 - "target"
Cohesion: 0.09
Nodes (22): name, runs-on, steps, env, TARGET, VPS_HOST, VPS_SSH_KEY, VPS_USER (+14 more)

### Community 66 - "steps"
Cohesion: 0.09
Nodes (22): env, VPS_HOST, VPS_SSH_KEY, VPS_USER, jobs, metrics, name, runs-on (+14 more)

### Community 68 - "CheckoutCommand"
Cohesion: 0.26
Nodes (9): CheckoutCommand, CheckoutHandler, CheckoutService, ICheckoutService, CheckoutResultDto, CustomerDto, ICheckoutPort, CheckoutHandlerTests (+1 more)

### Community 69 - "ShopifyPlugin.cs"
Cohesion: 0.16
Nodes (18): UcpAgent.Catalog.Shopify, FeaturedImage, MoneyV2, PageInfo, PriceRangeV2, ProductConnection, ProductEdge, ProductNode (+10 more)

### Community 70 - "mobile-v2/src/lib/api.ts"
Cohesion: 0.15
Nodes (15): dynamic, VerifyPage(), VerifyEmailForm(), handleSend(), handleVerify(), ApiError, apiLogin(), apiMe() (+7 more)

### Community 71 - "IntentRenderer.tsx"
Cohesion: 0.22
Nodes (15): Props, IntentRenderer(), Props, CustomerDto, CheckoutCard(), fmt(), Props, formatPrice() (+7 more)

### Community 72 - "web/src/types/ucp.ts"
Cohesion: 0.13
Nodes (20): BtnClose(), fmt(), fmtDatetime(), FULFILLMENT_FLOW, inferFulfillmentStatus(), OrderTrackingCard(), Props, STATUS_INDEX (+12 more)

### Community 73 - "EfiPaymentAdapter"
Cohesion: 0.22
Nodes (6): EfiPaymentAdapter, EfiPaymentAdapterTests, RoutingHandler, FailTwiceHandler, CallCount, PollyResilienceIntegrationTest

### Community 74 - "environment"
Cohesion: 0.10
Nodes (21): CLUSTER_ID, KAFKA_ADVERTISED_LISTENERS, KAFKA_AUTO_CREATE_TOPICS_ENABLE, KAFKA_CONTROLLER_LISTENER_NAMES, KAFKA_CONTROLLER_QUORUM_VOTERS, KAFKA_LISTENER_SECURITY_PROTOCOL_MAP, KAFKA_LISTENERS, KAFKA_LOG_DIRS (+13 more)

### Community 76 - "steps"
Cohesion: 0.10
Nodes (20): name, runs-on, steps, LINEAR_API_KEY, version, jobs, cleanup, name (+12 more)

### Community 77 - "populate"
Cohesion: 0.10
Nodes (20): rounds, target_url, jobs, populate, name, name, runs-on, steps (+12 more)

### Community 78 - "test_type"
Cohesion: 0.10
Nodes (21): default, description, required, duration, target_url, test_type, vus, default (+13 more)

### Community 79 - "comprai-mobile-v2"
Cohesion: 0.10
Nodes (20): environment, container_name, depends_on, environment, image, networks, ports, profiles (+12 more)

### Community 80 - "checkout/page.tsx"
Cohesion: 0.16
Nodes (15): EMPTY_CART, CheckoutPage(), handlePlaceOrder(), dynamic, fmt(), PaymentMethod, CartItemRow(), fmt() (+7 more)

### Community 81 - "comprai-web"
Cohesion: 0.10
Nodes (19): NEXT_PUBLIC_API_URL, args, context, dockerfile, external, build, container_name, env_file (+11 more)

### Community 82 - "PaymentMethodDto"
Cohesion: 0.26
Nodes (8): StripePaymentAdapter, MockPaymentAdapter, ProcessPaymentCommand, ProcessPaymentHandler, IPaymentPort, PaymentMethodDto, PaymentResultDto, ProcessPaymentHandlerTests

### Community 83 - "UcpAgent.Api"
Cohesion: 0.10
Nodes (20): Microsoft.AspNetCore.Authentication.JwtBearer (10.0.0), Microsoft.AspNetCore.OpenApi (10.0.0), Microsoft.Extensions.Caching.StackExchangeRedis (10.0.0), Microsoft.OpenApi (2.11.0), OpenTelemetry.Exporter.Prometheus.AspNetCore (1.16.0-rc.2), OpenTelemetry.Instrumentation.Runtime (1.11.1), Otp.NET (1.4.0), Resend (0.22.0) (+12 more)

### Community 84 - "OrderListCard.tsx"
Cohesion: 0.18
Nodes (17): fmt(), fmtDate(), getDisplayStatus(), OrderListCard(), Props, STATUS_COLOR, STATUS_LABEL, fmt() (+9 more)

### Community 85 - "compilerOptions"
Cohesion: 0.11
Nodes (18): compilerOptions, allowJs, esModuleInterop, incremental, isolatedModules, jsx, lib, module (+10 more)

### Community 86 - "compilerOptions"
Cohesion: 0.11
Nodes (18): compilerOptions, allowJs, esModuleInterop, incremental, isolatedModules, jsx, lib, module (+10 more)

### Community 87 - "compilerOptions"
Cohesion: 0.11
Nodes (18): compilerOptions, allowJs, esModuleInterop, incremental, isolatedModules, jsx, lib, module (+10 more)

### Community 88 - ".MapHealthStatusEndpoint"
Cohesion: 0.22
Nodes (8): HealthStatusEndpoint, ComponentStatus, Incident, IncidentUpdate, StatusPageInfo, StatusPageResponse, StatusSummary, StatusPageResponseTests

### Community 89 - "ResilienceOptions"
Cohesion: 0.12
Nodes (15): ResilienceExtensions, CircuitBreakerOptions, BreakDurationSeconds, FailureRatio, MinimumThroughput, SamplingDurationSeconds, ResilienceOptions, CircuitBreaker (+7 more)

### Community 90 - "compilerOptions"
Cohesion: 0.11
Nodes (17): compilerOptions, allowJs, esModuleInterop, incremental, isolatedModules, jsx, lib, module (+9 more)

### Community 91 - "broker"
Cohesion: 0.11
Nodes (18): default, description, options, required, type, broker, usar_banco, branches (+10 more)

### Community 92 - "with"
Cohesion: 0.29
Nodes (18): steps, steps, steps, steps, steps, continue-on-error, id, if (+10 more)

### Community 93 - "linear-populate.ts"
Cohesion: 0.18
Nodes (15): backfill(), cleanupVersion(), client, deleteDuplicates(), findStateId(), getOrCreateCycle(), getOrCreateLabel(), getOrCreateProject() (+7 more)

### Community 94 - ".Handle"
Cohesion: 0.37
Nodes (5): MockOrderPort, GetOrderBySessionQuery, GetOrderBySessionQueryHandler, IOrderPort, GetOrderBySessionQueryHandlerTests

### Community 95 - "RateLimitOptions"
Cohesion: 0.12
Nodes (11): AuthWindowOptions, PermitLimit, WindowSeconds, FixedWindowOptions, PermitLimit, QueueLimit, WindowSeconds, RateLimitOptions (+3 more)

### Community 96 - "List"
Cohesion: 0.21
Nodes (14): UcpAgent.Catalog.VtexCatalog, UcpAgent.Catalog.VtexSearch, VtexImage, VtexItem, VtexOffer, VtexProduct, VtexSeller, VtexSearchImage (+6 more)

### Community 97 - "environment"
Cohesion: 0.12
Nodes (17): container_name, environment, image, networks, profiles, restart, volumes, DD_APM_ENABLED (+9 more)

### Community 98 - "mobile/src/types/ucp.ts"
Cohesion: 0.14
Nodes (14): CheckoutCard(), fmt(), Props, Address, CartItem, CheckoutData, IntentData, IntentResponse (+6 more)

### Community 99 - ".BuildClient"
Cohesion: 0.26
Nodes (4): CountingHandler, TokenCallCount, ResilienceTests, StubHandler

### Community 100 - "SearchProductsQuery"
Cohesion: 0.40
Nodes (4): SearchProductsHandler, SearchProductsQuery, IEventPublisher, SearchProductsHandlerTests

### Community 101 - "ProductDto"
Cohesion: 0.34
Nodes (6): AddToCartCommand, AddToCartHandler, CartService, ICartService, ProductDto, CartHandlerTests

### Community 102 - "comprai-api"
Cohesion: 0.13
Nodes (16): context, dockerfile, build, container_name, depends_on, networks, ports, restart (+8 more)

### Community 103 - "comprai-admin"
Cohesion: 0.15
Nodes (16): container_name, depends_on, image, logging, networks, ports, restart, logging (+8 more)

### Community 104 - "rabbitmq"
Cohesion: 0.15
Nodes (16): healthcheck, RABBITMQ_DEFAULT_PASS, RABBITMQ_DEFAULT_USER, interval, retries, test, timeout, container_name (+8 more)

### Community 105 - "MobileIntentRenderer.tsx"
Cohesion: 0.30
Nodes (13): formatTime(), isError(), MobileChatBubble(), Props, renderText(), MobileChatWindow, Props, MobileIntentRenderer() (+5 more)

### Community 106 - "web/package.json"
Cohesion: 0.14
Nodes (15): dependencies, next@^15.5.27, next-auth@^5.0.0-beta.25, react@^19.0.0, react-dom@^19.0.0, next, react, name (+7 more)

### Community 107 - "jobs"
Cohesion: 0.12
Nodes (16): name, runs-on, name, needs, runs-on, name, needs, runs-on (+8 more)

### Community 108 - "with"
Cohesion: 0.12
Nodes (16): with, cache, cache-dependency-path, host, if-no-files-found, key, name, node-version (+8 more)

### Community 109 - "with"
Cohesion: 0.12
Nodes (16): with, cache, cache-dependency-path, host, key, name, node-version, overwrite (+8 more)

### Community 110 - "EfiPayOptions"
Cohesion: 0.16
Nodes (11): EfiPayOptions, BaseUrl, CertificateBase64, CertificatePassword, CertificatePath, ChavePix, ClientId, ClientSecret (+3 more)

### Community 111 - "mobile/package.json"
Cohesion: 0.15
Nodes (14): dependencies, next@^15.5.27, react@^19.0.0, react-dom@^19.0.0, next, react, name, private (+6 more)

### Community 112 - "mobile-v2/package.json"
Cohesion: 0.17
Nodes (14): next-auth@^5.0.0-beta.25, dependencies, next@^15.5.27, next-auth@^5.0.0-beta.25, next-themes@^0.4.4, react@^19.0.0, react-dom@^19.0.0, next (+6 more)

### Community 113 - "Product"
Cohesion: 0.21
Nodes (9): formatPrice(), MobileProductCard(), Props, sourceLabel(), MobileProductCarousel(), onMouseUp(), updateArrows(), Props (+1 more)

### Community 114 - "web/src/components/ucp/CartCard.tsx"
Cohesion: 0.20
Nodes (11): BtnClose(), calcShipping(), CartCard(), EMPTY, fCEP(), fCPF(), fmt(), fPhone() (+3 more)

### Community 115 - "UcpAgent.McpServer"
Cohesion: 0.13
Nodes (15): Microsoft.Extensions.Hosting (10.0.0), ModelContextProtocol.AspNetCore (0.3.0-preview.2), Xunit.SkippableFact (1.4.13), Microsoft.NET.Sdk.Web, UcpAgent.McpServer, Microsoft.Extensions.Http (10.0.0), OpenTelemetry.Exporter.OpenTelemetryProtocol (1.11.2), OpenTelemetry.Extensions.Hosting (1.11.2) (+7 more)

### Community 116 - "ICartPort"
Cohesion: 0.37
Nodes (4): RemoveFromCartCommand, RemoveFromCartCommandHandler, ICartPort, RemoveFromCartCommandHandlerTests

### Community 117 - "RabbitMqNotificationPublisher"
Cohesion: 0.23
Nodes (3): RabbitMqAlertChannel, RabbitMqEventPublisher, RabbitMqNotificationPublisher

### Community 118 - "environment"
Cohesion: 0.14
Nodes (14): environment, CLUSTER_ID, KAFKA_ADVERTISED_LISTENERS, KAFKA_AUTO_CREATE_TOPICS_ENABLE, KAFKA_CONTROLLER_LISTENER_NAMES, KAFKA_CONTROLLER_QUORUM_VOTERS, KAFKA_LISTENER_SECURITY_PROTOCOL_MAP, KAFKA_LISTENERS (+6 more)

### Community 119 - "mobile/src/components/ucp/CartCard.tsx"
Cohesion: 0.21
Nodes (10): calcShipping(), CartCard(), EMPTY, fCEP(), fCPF(), fmt(), fPhone(), Props (+2 more)

### Community 120 - "mobile/src/components/ucp/PixCard.tsx"
Cohesion: 0.20
Nodes (8): formatPrice(), pad(), PixCard(), Props, formatPrice(), Props, StripeCard(), PaymentResult

### Community 121 - "(user)/account/page.tsx"
Cohesion: 0.20
Nodes (9): AccountPage(), Address, btnStyle, CustomerFull, Input(), UserLayout(), CustomerAddress, CustomerProfile (+1 more)

### Community 122 - "web/src/components/ucp/PixCard.tsx"
Cohesion: 0.20
Nodes (8): formatPrice(), pad(), PixCard(), Props, formatPrice(), Props, StripeCard(), PaymentResult

### Community 123 - "web/src/lib/api.ts"
Cohesion: 0.23
Nodes (11): ApiError, createCheckout(), createPayment(), get(), getCart(), getOrder(), messageForIntent(), post() (+3 more)

### Community 124 - "env"
Cohesion: 0.16
Nodes (14): env, CI, E2E_API_URL, E2E_BASE_URL, E2E_USER_EMAIL, E2E_USER_PASSWORD, GH_RUN, GH_RUN_ID (+6 more)

### Community 126 - "DummyProduct"
Cohesion: 0.14
Nodes (13): DummyJsonPlugin, SourceName, DummyProduct, Category, DiscountPercentage, Id, Price, Stock (+5 more)

### Community 127 - "comprai-admin"
Cohesion: 0.15
Nodes (13): container_name, depends_on, environment, image, networks, ports, restart, condition (+5 more)

### Community 128 - "comprai-postgres"
Cohesion: 0.15
Nodes (13): container_name, environment, image, networks, ports, restart, volumes, PGTZ (+5 more)

### Community 129 - "comprai-postgres"
Cohesion: 0.15
Nodes (13): container_name, environment, image, networks, ports, restart, volumes, PGTZ (+5 more)

### Community 130 - "admin/package.json"
Cohesion: 0.21
Nodes (12): dependencies, next@^15.5.27, react@19.3.0, react-dom@19.3.0, recharts@^2.15.0, next, react, name (+4 more)

### Community 131 - "stress.js"
Cohesion: 0.17
Nodes (8): buildStructured(), errorRate, handleSummary(), options, peakVus, rateLimited, responseTime, ROUTE_DEFS

### Community 132 - "SearchProductsHandlerTests.cs"
Cohesion: 0.18
Nodes (4): UcpAgent.Infrastructure.Messaging, UcpAgent.Application.Tests.Search, NullEventPublisher, KafkaEventPublisher_Placeholder

### Community 133 - "services"
Cohesion: 0.17
Nodes (11): driver, networks, comprai-network, services, volumes, grafana_data, kafka_data, loki_data (+3 more)

### Community 134 - "comprai-kafka-ui"
Cohesion: 0.17
Nodes (12): condition, container_name, depends_on, environment, image, networks, ports, profiles (+4 more)

### Community 135 - "services"
Cohesion: 0.17
Nodes (11): driver, networks, comprai-network, container_name, image, networks, ports, restart (+3 more)

### Community 136 - "devDependencies"
Cohesion: 0.17
Nodes (12): autoprefixer@^10, @types/react-dom@^19, autoprefixer@^10, @types/react-dom@^19, devDependencies, autoprefixer@^10, eslint@^9, postcss@^8 (+4 more)

### Community 137 - "typescript"
Cohesion: 0.21
Nodes (11): typescript@^5, typescript@^5, devDependencies, @linear/sdk@7.0.0, tsx@4.19.1, typescript@5.5.4, typescript, name (+3 more)

### Community 138 - "steps"
Cohesion: 0.36
Nodes (12): steps, steps, steps, steps, steps, continue-on-error, id, if (+4 more)

### Community 139 - "jobs"
Cohesion: 0.17
Nodes (11): concurrency, cancel-in-progress, group, jobs, smoke, name, run-name, if (+3 more)

### Community 140 - "env"
Cohesion: 0.17
Nodes (12): DURATION_INPUT, GH_PAT, GIT_SHA, PEAK_VUS, RESULTS_DIR, RUN_DATE, RUN_NUMBER, TARGET_URL (+4 more)

### Community 141 - "load.js"
Cohesion: 0.18
Nodes (8): buildStructured(), errorRate, handleSummary(), options, peakVus, rateLimited, responseTime, ROUTE_DEFS

### Community 142 - "smoke.js"
Cohesion: 0.18
Nodes (7): buildStructured(), errorRate, handleSummary(), options, rateLimited, responseTime, ROUTE_DEFS

### Community 143 - "soak.js"
Cohesion: 0.18
Nodes (8): buildStructured(), errorRate, handleSummary(), options, peakVus, rateLimited, responseTime, ROUTE_DEFS

### Community 144 - "spike.js"
Cohesion: 0.18
Nodes (8): buildStructured(), errorRate, handleSummary(), options, peakVus, rateLimited, responseTime, ROUTE_DEFS

### Community 146 - "CustomerRow"
Cohesion: 0.17
Nodes (12): CustomerRow, AvatarUrl, Document, Email, EmailVerified, Id, Name, PasswordHash (+4 more)

### Community 147 - "TestPriorityAttribute.cs"
Cohesion: 0.18
Nodes (5): CatalogPluginAttribute, Name, PriorityOrderer, TestPriorityAttribute, Priority

### Community 148 - "comprai-otel-collector"
Cohesion: 0.18
Nodes (11): command, container_name, environment, image, networks, profiles, restart, volumes (+3 more)

### Community 149 - "env"
Cohesion: 0.18
Nodes (10): env, API_PORT, BUILD_VERSION, COVERAGE_THRESHOLD, DOTNET_VERSION, MCP_PORT, VPS_HOST, VPS_PATH (+2 more)

### Community 150 - "api_url"
Cohesion: 0.18
Nodes (11): default, description, required, default, description, required, api_url, base_url (+3 more)

### Community 151 - "env"
Cohesion: 0.20
Nodes (11): env, CI, E2E_API_URL, E2E_BASE_URL, E2E_USER_EMAIL, E2E_USER_NAME, E2E_USER_PASSWORD, NODE_VERSION (+3 more)

### Community 152 - "net10.0"
Cohesion: 0.22
Nodes (11): FluentValidation (11.*), net10.0, UcpAgent.Application, MediatR (12.*), UcpAgent.Domain, UcpAgent.Domain.Tests, coverlet.collector (6.*), FluentAssertions (6.*) (+3 more)

### Community 153 - "UcpAgent.Application.Tests"
Cohesion: 0.18
Nodes (11): Moq (4.*), NSubstitute (5.*), UcpAgent.Application.Tests, Bogus (35.6.5), coverlet.collector (6.*), FluentAssertions (6.*), Microsoft.AspNetCore.Mvc.Testing (10.*), Microsoft.Extensions.Http.Resilience (9.*) (+3 more)

### Community 156 - "AddressRow"
Cohesion: 0.18
Nodes (11): AddressRow, City, Complement, Id, IsDefault, Label, Neighborhood, Number (+3 more)

### Community 157 - "e2e/package.json"
Cohesion: 0.18
Nodes (10): devDependencies, @playwright/test@^1.48.0, name, private, scripts, report, test, test:debug (+2 more)

### Community 158 - "comprai-grafana"
Cohesion: 0.20
Nodes (10): container_name, depends_on, environment, image, networks, ports, profiles, restart (+2 more)

### Community 159 - "comprai-rabbitmq"
Cohesion: 0.20
Nodes (10): container_name, environment, image, networks, ports, profiles, volumes, RABBITMQ_DEFAULT_PASS (+2 more)

### Community 160 - "kafka-ui"
Cohesion: 0.20
Nodes (10): KAFKA_CLUSTERS_0_BOOTSTRAPSERVERS, KAFKA_CLUSTERS_0_NAME, container_name, depends_on, environment, image, networks, ports (+2 more)

### Community 161 - "grafana"
Cohesion: 0.20
Nodes (10): container_name, depends_on, environment, image, networks, ports, profiles, restart (+2 more)

### Community 162 - "devDependencies"
Cohesion: 0.20
Nodes (10): devDependencies, eslint-config-next@^15, postcss@^8, tailwindcss@^3.4.17, @types/node@^22, typescript@^5, postcss, eslint-config-next@^15.5.27 (+2 more)

### Community 163 - "devDependencies"
Cohesion: 0.20
Nodes (10): @types/react@^19, devDependencies, eslint-config-next@^15.5.27, postcss@^8, @types/react@^19, typescript@^5, postcss, @types/react@^19 (+2 more)

### Community 164 - "app/login/page.tsx"
Cohesion: 0.33
Nodes (6): Backdrop(), EyeIcon(), ForgotPasswordModal(), LoginPage(), Modal, RegisterModal()

### Community 165 - "deploy"
Cohesion: 0.22
Nodes (10): if, name, needs, permissions, runs-on, steps, deploy, permissions (+2 more)

### Community 166 - "UcpAgent.Infrastructure"
Cohesion: 0.20
Nodes (10): BCrypt.Net-Next (4.0.3), Confluent.Kafka (2.6.3), Dapper (2.1.35), dbup-postgresql (5.0.23), Npgsql (9.0.3), UcpAgent.Infrastructure, Microsoft.Extensions.Caching.Hybrid (9.4.0), Microsoft.Extensions.Hosting.Abstractions (10.*) (+2 more)

### Community 167 - ".SearchAsync"
Cohesion: 0.44
Nodes (3): ISearchService, SearchService, SearchServiceTests

### Community 169 - "comprai-api"
Cohesion: 0.22
Nodes (9): container_name, extra_hosts, group_add, image, networks, ports, restart, volumes (+1 more)

### Community 170 - "healthcheck"
Cohesion: 0.47
Nodes (9): healthcheck, healthcheck, healthcheck, healthcheck, interval, retries, start_period, test (+1 more)

### Community 171 - "comprai-loki"
Cohesion: 0.22
Nodes (9): command, container_name, image, networks, ports, profiles, restart, volumes (+1 more)

### Community 172 - "loki"
Cohesion: 0.22
Nodes (9): command, container_name, image, networks, ports, profiles, restart, volumes (+1 more)

### Community 173 - "devDependencies"
Cohesion: 0.22
Nodes (9): eslint@^9, eslint@^9, devDependencies, autoprefixer@^10, eslint@^9, postcss@^8, @types/react-dom@^19, eslint (+1 more)

### Community 174 - "mobile/src/app/auth/login/LoginPage.tsx"
Cohesion: 0.31
Nodes (5): Field(), inputStyle, LoginPage(), LoginPageProps, Page()

### Community 175 - "web/src/app/auth/login/LoginPage.tsx"
Cohesion: 0.31
Nodes (5): Field(), inputStyle, LoginPage(), LoginPageProps, Page()

### Community 176 - "scrape_configs"
Cohesion: 0.22
Nodes (8): global, evaluation_interval, scrape_interval, scrape_configs, job_name, metrics_path, static_configs, targets

### Community 178 - "comprai-jaeger"
Cohesion: 0.25
Nodes (8): container_name, environment, image, networks, ports, profiles, restart, comprai-jaeger

### Community 179 - "comprai-kafka"
Cohesion: 0.25
Nodes (8): container_name, hostname, image, networks, ports, profiles, volumes, comprai-kafka

### Community 180 - "comprai-prometheus"
Cohesion: 0.25
Nodes (8): container_name, image, networks, ports, profiles, restart, volumes, comprai-prometheus

### Community 181 - "comprai-promtail"
Cohesion: 0.25
Nodes (8): command, container_name, image, networks, profiles, restart, volumes, comprai-promtail

### Community 182 - "jaeger"
Cohesion: 0.25
Nodes (8): container_name, environment, image, networks, ports, profiles, restart, jaeger

### Community 183 - "prometheus"
Cohesion: 0.25
Nodes (8): container_name, image, networks, ports, profiles, restart, volumes, prometheus

### Community 184 - "promtail"
Cohesion: 0.25
Nodes (8): command, container_name, image, networks, profiles, restart, volumes, promtail

### Community 185 - "volumes"
Cohesion: 0.25
Nodes (8): volumes, grafana_data, kafka_data, loki_data, postgres_data, prometheus_data, rabbitmq_data, redis_data

### Community 186 - "mobile/src/lib/auth.ts"
Cohesion: 0.32
Nodes (5): auth, handlers, signIn, signOut, config

### Community 187 - "app/proxy/[...path]/route.ts"
Cohesion: 0.25
Nodes (6): Context, DELETE, GET, PATCH, POST, PUT

### Community 188 - "web/src/lib/auth.ts"
Cohesion: 0.32
Nodes (5): auth, handlers, signIn, signOut, config

### Community 189 - "steps"
Cohesion: 0.39
Nodes (8): steps, steps, continue-on-error, if, name, run, uses, working-directory

### Community 190 - "steps"
Cohesion: 0.25
Nodes (8): steps, continue-on-error, id, if, name, run, uses, working-directory

### Community 191 - "IdempotencyMiddleware"
Cohesion: 0.29
Nodes (3): IdempotencyMiddleware, IdempotencyRecord, SecurityHeadersMiddleware

### Community 193 - "options"
Cohesion: 0.33
Nodes (7): logging, logging, driver, options, max-file, max-size, tag

### Community 194 - "comprai-redis"
Cohesion: 0.29
Nodes (7): command, container_name, image, networks, ports, restart, comprai-redis

### Community 195 - "comprai-redis-commander"
Cohesion: 0.29
Nodes (7): container_name, environment, image, networks, ports, profiles, comprai-redis-commander

### Community 196 - "e2e-tests"
Cohesion: 0.29
Nodes (6): name, runs-on, timeout-minutes, jobs, e2e-tests, name

### Community 197 - "e2e-v2"
Cohesion: 0.29
Nodes (6): name, runs-on, timeout-minutes, jobs, e2e-v2, name

### Community 198 - "api_url"
Cohesion: 0.29
Nodes (7): default, description, required, api_url, True, workflow_dispatch, inputs

### Community 201 - "dashboard/layout.tsx"
Cohesion: 0.33
Nodes (3): DashboardLayout(), navItems, pageTitles

### Community 202 - "mobile-v2/src/types/next-auth.d.ts"
Cohesion: 0.33
Nodes (5): @auth/core/types, JWT, next-auth/jwt, Session, User

### Community 203 - "smoke-tests"
Cohesion: 0.33
Nodes (6): smoke-tests, continue-on-error, name, needs, runs-on, timeout-minutes

### Community 204 - "publicar-e2e"
Cohesion: 0.33
Nodes (6): publicar-e2e, if, name, needs, runs-on, timeout-minutes

### Community 205 - "load"
Cohesion: 0.33
Nodes (6): load, if, name, needs, runs-on, timeout-minutes

### Community 206 - "soak"
Cohesion: 0.33
Nodes (6): soak, if, name, needs, runs-on, timeout-minutes

### Community 207 - "spike"
Cohesion: 0.33
Nodes (6): spike, if, name, needs, runs-on, timeout-minutes

### Community 208 - "stress"
Cohesion: 0.33
Nodes (6): stress, if, name, needs, runs-on, timeout-minutes

### Community 209 - "summary"
Cohesion: 0.33
Nodes (6): summary, if, name, needs, runs-on, timeout-minutes

### Community 210 - "CacheStrategy"
Cohesion: 0.33
Nodes (5): CacheStrategy, Aside, Hybrid, ReadThrough, Ttl

### Community 211 - "Result"
Cohesion: 0.33
Nodes (4): Result, Error, IsSuccess, Value

### Community 212 - "MlSearchResponse"
Cohesion: 0.60
Nodes (4): UcpAgent.Catalog.MercadoLivre, MlItem, MlPaging, MlSearchResponse

### Community 213 - "depends_on"
Cohesion: 0.40
Nodes (5): depends_on, condition, condition, comprai-postgres, comprai-redis

### Community 214 - "scripts"
Cohesion: 0.40
Nodes (5): scripts, build, dev, lint, start

### Community 215 - "scripts"
Cohesion: 0.40
Nodes (5): scripts, build, dev, lint, start

### Community 216 - "web/src/app/layout.tsx"
Cohesion: 0.50
Nodes (3): metadata, RootLayout(), ThemeProvider()

### Community 217 - "sonar"
Cohesion: 0.40
Nodes (5): sonar, name, needs, runs-on, timeout-minutes

### Community 218 - "publicar-pages"
Cohesion: 0.40
Nodes (5): publicar-pages, if, name, needs, runs-on

### Community 219 - "UcpAgent.PriceWatcher"
Cohesion: 0.40
Nodes (5): Microsoft.AspNetCore.SignalR.Core (1.2.0), Newtonsoft.Json (13.0.3), UcpAgent.PriceWatcher, Microsoft.Extensions.Hosting.Abstractions (10.0.0), RabbitMQ.Client (7.1.2)

### Community 220 - "OpenFoodFactsResponse.cs"
Cohesion: 0.67
Nodes (3): UcpAgent.Catalog.OpenFoodFacts, OffProduct, OffSearchResponse

### Community 222 - "tailwindcss"
Cohesion: 0.50
Nodes (4): tailwindcss@^3.4.17, tailwindcss@^3.4.17, tailwindcss@^3.4.17, tailwindcss

### Community 223 - "@types/node"
Cohesion: 0.50
Nodes (4): @types/node@^22, @types/node@^22, @types/node@^22, @types/node

### Community 224 - "UcpAgent.Catalog.MercadoLivreOrders"
Cohesion: 0.50
Nodes (4): Microsoft.Extensions.Caching.Memory (10.0.0), UcpAgent.Catalog.MercadoLivreOrders, Microsoft.Extensions.Configuration.Abstractions (10.0.0), Microsoft.Extensions.Http (10.0.0)

### Community 225 - "UcpAgent.Catalog.Shopify"
Cohesion: 0.50
Nodes (4): Microsoft.Extensions.Logging.Abstractions (10.0.0), Microsoft.Extensions.Options (10.0.0), UcpAgent.Catalog.Shopify, Microsoft.Extensions.Http (10.0.0)

## Knowledge Gaps
- **267 isolated node(s):** `Strategy`, `Strategy`, `BaseUrl`, `CertificateBase64`, `CertificatePassword` (+262 more)
  These have ≤1 connection - possible missing edges. (Counts symbols only; 1856 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **32 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `UcpAgent.SharedKernel.Ports` connect `UcpAgent.SharedKernel.Ports` to `CancellationToken`, `Task`, `SearchProductsHandlerTests.cs`, `UcpAgent.Infrastructure.Persistence.Repositories`, `Guid`, `.ProcessAsync`, `UcpAgent.Api/Program.cs`, `system_diagnostics_codeanalysis`, `xunit`, `EfiPaymentAdapterTests.cs`, `system_text_json`, `PriceWatcherService`, `DateTime`, `Result`, `CheckoutCommand`, `ShopifyPlugin.cs`, `PaymentMethodDto`, `.Handle`, `SearchProductsQuery`, `IChannelPort`, `ICartPort`?**
  _High betweenness centrality (0.015) - this node is a cross-community bridge._
- **What connects `Strategy`, `Strategy`, `BaseUrl` to the rest of the system?**
  _267 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `CancellationToken` be split into smaller, more focused modules?**
  _Cohesion score 0.07798855766335441 - nodes in this community are weakly interconnected._
- **Why does `CustomerRow` connect `CustomerRow` to `CancellationToken`, `Guid`?**
  _High betweenness centrality (0.009) - this node is a cross-community bridge._
- **Should `.CreateAsync` be split into smaller, more focused modules?**
  _Cohesion score 0.07298245614035087 - nodes in this community are weakly interconnected._
- **Why does `Order` connect `Order` to `List`, `xunit`, `.CreateAsync`, `Guid`, `OrderTests`, `DateTime`, `.ProcessAsync`?**
  _High betweenness centrality (0.008) - this node is a cross-community bridge._
- **Should `Task` be split into smaller, more focused modules?**
  _Cohesion score 0.0771478667445938 - nodes in this community are weakly interconnected._