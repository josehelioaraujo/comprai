import type {
  IntentResponse,
  Cart,
  CartItem,
  CheckoutData,
  PaymentResult,
  Order,
  Address,
  PaymentMethod,
  PaymentProvider,
} from '@/types/ucp'

const BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? 'https://comprai.2.25.122.11.nip.io'

// ─── Helpers ──────────────────────────────────────────────────────────────────

async function get<T>(path: string): Promise<T> {
  const res = await fetch(`${BASE_URL}${path}`, {
    headers: { 'Content-Type': 'application/json' },
    cache: 'no-store',
  })
  if (!res.ok) throw new ApiError(res.status, await res.text())
  return res.json()
}

async function post<T>(path: string, body: unknown, idempotencyKey: string): Promise<T> {
  const res = await fetch(`${BASE_URL}${path}`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'X-Idempotency-Key': idempotencyKey,  // ← rastreabilidade em TODA escrita
    },
    body: JSON.stringify(body),
    cache: 'no-store',
  })
  if (!res.ok) throw new ApiError(res.status, await res.text())
  return res.json()
}

export class ApiError extends Error {
  constructor(public status: number, message: string) {
    super(message)
    this.name = 'ApiError'
  }
}

// ─── Intent (entrada principal do chat) ──────────────────────────────────────

export async function postIntent(
  message: string,
  sessionId: string,
  idempotencyKey: string,
): Promise<IntentResponse> {
  return post<IntentResponse>('/api/intent', { message, sessionId }, idempotencyKey)
}

// ─── Search ───────────────────────────────────────────────────────────────────

export async function searchProducts(query: string, sessionId: string) {
  return get<{ products: import('@/types/ucp').Product[] }>(
    `/api/search?q=${encodeURIComponent(query)}&sessionId=${sessionId}`,
  )
}

// ─── Cart ─────────────────────────────────────────────────────────────────────

export async function getCart(sessionId: string): Promise<{ cart: Cart }> {
  return get(`/api/cart/${sessionId}`)
}

export async function addToCart(
  sessionId: string,
  item: Pick<CartItem, 'productId' | 'title' | 'price' | 'quantity'> & { image?: string },
  idempotencyKey: string,
): Promise<{ cart: Cart }> {
  return post(`/api/cart/${sessionId}/items`, item, idempotencyKey)
}

export async function removeFromCart(
  sessionId: string,
  productId: string,
  idempotencyKey: string,
): Promise<{ cart: Cart }> {
  return post(`/api/cart/${sessionId}/remove`, { productId }, idempotencyKey)
}

// ─── Checkout ─────────────────────────────────────────────────────────────────

export async function createCheckout(
  sessionId: string,
  address: Address,
  shippingMethod: 'standard' | 'express',
  idempotencyKey: string,
): Promise<{ checkout: CheckoutData }> {
  return post(`/api/checkout/${sessionId}`, { address, shippingMethod }, idempotencyKey)
}

// ─── Payment ──────────────────────────────────────────────────────────────────

export async function createPayment(
  orderId: string,
  provider: PaymentProvider,
  method: PaymentMethod,
  idempotencyKey: string,
): Promise<{ payment: PaymentResult }> {
  return post(`/api/payment/${orderId}`, { provider, method }, idempotencyKey)
}

// ─── Orders ───────────────────────────────────────────────────────────────────

export async function getOrder(orderId: string): Promise<{ order: Order }> {
  return get(`/api/orders/${orderId}`)
}

export async function getOrders(sessionId: string): Promise<{ orders: Order[] }> {
  return get(`/api/orders?sessionId=${sessionId}`)
}
