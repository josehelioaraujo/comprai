# 🗺️ Roadmap — Comprai

## ✅ Concluído

### V039–V041 — Backend + Observabilidade
- [x] Fluxo UCP completo: Search → Cart → Checkout → Payment → Order
- [x] Plugins: DummyJSON, Mock, Shopify, MercadoLivre
- [x] Pagamento: Mock, Stripe (cartão teste), Efi (Pix — aguardando cert)
- [x] Redis cache + HybridCache .NET 10
- [x] Kafka: tópicos canônicos + events de domínio
- [x] OTel pipeline: comprai-api → collector → Datadog + New Relic
- [x] OpsWatch: dashboard com QA, observabilidade, pentest, K6, New Relic APM
- [x] Health checks condicionais por feature flag
- [x] SonarCloud: Quality Gate Passed, cobertura 97%+

### V042–V043 — Web Chat Frontend
- [x] Next.js 15 App Router + Tailwind + TypeScript
- [x] Fluxo UCP completo via Web Chat
- [x] ProductCarousel com setas de navegação
- [x] CartCard com +/- quantidade, frete radio, sincronização sidebar
- [x] CheckoutCard, PixCard, StripeCard, OrderTrackingCard
- [x] Dark/Light mode com tokens CSS
- [x] Sidebar colapsável + modal sobre UCP
- [x] Widget embeddável (botão flutuante)
- [x] Versão + SHA na sidebar com bump automático
- [x] Responsivo (mobile/tablet/desktop)
- [x] Painel de logs request/response

---

## 🔜 Próximas Versões

### V044 — Histórico de Pedidos
- [ ] Opção 1: Chat responde "meus pedidos" com lista via `/api/orders?sessionId`
- [ ] Opção 2: Tela `/orders` com timeline, filtros por status, detalhes do pagamento
- [ ] Opção 3: Ambos — chat com resumo + botão "Ver todos" abrindo tela dedicada

### V045 — UX e Sidebar
- [ ] Categorias de produtos na sidebar (Vestuário, Eletrônicos, Eletrodomésticos, etc.)
- [ ] Filtros de preço, marca, avaliação
- [ ] Histórico de buscas recentes
- [ ] Botão remover item individual no CartCard
- [ ] Animações de transição entre steps UCP

### V046 — Gateway de Pagamento Próprio
- [ ] Serviço dedicado com PostgreSQL (transações, idempotency, status)
- [ ] Filas de processamento: RabbitMQ ou Kafka
- [ ] Retry automático com DLQ
- [ ] Webhooks de confirmação (pending → processing → paid → failed)
- [ ] Pix real via EFI Bank (Gerencianet)
- [ ] Cartão via Stripe (produção)
- [ ] Admin dashboard de transações

### V047 — Rastreio de Pedidos
- [ ] Serviço de rastreio com integração transportadora
- [ ] Status em tempo real: pago → em separação → em trânsito → entregue
- [ ] OrderTrackingCard com timeline dinâmica
- [ ] Push via SignalR quando status muda
- [ ] Webhook do parceiro logístico

### V048 — WhatsApp Canal 2
- [ ] Meta Cloud API oficial (prod) / Baileys (dev)
- [ ] `IChannelPort` plugável — mesmo core UCP
- [ ] Sessão por número de telefone (Redis TTL 30min)
- [ ] Fluxo completo via WhatsApp: busca → carrinho → pix

### V049 — Resiliência e Observabilidade Front
- [ ] Outbox Pattern: PostgreSQL/Redis Streams — at-least-once delivery
- [ ] Polly retry com jitter backoff no Kafka publisher
- [ ] Dead Letter Queue (DLQ) no RabbitMQ
- [ ] OTel JS no front: traces, logs, métricas → OpsWatch + New Relic
- [ ] Notificações email via Resend (3.000/mês grátis)

### V050 — Infraestrutura
- [ ] Domínio + SSL via Certbot (atualmente HTTP no IP direto)
- [ ] Ollama na VPS: qwen2.5:3b (~2GB) para classificação de intent real
- [ ] K3s + Helm + Argo CD: migração Docker Compose → K3s
- [ ] runtime-config.json: URLs configuráveis sem rebuild do front
