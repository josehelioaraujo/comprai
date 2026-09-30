# 🗺️ Roadmap — Comprai

## ✅ Concluído

### Backend + Observabilidade (V039–V041)
- [x] Fluxo UCP completo: Search → Cart → Checkout → Payment → Order
- [x] Plugins de catálogo: DummyJSON, Mock, Shopify, MercadoLivre
- [x] Pagamento: Mock, Stripe (cartão teste), Efi/Pix (aguardando cert produção)
- [x] Redis cache + HybridCache .NET 10
- [x] Kafka: tópicos canônicos + events de domínio (SearchQueried, CartItemAdded, OrderCreated)
- [x] OTel pipeline: comprai-api → collector → Datadog + New Relic (93 spans confirmados)
- [x] OpsWatch: dashboard QA, observabilidade, pentest OWASP ZAP, K6, New Relic APM drill-down
- [x] Health checks condicionais por feature flag (/health/live, /health/ready)
- [x] SonarCloud: Quality Gate Passed, cobertura 97%+
- [x] CI/CD: unit-tests → integration-tests → sonar → deploy → smoke-tests

### 🌐 Web Chat Frontend (V042–V043)
- [x] Next.js 15 App Router + Tailwind + TypeScript
- [x] Fluxo UCP completo via chat: Search → Cart → Checkout → Pix/Cartão → Entrega
- [x] `ProductCarousel` — setas ‹ › de navegação, sem label de fonte
- [x] `CartCard` — +/- quantidade (mínimo 1), radio frete, subtotal/frete/total compacto
- [x] `CheckoutCard` — resumo do pedido + seleção Pix / Cartão
- [x] `PixCard` — QR Code + countdown 15min + cópia Pix (fallback `execCommand` em HTTP)
- [x] `StripeCard` — formulário inline cartão de crédito
- [x] `OrderTrackingCard` — timeline de status do pedido
- [x] `UcpProgressBar` — steps horizontais, badge 🛒, clique abre popup do carrinho
- [x] `Sidebar` — colapsável ‹ ›, logo clicável abre modal UCP, dark/light mode toggle
- [x] Modal UCP: badge "⚡ Universal Commerce Protocol" + steps + step atual destacado
- [x] Dark/Light mode com tokens CSS, persistido em localStorage
- [x] Versão `v1.0.x` + short SHA em linhas separadas na sidebar
- [x] Bump automático de patch version a cada deploy via GitHub Actions
- [x] Widget embeddável — botão flutuante 🛍️ com badge, abre chat em qualquer site
- [x] Popup carrinho na sidebar — ícone + badge quantidade + total + botão Fechar pedido
- [x] Botão "🛒 Fechar pedido" fixo no input quando há itens
- [x] +/- quantidade sincroniza sidebar em tempo real via `onQuantityChange`
- [x] Responsivo — sidebar oculta em mobile, header mobile com badge e toggle
- [x] Painel de logs 🔍 — request/response com erros em vermelho
- [x] Deploy: GitHub Actions → GHCR → VPS Hostinger porta 3002

---

## 🔜 Próximas Versões

### V044 — Histórico de Pedidos
- [ ] **Opção 1:** Chat responde "meus pedidos" listando pedidos da sessão via `/api/orders?sessionId`
- [ ] **Opção 2:** Tela `/orders` com timeline, filtros por status (pago/em trânsito/entregue) e detalhes do pagamento
- [ ] **Opção 3:** Ambos — chat com lista resumida + botão "Ver todos" abrindo tela dedicada

### V045 — UX Web Chat
- [ ] Categorias de produtos na sidebar: Vestuário, Eletrônicos, Eletrodomésticos, etc. (como e-commerces)
- [ ] Filtros de preço, marca e avaliação na sidebar
- [ ] Histórico de buscas recentes
- [ ] Botão remover item individual no CartCard
- [ ] Animações de transição entre steps UCP
- [ ] `paths` filter no `ci-cd.yml` para não disparar backend quando só há mudança em `apps/web/**`

### V046 — Gateway de Pagamento Próprio
- [ ] Serviço dedicado com PostgreSQL (transações, idempotency, auditoria)
- [ ] Filas de processamento: RabbitMQ + Kafka
- [ ] Retry automático + Dead Letter Queue (DLQ)
- [ ] Webhooks de confirmação: `pending → processing → paid → failed`
- [ ] Pix real via EFI Bank / Gerencianet
- [ ] Cartão de crédito via Stripe (produção)
- [ ] Admin dashboard de transações

### V447 — Rastreio de Pedidos
- [ ] Serviço de rastreio com integração de transportadora
- [ ] Status em tempo real: pago → em separação → em trânsito → entregue
- [ ] `OrderTrackingCard` com timeline dinâmica via push (SignalR)
- [ ] Webhook do parceiro logístico

### V448 — WhatsApp Canal 2
- [ ] Meta Cloud API oficial (prod) / Baileys (dev/test)
- [ ] `IChannelPort` plugável — mesmo core UCP, zero mudança no backend
- [ ] Sessão por número de telefone (Redis TTL 30min)
- [ ] Fluxo completo: busca → carrinho → pix via WhatsApp

### V449 — Resiliência + Observabilidade Front
- [ ] Outbox Pattern: at-least-once delivery para eventos Kafka
- [ ] Polly retry com jitter backoff no Kafka publisher
- [ ] Notificações email via Resend (3.000/mês grátis) — triggers: pedido criado/atualizado
- [ ] OTel JS no front: traces + logs + métricas → OpsWatch + New Relic
- [ ] runtime-config.json: URLs configuráveis sem rebuild do front

### V450 — Infraestrutura
- [ ] Domínio próprio + SSL via Certbot (atualmente HTTP no IP direto)
- [ ] Ollama na VPS: qwen2.5:3b (~2GB) para classificação de intent real (sem mock)
- [ ] K3s + Helm + Argo CD: migração Docker Compose → Kubernetes na VPS
- [ ] StressForge / StatusForge: produtos irmãos agnósticos de stack
