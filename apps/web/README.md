# 🛍️ Comprai — Web Chat

> **Você pede. A IA compra.**
>
> Assistente de compras com Inteligência Artificial que guia do pedido à entrega em linguagem natural, usando o fluxo **UCP (Universal Commerce Protocol)**.

---

## 🌐 Web Chat — Frontend

### Stack
- **Next.js 15** App Router + TypeScript
- **Tailwind CSS v3** com tokens CSS para dark/light mode
- Deploy via **GitHub Actions** → **GHCR** → **VPS Hostinger**

### Acesso
| Ambiente | URL |
|----------|-----|
| Web Chat | http://2.25.122.11:3002 |
| Widget demo | http://2.25.122.11:3002/widget |

### Fluxo UCP
```
🔍 Busca → 🛒 Carrinho → 📋 Pedido → 💳 Pagamento → 📦 Entrega
```

### Componentes principais
| Componente | Descrição |
|-----------|-----------|
| `ProductCarousel` | Cards com setas ‹ ›, sem label de fonte |
| `CartCard` | +/- quantidade, radio frete, subtotal/total |
| `CheckoutCard` | Resumo pedido + seleção Pix / Cartão |
| `PixCard` | QR Code + countdown + cópia Pix (fallback HTTP) |
| `StripeCard` | Formulário inline cartão de crédito |
| `OrderTrackingCard` | Timeline de status do pedido |
| `Sidebar` | Colapsável, logo clicável modal UCP, dark/light |
| `UcpProgressBar` | Steps horizontais, badge 🛒, clique abre carrinho |
| `CompraiWidget` | Botão flutuante embeddável em qualquer site |

### Embeddável
Para adicionar o chat em qualquer site:
```html
<script src="http://2.25.122.11:3002/widget.js"></script>
```

### Variáveis de build
| Variável | Descrição |
|---------|-----------|
| `NEXT_PUBLIC_API_URL` | URL da API backend |
| `NEXT_PUBLIC_APP_VERSION` | Versão bumped automaticamente |
| `NEXT_PUBLIC_GIT_SHA` | Short SHA do commit |

---

## 🔧 Backend API

- **Porta:** `5020`
- **OpenAPI:** http://2.25.122.11:5020/openapi/v1.json
- **Stack:** .NET 10, Minimal API, MediatR, Redis, Kafka

## 📊 Observabilidade

| Serviço | URL |
|--------|-----|
| Grafana | http://2.25.122.11:3001 |
| Jaeger | http://2.25.122.11:16687 |
| OpsWatch | http://2.25.122.11/k6/dashboard |
| Kafka UI | http://2.25.122.11:8083 |

## 🏗️ Arquitetura

```
Vertical Slice + Hexagonal + Plugin Pattern
Canal (Web/WhatsApp) → Intent → UCP Handler → Plugins de Catálogo
                                            → Gateway de Pagamento
                                            → Rastreio de Pedidos
```
