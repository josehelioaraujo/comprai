'use client'

import { useState, useCallback, useRef } from 'react'
import { getSessionId } from '@/lib/session'
import { newIdempotencyKey, getIdempotencyKey, cartAddKey, checkoutKey, paymentKey } from '@/lib/idempotency'
import { postIntent, addToCart, createCheckout, createPayment } from '@/lib/api'
import type {
  ChatMessage,
  SessionState,
  UcpStep,
  Product,
  Address,
  PaymentMethod,
  PaymentProvider,
} from '@/types/ucp'

function makeId() {
  return Math.random().toString(36).slice(2)
}

const INITIAL_SESSION: SessionState = {
  sessionId: '',
  step: 'idle',
  cart: null,
  currentOrder: null,
  lastIdempotencyKey: null,
}

const WELCOME: ChatMessage = {
  id: 'welcome',
  role: 'bot',
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

  // ─── ENVIAR MENSAGEM ──────────────────────────────────────────────────────
  const sendMessage = useCallback(
    async (text: string) => {
      const sessionId = getOrCreateSession()
      const idempotencyKey = newIdempotencyKey()

      pushMessage({ role: 'user', text, idempotencyKey })
      setIsTyping(true)

      try {
        const res = await postIntent(text, sessionId, idempotencyKey)

        const stepMap: Partial<Record<string, UcpStep>> = {
          search:       'search',
          cart_add:     'cart',
          cart_view:    'cart',
          cart_remove:  'cart',
          checkout:     'checkout',
          payment_pix:  'payment',
          payment_card: 'payment',
          payment_mock: 'payment',
          order_status: 'order',
          order_list:   'order',
        }
        const nextStep = stepMap[res.intent]
        if (nextStep) advanceStep(nextStep)

        if (res.data?.type === 'cart') {
          setSession((s) => ({ ...s, cart: (res.data as any).cart }))
        }
        if (res.data?.type === 'order') {
          setSession((s) => ({ ...s, currentOrder: (res.data as any).order }))
        }

        pushMessage({
          role: 'bot',
          text: res.response,
          intent: res.intent,
          data: res.data,
          idempotencyKey,
        })
      } catch (err) {
        const msg = err instanceof Error ? err.message : 'Erro desconhecido'
        pushMessage({ role: 'bot', text: `Ops! Algo deu errado: ${msg}. Tente novamente.` })
      } finally {
        setIsTyping(false)
      }
    },
    [getOrCreateSession, pushMessage, advanceStep],
  )

  // ─── ADD TO CART (direto do ProductCarousel) ──────────────────────────────
  const handleAddToCart = useCallback(
    async (product: Product) => {
      const sessionId = getOrCreateSession()
      const idempotencyKey = getIdempotencyKey(cartAddKey(sessionId, product.id))

      pushMessage({ role: 'user', text: `Adicionar: ${product.title}`, idempotencyKey })
      setIsTyping(true)

      try {
        // addToCart agora recebe Product completo — alinhado com a assinatura do backend
        await addToCart(sessionId, product, idempotencyKey)

        // busca carrinho atualizado
        const { getCart } = await import('@/lib/api')
        const cartData = await getCart(sessionId)

        const cart = {
          sessionId,
          items: (cartData as any).items ?? [],
          total: (cartData as any).total ?? product.price,
        }

        setSession((s) => ({ ...s, cart, step: 'cart' }))

        pushMessage({
          role: 'bot',
          text: `✅ **${product.title}** adicionado ao carrinho!`,
          intent: 'cart_add',
          data: { type: 'cart', cart },
          idempotencyKey,
        })
      } catch (err) {
        const msg = err instanceof Error ? err.message : 'Erro'
        pushMessage({ role: 'bot', text: `Não consegui adicionar ao carrinho: ${msg}` })
      } finally {
        setIsTyping(false)
      }
    },
    [getOrCreateSession, pushMessage],
  )

  // ─── CHECKOUT ─────────────────────────────────────────────────────────────
  const handleCheckout = useCallback(
    async (address: Address, shippingMethod: 'standard' | 'express') => {
      const sessionId = getOrCreateSession()
      const idempotencyKey = getIdempotencyKey(checkoutKey(sessionId))

      pushMessage({ role: 'user', text: 'Confirmar endereço e finalizar pedido' })
      setIsTyping(true)

      try {
        const res = await createCheckout(sessionId, address, shippingMethod, idempotencyKey)
        advanceStep('checkout')

        pushMessage({
          role: 'bot',
          text: 'Pedido criado! Como você quer pagar?',
          intent: 'checkout',
          data: { type: 'checkout', checkout: (res as any).checkout ?? res },
        })
      } catch (err) {
        pushMessage({ role: 'bot', text: 'Erro ao criar pedido. Verifique o endereço e tente novamente.' })
      } finally {
        setIsTyping(false)
      }
    },
    [getOrCreateSession, pushMessage, advanceStep],
  )

  // ─── PAYMENT ──────────────────────────────────────────────────────────────
  const handlePayment = useCallback(
    async (orderId: string, provider: PaymentProvider, method: PaymentMethod) => {
      const idempotencyKey = getIdempotencyKey(paymentKey(orderId, method))

      pushMessage({ role: 'user', text: method === 'pix' ? 'Pagar com Pix' : 'Pagar com cartão' })
      setIsTyping(true)

      try {
        const res = await createPayment(orderId, provider, method, idempotencyKey)
        advanceStep('payment')

        const intent = method === 'pix' ? 'payment_pix' : 'payment_card'
        pushMessage({
          role: 'bot',
          text: method === 'pix'
            ? 'Escaneie o QR Code para pagar. Expira em 15 minutos.'
            : 'Insira os dados do cartão abaixo.',
          intent,
          data: { type: 'payment', payment: (res as any).payment ?? res },
        })
      } catch (err) {
        pushMessage({ role: 'bot', text: 'Erro ao iniciar pagamento. Tente novamente.' })
      } finally {
        setIsTyping(false)
      }
    },
    [pushMessage, advanceStep],
  )

  // ─── PAYMENT CONFIRMED ────────────────────────────────────────────────────
  const handlePaymentConfirmed = useCallback(
    (orderId: string) => {
      advanceStep('order')
      setSession((s) => ({
        ...s,
        cart: null,
        step: 'order',
        currentOrder: s.currentOrder
          ? { ...s.currentOrder, status: 'payment_confirmed' }
          : null,
      }))
      pushMessage({
        role: 'bot',
        text: `🎉 Pagamento confirmado! Seu pedido **#${orderId}** está sendo preparado.`,
        intent: 'order_status',
      })
    },
    [pushMessage, advanceStep],
  )

  return {
    messages,
    session,
    isTyping,
    sendMessage,
    handleAddToCart,
    handleCheckout,
    handlePayment,
    handlePaymentConfirmed,
  }
}
