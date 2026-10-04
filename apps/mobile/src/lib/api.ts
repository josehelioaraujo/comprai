import type {
  IntentResponse, Cart, CartItem, CheckoutData,
  PaymentResult, Order, PaymentMethod, PaymentProvider, Product,
} from '@/types/ucp'
import type { CustomerDto } from '@/components/ucp/CartCard'

const BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5020'

async function get<T>(path: string): Promise<T> {
  const res = await fetch(`${BASE_URL}${path}`, { headers: { 'Content-Type': 'application/json' }, cache: 'no-store' })
  if (!res.ok) throw new ApiError(res.status, await res.text())
  return res.json()
}

async function post<T>(path: string, body: unknown, idempotencyKey: string): Promise<T> {
  const res = await fetch(`${BASE_URL}${path}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', 'X-Idempotency-Key': idempotencyKey },
    body: JSON.stringify(body),
    cache: 'no-store',
  })
  if (!res.ok) throw new ApiError(res.status, await res.text())
  return res.json()
}

export class ApiError extends Error {
  constructor(public status: number, message: string) { super(message); this.name = 'ApiError' }
}

export async function postIntent(message: string, sessionId: string, idempotencyKey: string): Promise<IntentResponse> {
  const raw = await post<{ intent: string; success: boolean; data: any; message: string | null }>(
    '/api/intent', { text: message, sessionId }, idempotencyKey)
  const intentMap: Record<string, string> = {
    SearchProducts:'search', AddToCart:'cart_add', RemoveFromCart:'cart_remove',
    ViewCart:'cart_view', Checkout:'checkout', GetOrder:'order_status', Unknown:'unknown',
  }
  const intent = (intentMap[raw.intent] ?? 'unknown') as IntentResponse['intent']
  let data: IntentResponse['data'] | undefined = undefined
  if (raw.success && raw.data) {
    if (intent === 'search' && raw.data.items !== undefined) {
      data = { type:'search', products: (raw.data.items ?? []).map((item: any) => ({
        id: item.id ?? String(Math.random()), title: item.title ?? 'Produto',
        price: item.price ?? 0, originalPrice: item.originalPrice,
        image: item.imageUrl ?? item.image ?? '', source: item.source ?? 'catalog',
        url: item.url, rating: item.rating, available: item.available !== false,
      }))}
    } else if (['cart_add','cart_view','cart_remove'].includes(intent)) {
      data = { type:'cart', cart: raw.data }
    } else if (intent === 'checkout') { data = { type:'checkout', checkout: raw.data }
    } else if (intent === 'order_status') { data = { type:'order', order: raw.data } }
  }
  return { intent, sessionId, response: raw.message ?? messageForIntent(intent, raw.success), data }
}

function messageForIntent(intent: string, success: boolean): string {
  if (!success) return 'NÃ£o entendi. Tente: buscar produto, ver carrinho, finalizar pedido.'
  const msgs: Record<string, string> = {
    search:'Encontrei esses produtos para vocÃª:', cart_add:'Produto adicionado ao carrinho!',
    cart_view:'Aqui estÃ¡ seu carrinho:', cart_remove:'Produto removido do carrinho.',
    checkout:'Pedido criado! Como vocÃª quer pagar?', order_status:'Aqui estÃ¡ o status do seu pedido:',
  }
  return msgs[intent] ?? 'Ok!'
}

export async function searchProducts(query: string, sessionId: string) {
  return get<{ products: Product[] }>(`/api/search?q=${encodeURIComponent(query)}&sessionId=${sessionId}`)
}

export async function getCart(sessionId: string) {
  return get<any>(`/api/cart/${sessionId}`)
}

export async function addToCart(sessionId: string, product: Product, idempotencyKey: string): Promise<{ itemId: string }> {
  return post(`/api/cart/${sessionId}/items`, {
    product: { id:product.id, title:product.title, price:product.price, imageUrl:product.image,
      url:product.url ?? '', category:product.source, source:product.source,
      originalPrice:product.originalPrice ?? null, availableQuantity:null },
    quantity: 1,
  }, idempotencyKey)
}

export async function removeFromCart(sessionId: string, itemId: string, idempotencyKey: string): Promise<void> {
  await fetch(`${BASE_URL}/api/cart/${sessionId}/items/${itemId}`, {
    method: 'DELETE', headers: { 'X-Idempotency-Key': idempotencyKey }
  })
}
export async function clearCart(sessionId: string): Promise<void> {
  try {
    await fetch(`${BASE_URL}/api/cart/${sessionId}`, {
      method: 'DELETE', cache: 'no-store',
    })
  } catch {}
}

export async function createCheckout(
  sessionId: string, customer: CustomerDto,
  shippingMethod: 'standard' | 'express', idempotencyKey: string,
): Promise<any> {
  return post(`/api/checkout/${sessionId}`, customer, idempotencyKey)
}

// payload correto: { amount: "string", currency: "BRL", method: { provider, cardToken, pixKey } }
export async function createPayment(
  orderId: string,
  method: PaymentMethod,
  amount: number,
  idempotencyKey: string,
): Promise<any> {
  const amountStr = amount.toFixed(2) // string com 2 casas â evita problema de precisÃ£o float
  const body = {
    amount: amountStr,
    currency: 'BRL',
    method: {
      provider: 'mock',
      cardToken: method === 'credit_card' ? 'mock-card-token' : null,
      pixKey:    method === 'pix'         ? 'mock-pix-key'   : null,
    }
  }
  return post(`/api/payment/${orderId}`, body, idempotencyKey)
}

export async function getOrder(orderId: string): Promise<{ order: Order }> {
  return get(`/api/orders/${orderId}`)
}

export async function getOrders(sessionId: string): Promise<{ orders: Order[] }> {
  return get(`/api/orders?sessionId=${sessionId}`)
}

/** Persiste o pedido confirmado no Redis (fonte da verdade) */
export async function persistOrder(order: Order, sessionId: string): Promise<void> {
  try {
    await fetch(`${BASE_URL}/api/orders/persist`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ order, sessionId }),
      cache: 'no-store',
    })
  } catch {
    // falha silenciosa â localStorage ainda garante UX local
  }
}

/** Busca pedidos da sessÃ£o no Redis */
export async function getOrderBySession(sessionId: string): Promise<Order | null> {
  try {
    const res = await fetch(`${BASE_URL}/api/orders/session/${sessionId}`, {
      headers: { 'Content-Type': 'application/json' },
      cache: 'no-store',
    })
    if (!res.ok) return null
    const data = await res.json()
    return data?.order ?? null
  } catch {
    return null
  }
}
