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

const BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5020'

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
      'X-Idempotency-Key': idempotencyKey,
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

// ─── Intent ───────────────────────────────────────────────────────────────────
// Backend espera: { text: string, sessionId: string }
// Backend retorna: { intent, success, data, message }
// Mapeamos para o IntentResponse do front

export async function postIntent(
  message: string,
  sessionId: string,
  idempotencyKey: string,
): Promise<IntentResponse> {
  const raw = await post<{
    intent: string
    success: boolean
    data: any
    message: string | null
  }>('/api/intent', { text: message, sessionId }, idempotencyKey)

  // mapeia intent do backend (PascalCase) para o front (camelCase/snake)
  const intentMap: Record<string, string> = {
    SearchProducts: 'search',
    AddToCart:      'cart_add',
    RemoveFromCart: 'cart_remove',
    ViewCart:       'cart_view',
    Checkout:       'checkout',
    GetOrder:       'order_status',
    Unknown:        'unknown',
  }

  const intent = (intentMap[raw.intent] ?? 'unknown') as IntentResponse['intent']

  // monta data no formato que o IntentRenderer espera
  let data: IntentResponse['data'] | undefined = undefined

  if (raw.success && raw.data) {
    if (intent === 'search' && raw.data.items !== undefined) {
      // backend retorna { items, totalItems, page, pageSize, source }
      // mapeamos items → products
      data = {
        type: 'search',
        products: (raw.data.items ?? []).map((item: any) => ({
          id:            item.id ?? item.productId ?? String(Math.random()),
          title:         item.title ?? item.name ?? 'Produto',
          price:         item.price ?? 0,
          originalPrice: item.originalPrice,
          image:         item.imageUrl ?? item.image ?? '',
          source:        item.source ?? raw.data.source ?? 'catalog',
          url:           item.url,
          rating:        item.rating,
          available:     item.available !== false,
        })),
      }
    } else if (intent === 'cart_add' || intent === 'cart_view' || intent === 'cart_remove') {
      data = { type: 'cart', cart: raw.data }
    } else if (intent === 'checkout') {
      data = { type: 'checkout', checkout: raw.data }
    } else if (intent === 'order_status') {
      data = { type: 'order', order: raw.data }
    }
  }

  return {
    intent,
    sessionId,
    response: raw.message ?? messageForIntent(intent, raw.success),
    data,
  }
}

function messageForIntent(intent: string, success: boolean): string {
  if (!success) return 'Não entendi. Tente: buscar produto, ver carrinho, finalizar pedido.'
  const msgs: Record<string, string> = {
    search:      'Encontrei esses produtos para você:',
    cart_add:    'Produto adicionado ao carrinho!',
    cart_view:   'Aqui está seu carrinho:',
    cart_remove: 'Produto removido do carrinho.',
    checkout:    'Pedido criado! Como você quer pagar?',
    order_status:'Aqui está o status do seu pedido:',
  }
  return msgs[intent] ?? 'Ok!'
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
