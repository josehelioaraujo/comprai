// ── Auth ──────────────────────────────────────────────────────────────────────

export interface AuthCustomer {
  id: string
  name: string
  email: string
  avatarUrl?: string | null
  emailVerified?: boolean
  provider?: string
}

export interface AuthResponse {
  token: string
  refreshToken: string
  customer: AuthCustomer
}

// ── Products ──────────────────────────────────────────────────────────────────

export interface Product {
  id: string
  title: string
  price: number
  originalPrice?: number
  image: string
  source?: string
  url?: string
  rating?: number
  available: boolean
}

// ── Cart ──────────────────────────────────────────────────────────────────────

export interface CartItem {
  itemId: string      // 8-char ID used by the backend for removal
  productId: string
  title: string
  price: number
  quantity: number
  image?: string
  source?: string
  url?: string
}

export interface Cart {
  sessionId?: string
  items: CartItem[]
  total?: number
  itemCount?: number
}

// ── Orders ────────────────────────────────────────────────────────────────────

export type OrderStatus =
  | 'pending'
  | 'confirmed'
  | 'processing'
  | 'shipped'
  | 'delivered'
  | 'cancelled'

export interface OrderSummary {
  orderId: string
  status: string
  total: number
  tracking?: string | null
  paymentMethod?: string | null
  shippingCity?: string | null
  shippingState?: string | null
  itemCount: number
  createdAt: string
}

export interface FulfillmentEvent {
  status: string
  description: string
  occurredAt: string
  trackingCode?: string | null
  location?: string | null
}

// ── UCP / Chat ────────────────────────────────────────────────────────────────

export type UcpStep = 'search' | 'cart' | 'checkout' | 'order'

export interface ChatMessage {
  id:       string
  role:     'user' | 'assistant'
  content:  string
  ts:       number
  products?: Product[]
}

// ── Session ───────────────────────────────────────────────────────────────────

declare module 'next-auth' {
  interface User {
    apiToken?:     string
    refreshToken?: string
    customerId?:   string
  }
  interface Session {
    user: {
      id:             string
      name?:          string | null
      email?:         string | null
      image?:         string | null
      emailVerified?: boolean | null
      apiToken?:      string
    }
  }
}
