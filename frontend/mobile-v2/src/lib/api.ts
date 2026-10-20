import type { AuthResponse, Product, Cart, CartItem, OrderSummary, FulfillmentEvent } from '@/types'

// In the browser we call /proxy/* so Next.js server-side rewrites forward to the API
// (avoids CORS — the browser never talks directly to the backend IP).
// In SSR/Node.js context, use the direct API URL.
const BASE = typeof window !== 'undefined'
  ? '/proxy'
  : (process.env.API_URL ?? process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5020')

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

export async function apiSendVerification(token: string, email?: string): Promise<void> {
  return request<void>('/api/auth/send-verification', {
    method: 'POST',
    body: email ? JSON.stringify({ email }) : undefined,
  }, token)
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

/** Raw shape returned by GET /api/cart/{sessionId} */
interface RawCartItem {
  itemId:   string
  product:  { id: string; title: string; price: number; imageUrl?: string; url?: string; category?: string; source: string }
  quantity: number
  subtotal: number
}
interface RawCart {
  sessionId: string
  items:     RawCartItem[]
  total:     number
}

function mapCart(raw: RawCart): Cart {
  return {
    sessionId: raw.sessionId,
    total:     raw.total,
    items:     (raw.items ?? []).map(i => ({
      itemId:    i.itemId,
      productId: i.product.id,
      title:     i.product.title,
      price:     i.product.price,
      quantity:  i.quantity,
      image:     i.product.imageUrl ?? '',
      source:    i.product.source,
      url:       i.product.url,
    } satisfies CartItem)),
  }
}

export async function apiGetCart(sessionId: string): Promise<Cart> {
  const raw = await request<RawCart>(`/api/cart/${sessionId}`)
  return mapCart(raw)
}

/**
 * Add a product to cart.  The backend accumulates quantity if the same
 * (productId + source) already exists, so calling with quantity:1 increments.
 * Returns the updated cart (fetched after add).
 */
export async function apiAddToCart(
  productId: string,
  quantity:  number,
  sessionId: string,
  meta?: { title?: string; price?: number; image?: string; source?: string; url?: string },
): Promise<Cart> {
  await request<{ itemId: string }>(`/api/cart/${sessionId}/items`, {
    method: 'POST',
    body:   JSON.stringify({
      product: {
        id:       productId,
        title:    meta?.title    ?? '',
        price:    meta?.price    ?? 0,
        imageUrl: meta?.image    ?? '',
        url:      meta?.url      ?? '',
        category: meta?.source   ?? '',
        source:   meta?.source   ?? 'catalog',
      },
      quantity,
    }),
  })
  return apiGetCart(sessionId)
}

/**
 * Remove a cart item by its itemId (the 8-char backend ID, not productId).
 * Returns the updated cart.
 */
export async function apiRemoveFromCart(itemId: string, sessionId: string): Promise<Cart> {
  await request<void>(`/api/cart/${sessionId}/items/${itemId}`, { method: 'DELETE' })
  return apiGetCart(sessionId)
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

/** Fetch a single order by ID (no auth required). */
export async function apiGetOrder(orderId: string, token?: string): Promise<OrderSummary | null> {
  try {
    return await request<OrderSummary>(`/api/orders/${orderId}`, {}, token)
  } catch {
    return null
  }
}

export async function apiGetFulfillment(orderId: string, token?: string): Promise<FulfillmentEvent[]> {
  try {
    return await request<FulfillmentEvent[]>(`/api/orders/${orderId}/fulfillment`, {}, token)
  } catch {
    return []
  }
}
