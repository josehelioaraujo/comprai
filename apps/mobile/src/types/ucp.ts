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
  source: string       // MercadoLivre | Shopify | DummyJSON | etc
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
  city: string
  state: string
  zipCode: string
}

export interface CheckoutData {
  orderId: string
  sessionId: string
  items: CartItem[]
  total: number
  address?: Address
  shippingMethod?: 'standard' | 'express'
  shippingCost?: number
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
  pixQrCode?: string         // base64 imagem QR
  pixCopyPaste?: string      // código copia-e-cola
  pixExpiresAt?: string      // ISO datetime
  stripeClientSecret?: string
  amount: number
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
  items: CartItem[]
  total: number
  createdAt: string
  tracking?: string
  events?: OrderEvent[]
}

// ─── Intent Response (POST /api/intent) ──────────────────────────────────────

export interface IntentResponse {
  intent: UcpIntent
  sessionId: string
  response: string           // texto que o bot fala
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
  idempotencyKey?: string    // rastreabilidade da ação
}

// ─── Estado da sessão ─────────────────────────────────────────────────────────

export interface SessionState {
  sessionId: string
  step: UcpStep
  cart: Cart | null
  currentOrder: Order | null
  lastIdempotencyKey: string | null
}
