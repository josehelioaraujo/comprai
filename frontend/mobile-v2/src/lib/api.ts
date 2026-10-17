import type { AuthResponse, Product, Cart, OrderSummary, FulfillmentEvent } from '@/types'

const BASE = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5020'

// ── helpers ──────────────────────────────────────────────────────────────────

export class ApiError extends Error {
  constructor(public status: number, message: string) {
    super(message)
    this.name = 'ApiError'
  }
}

async function request<T>(
  path: string,
  options: RequestInit = {},
  token?: string,
): Promise<T> {
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...(options.headers as Record<string, string> ?? {}),
  }
  if (token) headers['Authorization'] = `Bearer ${token}`

  const res = await fetch(`${BASE}${path}`, { ...options, headers, cache: 'no-store' })
  if (!res.ok) {
    const body = await res.text()
    throw new ApiError(res.status, body)
  }
  if (res.status === 204) return undefined as T
  return res.json()
}

// ── Auth ──────────────────────────────────────────────────────────────────────

export async function apiRegister(name: string, email: string, password: string): Promise<AuthResponse> {
  return request<AuthResponse>('/api/auth/register', {
    method: 'POST',
    body: JSON.stringify({ name, email, password }),
  })
}

export async function apiLogin(email: string, password: string): Promise<AuthResponse> {
  return request<AuthResponse>('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify({ email, password }),
  })
}

export async function apiSendVerification(token: string): Promise<void> {
  return request<void>('/api/auth/send-verification', { method: 'POST' }, token)
}

export async function apiVerifyEmail(code: string, token: string): Promise<void> {
  return request<void>('/api/auth/verify-email', {
    method: 'POST',
    body: JSON.stringify({ code }),
  }, token)
}

export async function apiMe(token: string) {
  return request<{ id: string; name: string; email: string; emailVerified: boolean }>('/api/auth/me', {}, token)
}

// ── Search ────────────────────────────────────────────────────────────────────

interface IntentRaw {
  intent:  string
  success: boolean
  data: {
    items?: Array<{
      id?: string; title?: string; price?: number
      imageUrl?: string; image?: string; source?: string
      url?: string; rating?: number; available?: boolean
    }>
  } | null
  message: string | null
}

export async function apiSearch(query: string, sessionId: string): Promise<Product[]> {
  const raw = await request<IntentRaw>('/api/intent', {
    method: 'POST',
    body: JSON.stringify({ text: query, sessionId }),
  })
  if (!raw.success || !raw.data?.items) return []
  return raw.data.items.map(item => ({
    id:        item.id ?? String(Math.random()),
    title:     item.title ?? 'Product',
    price:     item.price ?? 0,
    image:     item.imageUrl ?? item.image ?? '',
    source:    item.source ?? 'catalog',
    url:       item.url,
    rating:    item.rating,
    available: item.available !== false,
  }))
}

// ── Cart ──────────────────────────────────────────────────────────────────────

export async function apiGetCart(sessionId: string): Promise<Cart> {
  return request<Cart>(`/api/cart/${sessionId}`)
}

/** Add or update item quantity in cart.
 *  Pass full product details on first add; for quantity updates (increase/decrease)
 *  the backend uses the stored data — pass empty strings for title/image/source. */
export async function apiAddToCart(
  productId: string,
  quantity:  number,
  sessionId: string,
  meta?: { title?: string; price?: number; image?: string; source?: string },
): Promise<Cart> {
  return request<Cart>('/api/cart/add', {
    method: 'POST',
    body:   JSON.stringify({
      sessionId,
      productId,
      quantity,
      ...(meta ?? {}),
    }),
  })
}

export async function apiRemoveFromCart(productId: string, sessionId: string): Promise<Cart> {
  return request<Cart>('/api/cart/remove', {
    method: 'POST',
    body:   JSON.stringify({ sessionId, productId }),
  })
}

// ── Orders ────────────────────────────────────────────────────────────────────

/** Fetch order history. Pass either sessionId (guest) or token (authenticated). */
export async function apiGetOrders(
  sessionId?: string,
  token?: string,
): Promise<OrderSummary[]> {
  const qs = sessionId ? `?sessionId=${encodeURIComponent(sessionId)}` : ''
  return request<OrderSummary[]>(`/api/orders${qs}`, {}, token)
}

export async function apiGetFulfillment(orderId: string, token?: string): Promise<FulfillmentEvent[]> {
  return request<FulfillmentEvent[]>(`/api/orders/${orderId}/fulfillment`, {}, token)
}
