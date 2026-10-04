// ─── UCP Flow Types ───────────────────────────────────────────────────────────

export type UcpStep = 'idle' | 'search' | 'cart' | 'checkout' | 'payment' | 'order'

export type UcpIntent =
  | 'greeting'
  | 'search'
  | 'cart_add'
  | 'cart_view'
  | 'cart_remove'
  | 'checkout'
  | 'payment_pix'
  | 'payment_card'
  | 'payment_mock'
  | 'order_status'
  | 'order_list'
  | 'unknown'

// ─── Produto ──────────────────────────────────────────────────────────────────

export interface Product {
  id: string
  title: string
  price: number
  originalPrice?: number
  image: string
  source: string
  url?: string
  rating?: number
  available: boolean
}

// ─── Carrinho ─────────────────────────────────────────────────────────────────

export interface CartItem {
  productId: string
  title: string
  price: number
  quantity: number
  image?: string
}

export interface Cart {
  sessionId: string
  items: CartItem[]
  total: number
}

// ─── Checkout ─────────────────────────────────────────────────────────────────

export interface Address {
  name: string
  street: string
  number: string
  complement?: string
  neighborhood: string
  city: string
  state: string
  zipCode: string
}

export interface CheckoutData {
  orderId: string
  sessionId: string
  items: CartItem[]
  total: number          // total COM frete — valor real a pagar
  shippingCost: number   // custo do frete isolado
  isFreeShipping: boolean
  address?: Address
  shippingMethod?: 'standard' | 'express'
}

// ─── Pagamento ────────────────────────────────────────────────────────────────

export type PaymentStatus = 'pending' | 'processing' | 'paid' | 'failed' | 'cancelled'
export type PaymentProvider = 'mock' | 'stripe' | 'efi'
export type PaymentMethod = 'pix' | 'credit_card'

export interface PaymentResult {
  paymentId: string
  orderId: string
  status: PaymentStatus
  provider: PaymentProvider
  method: PaymentMethod
  pixQrCode?: string
  pixCopyPaste?: string
  pixExpiresAt?: string
  stripeClientSecret?: string
  amount: number         // total COM frete — igual ao checkoutData.total
}

// ─── Fulfillment / Rastreamento ───────────────────────────────────────────────

export type FulfillmentStatus =
  | 'payment_confirmed'   // Pagamento confirmado
  | 'preparing'           // Picking & Packing
  | 'ready_to_ship'       // Aguardando coleta
  | 'handed_to_carrier'   // Coletado pela transportadora
  | 'in_transit'          // Em trânsito
  | 'out_for_delivery'    // Saiu para entrega
  | 'delivered'           // Entregue
  | 'cancelled'           // Cancelado

export interface FulfillmentEvent {
  status: FulfillmentStatus
  description: string
  occurredAt: string       // ISO datetime
  trackingCode?: string
  carrierCode?: string
  location?: string
}

// ─── Pedido ───────────────────────────────────────────────────────────────────

export type OrderStatus =
  | 'created'
  | 'payment_pending'
  | 'payment_confirmed'
  | 'preparing'
  | 'shipped'
  | 'delivered'
  | 'cancelled'

export interface OrderEvent {
  status: OrderStatus
  label: string
  timestamp?: string
  done: boolean
}

export interface Order {
  orderId: string
  sessionId: string
  status: OrderStatus
  fulfillmentStatus?: FulfillmentStatus   // status detalhado do fulfillment
  items: CartItem[]
  total: number
  shippingCost?: number
  isFreeShipping?: boolean
  shippingMethod?: 'standard' | 'express'
  paymentMethod?: PaymentMethod           // método de pagamento usado
  paymentDetail?: string                   // chave Pix ou últimos 4 dígitos do cartão
  address?: Address                        // endereço de entrega
  createdAt: string
  tracking?: string
  events?: OrderEvent[]
  fulfillmentHistory?: FulfillmentEvent[] // histórico completo de eventos
}

// ─── Intent Response ──────────────────────────────────────────────────────────

export interface IntentResponse {
  intent: UcpIntent
  sessionId: string
  response: string
  data?: IntentData
}

export type IntentData =
  | { type: 'search';    products: Product[] }
  | { type: 'cart';      cart: Cart }
  | { type: 'checkout';  checkout: CheckoutData }
  | { type: 'payment';   payment: PaymentResult }
  | { type: 'order';     order: Order }
  | { type: 'orders';    orders: Order[] }

// ─── Mensagem do Chat ─────────────────────────────────────────────────────────

export type MessageRole = 'user' | 'bot'

export interface ChatMessage {
  id: string
  role: MessageRole
  text: string
  intent?: UcpIntent
  data?: IntentData
  timestamp: Date
  idempotencyKey?: string
}

// ─── Estado da sessão ─────────────────────────────────────────────────────────

export interface SessionState {
  sessionId: string
  step: UcpStep
  cart: Cart | null
  currentOrder: Order | null
  lastIdempotencyKey: string | null
  // Total com frete — mantido separado para pagamento consistente
  confirmedTotal: number
}
