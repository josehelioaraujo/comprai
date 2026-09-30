import { generateUuid } from './session'

/**
 * Cache de idempotency keys em memória.
 * Regra: mesma ação do usuário = mesmo key (para retry seguro).
 * Nova ação = novo key.
 *
 * Key de cache: hash simples da ação (sessionId + intent + payload resumido)
 */
const keyCache = new Map<string, string>()

/**
 * Gera ou recupera um idempotency key para uma ação.
 * @param actionKey - identificador único da ação (ex: "cart_add:prod123:sessabc")
 * @returns UUID v4 estável para essa ação
 */
export function getIdempotencyKey(actionKey: string): string {
  if (keyCache.has(actionKey)) {
    return keyCache.get(actionKey)!
  }
  const key = generateUuid()
  keyCache.set(actionKey, key)
  return key
}

/**
 * Gera um key novo para uma nova ação do usuário (não retry).
 * Deve ser chamado quando o usuário digita uma nova mensagem.
 */
export function newIdempotencyKey(): string {
  return generateUuid()
}

/**
 * Monta o actionKey de cache para cart_add
 */
export function cartAddKey(sessionId: string, productId: string): string {
  return `cart_add:${sessionId}:${productId}`
}

/**
 * Monta o actionKey de cache para checkout
 */
export function checkoutKey(sessionId: string): string {
  return `checkout:${sessionId}`
}

/**
 * Monta o actionKey de cache para payment
 */
export function paymentKey(orderId: string, method: string): string {
  return `payment:${orderId}:${method}`
}

/**
 * Limpa o cache de keys (usar ao resetar sessão)
 */
export function clearIdempotencyCache(): void {
  keyCache.clear()
}
