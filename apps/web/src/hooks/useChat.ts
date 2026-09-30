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
  Cart,
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

  // ─── inicializa sessionId lazy ───────────────────────────────────────────
  const getOrCreateSession = useCallback((): string => {
    if (!sessionRef.current) {
      sessionRef.current = getSessionId()
      setSession((s) => ({ ...s, sessionId: sessionRef.current }))
    }
    return sessionRef.current
  }, [])

  // ─── adiciona mensagem na lista ──────────────────────────────────────────
  const pushMessage = useCallback((msg: Omit<ChatMessage, 'id' | 'timestamp'>) => {
    const full: ChatMessage = { ...msg, id: makeId(), timestamp: new Date() }
    setMessages((prev) => [...prev, full])
    return full
  }, [])

  // ─── avança o step do funil UCP ──────────────────────────────────────────
  const advanceStep = useCallback((step: UcpStep) => {
    setSession((s) => ({ ...s, step }))
  }, [])

  // ─── ENVIAR MENSAGEM (entrada principal) ─────────────────────────────────
  const sendMessage = useCallback(
    async (text: string) => {
      const sessionId = getOrCreateSession()
      const idempotencyKey = newIdempotencyKey() // nova ação = novo key

      // 1. mostra mensagem do usuário imediatamente
      pushMessage({ role: 'user', text, idempotencyKey })
      setIsTyping(true)

      try {
        // 2. chama /api/intent
        const res = await postIntent(text, sessionId, idempotencyKey)

        // 3. atualiza step do funil conforme intent
        const stepMap: Partial<Record<string, UcpStep>> = {
          search: 'search',
          cart_add: 'cart',
          cart_view: 'cart',
          cart_remove: 'cart',
          checkout: 'checkout',
          payment_pix: 'payment',
          payment_card: 'payment',
          payment_mock: 'payment',
          order_status: 'order',
          order_list: 'order',
        }
        const nextStep = stepMap[res.intent]
        if (nextStep) advanceStep(nextStep)

        // 4. atualiza carrinho no estado se vier no response
        if (res.data?.type === 'cart') {
          setSession((s) => ({ ...s, cart: res.data!.type === 'cart' ? (res.data as any).cart : s.cart }))
        }
        if (res.data?.type === 'order') {
          setSession((s) => ({ ...s, currentOrder: res.data!.type === 'order' ? (res.data as any).order : s.currentOrder }))
        }

        // 5. adiciona resposta do bot com data para o IntentRenderer
        pushMessage({
          role: 'bot',
          text: res.response,
          intent: res.intent,
          data: res.data,
          idempotencyKey,
        })
      } catch (err) {
        const msg = err instanceof Error ? err.message : 'Erro desconhecido'
        pushMessage({
          role: 'bot',
          text: `Ops! Algo deu errado: ${msg}. Tente novamente.`,
        })
      } finally {
        setIsTyping(false)
      }
    },
    [getOrCreateSession, pushMessage, advanceStep],
  )

  // ─── ADD TO CART (ação direta do ProductCarousel) ────────────────────────
  const handleAddToCart = useCallback(
    async (product: Product) => {
      const sessionId = getOrCreateSession()
      // idempotency estável: retry do mesmo produto = mesmo key
      const idempotencyKey = getIdempotencyKey(cartAddKey(sessionId, product.id))

      pushMessage({ role: 'user', text: `Adicionar: ${product.title}` })
      setIsTyping(true)

      try {
        const res = await addToCart(
          sessionId,
          {
            productId: product.id,
            title: product.title,
            price: product.price,
            quantity: 1,
            image: product.image,
          },
          idempotencyKey,
        )

        setSession((s) => ({ ...s, cart: res.cart, step: 'cart' }))

        pushMessage({
          role: 'bot',
          text: `✅ **${product.title}** adicionado ao carrinho!`,
          intent: 'cart_add',
          data: { type: 'cart', cart: res.cart },
        })
      } catch {
        pushMessage({ role: 'bot', text: 'Não consegui adicionar ao carrinho. Tente novamente.' })
      } finally {
        setIsTyping(false)
      }
    },
    [getOrCreateSession, pushMessage],
  )

  // ─── CHECKOUT (ação do CartCard) ─────────────────────────────────────────
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
          text: `Pedido criado! Como você quer pagar?`,
          intent: 'checkout',
          data: { type: 'checkout', checkout: res.checkout },
        })
      } catch {
        pushMessage({ role: 'bot', text: 'Erro ao criar pedido. Verifique o endereço e tente novamente.' })
      } finally {
        setIsTyping(false)
      }
    },
    [getOrCreateSession, pushMessage, advanceStep],
  )

  // ─── PAYMENT (ação do CheckoutCard) ──────────────────────────────────────
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
          data: { type: 'payment', payment: res.payment },
        })
      } catch {
        pushMessage({ role: 'bot', text: 'Erro ao iniciar pagamento. Tente novamente.' })
      } finally {
        setIsTyping(false)
      }
    },
    [pushMessage, advanceStep],
  )

  // ─── CONFIRMAR PAGAMENTO (mock / callback) ───────────────────────────────
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
