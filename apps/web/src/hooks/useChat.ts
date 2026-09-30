'use client'

import { useState, useCallback, useRef } from 'react'
import { getSessionId } from '@/lib/session'
import { newIdempotencyKey, getIdempotencyKey, cartAddKey, checkoutKey, paymentKey } from '@/lib/idempotency'
import { postIntent, addToCart, getCart, createCheckout, createPayment } from '@/lib/api'
import type {
  ChatMessage, SessionState, UcpStep, Product,
  PaymentMethod, PaymentProvider, Cart,
} from '@/types/ucp'
import type { CustomerDto } from '@/components/ucp/CartCard'

function makeId() { return Math.random().toString(36).slice(2) }

const INITIAL_SESSION: SessionState = {
  sessionId: '', step: 'idle', cart: null, currentOrder: null, lastIdempotencyKey: null,
}

const WELCOME: ChatMessage = {
  id: 'welcome', role: 'bot',
  text: 'Olá! Sou o Comprai 🛍️ — seu assistente de compras com IA. O que você quer encontrar hoje?',
  timestamp: new Date(),
}

export function useChat() {
  const [messages, setMessages] = useState<ChatMessage[]>([WELCOME])
  const [session, setSession] = useState<SessionState>(INITIAL_SESSION)
  const [isTyping, setIsTyping] = useState(false)
  const sessionRef = useRef<string>('')

  const getOrCreateSession = useCallback((): string => {
    if (!sessionRef.current) {
      sessionRef.current = getSessionId()
      setSession((s) => ({ ...s, sessionId: sessionRef.current }))
    }
    return sessionRef.current
  }, [])

  const pushMessage = useCallback((msg: Omit<ChatMessage, 'id' | 'timestamp'>) => {
    const full: ChatMessage = { ...msg, id: makeId(), timestamp: new Date() }
    setMessages((prev) => [...prev, full])
    return full
  }, [])

  const advanceStep = useCallback((step: UcpStep) => {
    setSession((s) => ({ ...s, step }))
  }, [])

  function normalizeCart(raw: any, sessionId: string): Cart {
    return {
      sessionId,
      items: (raw.items ?? []).map((i: any) => ({
        productId: i.product?.id ?? i.productId ?? '',
        title:     i.product?.title ?? i.title ?? '',
        price:     i.product?.price ?? i.price ?? 0,
        quantity:  i.quantity ?? 1,
        image:     i.product?.imageUrl ?? i.image ?? '',
      })),
      total: raw.total ?? 0,
    }
  }

  const sendMessage = useCallback(async (text: string) => {
    const sessionId = getOrCreateSession()
    const idempotencyKey = newIdempotencyKey()
    pushMessage({ role: 'user', text, idempotencyKey })
    setIsTyping(true)
    try {
      const res = await postIntent(text, sessionId, idempotencyKey)
      const stepMap: Partial<Record<string, UcpStep>> = {
        search:'search', cart_add:'cart', cart_view:'cart', cart_remove:'cart',
        checkout:'checkout', payment_pix:'payment', payment_card:'payment',
        payment_mock:'payment', order_status:'order', order_list:'order',
      }
      const nextStep = stepMap[res.intent]
      if (nextStep) advanceStep(nextStep)
      if (res.data?.type === 'cart') setSession((s) => ({ ...s, cart: (res.data as any).cart }))
      if (res.data?.type === 'order') setSession((s) => ({ ...s, currentOrder: (res.data as any).order }))
      pushMessage({ role:'bot', text:res.response, intent:res.intent, data:res.data, idempotencyKey })
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Erro desconhecido'
      pushMessage({ role:'bot', text:`Ops! Algo deu errado: ${msg}. Tente novamente.`, idempotencyKey })
    } finally { setIsTyping(false) }
  }, [getOrCreateSession, pushMessage, advanceStep])

  const handleAddToCart = useCallback(async (product: Product) => {
    const sessionId = getOrCreateSession()
    const idempotencyKey = getIdempotencyKey(cartAddKey(sessionId, product.id))
    pushMessage({ role:'user', text:`Adicionar: ${product.title}`, idempotencyKey })
    setIsTyping(true)
    try {
      await addToCart(sessionId, product, idempotencyKey)
      const raw = await getCart(sessionId)
      const cart = normalizeCart(raw, sessionId)
      setSession((s) => ({ ...s, cart, step: 'cart' }))
      pushMessage({ role:'bot', text:`✅ **${product.title}** adicionado ao carrinho!`,
        intent:'cart_add', data:{ type:'cart', cart }, idempotencyKey })
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Erro'
      pushMessage({ role:'bot', text:`Não consegui adicionar ao carrinho: ${msg}`, idempotencyKey })
    } finally { setIsTyping(false) }
  }, [getOrCreateSession, pushMessage])

  // handleCheckout agora recebe CustomerDto
  const handleCheckout = useCallback(async (customer: CustomerDto, shippingMethod: 'standard' | 'express') => {
    const sessionId = getOrCreateSession()
    const idempotencyKey = getIdempotencyKey(checkoutKey(sessionId))
    pushMessage({ role:'user', text:'Confirmar dados e finalizar pedido', idempotencyKey })
    setIsTyping(true)
    try {
      const res = await createCheckout(sessionId, customer, shippingMethod, idempotencyKey)
      advanceStep('checkout')
      const orderId = (res as any).orderId ?? (res as any).checkout?.orderId ?? ''
      pushMessage({ role:'bot', text:`Pedido **${orderId}** criado! Como você quer pagar?`,
        intent:'checkout', data:{ type:'checkout', checkout:(res as any).checkout ?? res }, idempotencyKey })
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Erro'
      pushMessage({ role:'bot', text:`Erro ao criar pedido: ${msg}`, idempotencyKey })
    } finally { setIsTyping(false) }
  }, [getOrCreateSession, pushMessage, advanceStep])

  const handlePayment = useCallback(async (orderId: string, provider: PaymentProvider, method: PaymentMethod) => {
    const idempotencyKey = getIdempotencyKey(paymentKey(orderId, method))
    pushMessage({ role:'user', text: method === 'pix' ? 'Pagar com Pix' : 'Pagar com cartão', idempotencyKey })
    setIsTyping(true)
    try {
      const res = await createPayment(orderId, provider, method, idempotencyKey)
      advanceStep('payment')
      const intent = method === 'pix' ? 'payment_pix' : 'payment_card'
      pushMessage({ role:'bot',
        text: method === 'pix' ? 'Escaneie o QR Code. Expira em 15 minutos.' : 'Insira os dados do cartão.',
        intent, data:{ type:'payment', payment:(res as any).payment ?? res }, idempotencyKey })
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Erro'
      pushMessage({ role:'bot', text:`Erro ao iniciar pagamento: ${msg}`, idempotencyKey })
    } finally { setIsTyping(false) }
  }, [pushMessage, advanceStep])

  const handlePaymentConfirmed = useCallback((orderId: string) => {
    advanceStep('order')
    setSession((s) => ({ ...s, cart:null, step:'order',
      currentOrder: s.currentOrder ? { ...s.currentOrder, status:'payment_confirmed' } : null }))
    pushMessage({ role:'bot',
      text:`🎉 Pagamento confirmado! Seu pedido **#${orderId}** está sendo preparado.`,
      intent:'order_status' })
  }, [pushMessage, advanceStep])

  return { messages, session, isTyping, sendMessage, handleAddToCart, handleCheckout, handlePayment, handlePaymentConfirmed }
}
