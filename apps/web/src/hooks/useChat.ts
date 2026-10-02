'use client'

import { useState, useCallback, useRef, useEffect } from 'react'
import { getSessionId } from '@/lib/session'
import { newIdempotencyKey, getIdempotencyKey, cartAddKey, checkoutKey, paymentKey } from '@/lib/idempotency'
import { postIntent, addToCart, getCart, createCheckout, createPayment } from '@/lib/api'
import type {
  ChatMessage, SessionState, UcpStep, Product,
  PaymentMethod, PaymentProvider, Cart, Order,
  FulfillmentStatus, FulfillmentEvent,
} from '@/types/ucp'
import type { CustomerDto } from '@/components/ucp/CartCard'

function makeId() { return Math.random().toString(36).slice(2) }

const FREE_SHIPPING_THRESHOLD = 1000
const STANDARD_COST           = 15.90
const EXPRESS_COST            = 29.90

export function calcShipping(subtotal: number, method: 'standard' | 'express'): number {
  if (subtotal >= FREE_SHIPPING_THRESHOLD) return 0
  return method === 'express' ? EXPRESS_COST : STANDARD_COST
}

const FULFILLMENT_SIMULATION = process.env.NEXT_PUBLIC_FULFILLMENT_SIMULATION !== 'false'

const FULFILLMENT_PIPELINE: { status: FulfillmentStatus; description: string; delay: number; location?: string }[] = [
  { status: 'preparing',         description: 'Separando e embalando os itens',          delay: 8000  },
  { status: 'ready_to_ship',     description: 'Embalado — aguardando coleta',             delay: 6000  },
  { status: 'handed_to_carrier', description: 'Coletado pela transportadora',             delay: 5000  },
  { status: 'in_transit',        description: 'Em trânsito — Centro de Distribuição SP', delay: 8000, location: 'São Paulo, SP' },
  { status: 'out_for_delivery',  description: 'Saiu para entrega',                        delay: 6000  },
  { status: 'delivered',         description: 'Entregue ao destinatário',                 delay: 5000  },
]

// ── Chaves de persistência localStorage ──────────────────────────────────────
const LS_ORDER_KEY       = 'comprai_last_order_id'
const LS_SESSION_KEY     = 'comprai_last_session_id'
const LS_FULFILLMENT_KEY = 'comprai_last_fulfillment'
const LS_TRACKING_KEY    = 'comprai_last_tracking'

const INITIAL_SESSION: SessionState = {
  sessionId: '', step: 'idle', cart: null, currentOrder: null,
  lastIdempotencyKey: null, confirmedTotal: 0,
}

const WELCOME: ChatMessage = {
  id: 'welcome', role: 'bot',
  text: 'Olá! Sou o Comprai 🛍️ — seu assistente de compras com IA. O que você quer encontrar hoje?',
  timestamp: new Date(),
}

export function useChat() {
  const [messages, setMessages] = useState<ChatMessage[]>([WELCOME])
  const [session, setSession]   = useState<SessionState>(INITIAL_SESSION)
  const [isTyping, setIsTyping] = useState(false)
  // Flag: exibe botão "Ver pedido anterior" enquanto não há pedido ativo
  const [hasPreviousOrder, setHasPreviousOrder] = useState(false)
  const sessionRef              = useRef<string>('')
  const fulfillmentHistoryRef   = useRef<Record<string, FulfillmentEvent[]>>({})
  const addingProductsRef       = useRef<Set<string>>(new Set())
  const confirmedTotalRef       = useRef<number>(0)

  // ── Detecta pedido anterior no localStorage ──────────────────────────────
  useEffect(() => {
    try {
      const savedOrderId   = localStorage.getItem(LS_ORDER_KEY)
      const savedSessionId = localStorage.getItem(LS_SESSION_KEY)
      if (savedOrderId && savedSessionId) {
        setHasPreviousOrder(true)
      }
    } catch { /* SSR / privado */ }
  }, [])

  const getOrCreateSession = useCallback((): string => {
    if (!sessionRef.current) {
      sessionRef.current = getSessionId()
      setSession(s => ({ ...s, sessionId: sessionRef.current }))
    }
    return sessionRef.current
  }, [])

  const pushMessage = useCallback((msg: Omit<ChatMessage, 'id' | 'timestamp'>) => {
    const full: ChatMessage = { ...msg, id: makeId(), timestamp: new Date() }
    setMessages(prev => [...prev, full])
    return full
  }, [])

  const upsertBotMessage = useCallback((msg: Omit<ChatMessage, 'id' | 'timestamp'>) => {
    setMessages(prev => {
      const lastIdx = [...prev].reverse().findIndex(
        m => m.role === 'bot' && m.intent === msg.intent
      )
      if (lastIdx === -1) {
        const full: ChatMessage = { ...msg, id: makeId(), timestamp: new Date() }
        return [...prev, full]
      }
      const realIdx = prev.length - 1 - lastIdx
      return prev.map((m, i) =>
        i === realIdx ? { ...m, text: msg.text, data: msg.data, timestamp: new Date() } : m
      )
    })
  }, [])

  const updateOrderMessage = useCallback((orderId: string, updater: (o: Order) => Order) => {
    setMessages(prev => prev.map(m => {
      if (m.intent !== 'order_status' || m.data?.type !== 'order') return m
      if (m.data.order.orderId !== orderId) return m
      return { ...m, data: { type: 'order', order: updater(m.data.order) }, timestamp: new Date() }
    }))
  }, [])

  const advanceStep = useCallback((step: UcpStep) => {
    setSession(s => ({ ...s, step }))
  }, [])

  function normalizeCart(raw: any, sessionId: string): Cart {
    return {
      sessionId,
      items: (raw?.items ?? []).map((i: any) => ({
        productId: i.product?.id ?? i.productId ?? '',
        title:     i.product?.title ?? i.title ?? '',
        price:     i.product?.price ?? i.price ?? 0,
        quantity:  i.quantity ?? 1,
        image:     i.product?.imageUrl ?? i.image ?? '',
      })),
      total: raw?.total ?? 0,
    }
  }

  const cartCount = session.cart?.items?.reduce((s, i) => s + i.quantity, 0) ?? 0

  const startFulfillmentSimulation = useCallback((orderId: string, orderTotal: number) => {
    const history: FulfillmentEvent[] = [{
      status: 'payment_confirmed',
      description: 'Pagamento confirmado — seu pedido está sendo preparado',
      occurredAt: new Date().toISOString(),
    }]
    fulfillmentHistoryRef.current[orderId] = history

    let accDelay = 0
    for (const step of FULFILLMENT_PIPELINE) {
      accDelay += step.delay
      const stepCopy = { ...step }
      const histCopy = history

      setTimeout(() => {
        const newEvent: FulfillmentEvent = {
          status:       stepCopy.status,
          description:  stepCopy.description,
          occurredAt:   new Date().toISOString(),
          location:     stepCopy.location,
          trackingCode: stepCopy.status === 'handed_to_carrier'
            ? `BR${orderId.slice(0, 6).toUpperCase()}001` : undefined,
        }
        histCopy.push(newEvent)
        fulfillmentHistoryRef.current[orderId] = [...histCopy]
        // Persiste no localStorage a cada etapa — restaurável na próxima sessão
        try { localStorage.setItem(LS_FULFILLMENT_KEY, JSON.stringify([...histCopy])) } catch { }
        // Salva tracking code quando disponível
        if (newEvent.trackingCode) {
          try { localStorage.setItem(LS_TRACKING_KEY, newEvent.trackingCode) } catch { }
        }

        const orderStatusMap: Partial<Record<FulfillmentStatus, Order['status']>> = {
          preparing:         'preparing',
          handed_to_carrier: 'shipped',
          in_transit:        'shipped',
          out_for_delivery:  'shipped',
          delivered:         'delivered',
        }

        updateOrderMessage(orderId, order => ({
          ...order,
          status:             orderStatusMap[stepCopy.status] ?? order.status,
          fulfillmentStatus:  stepCopy.status,
          fulfillmentHistory: [...histCopy],
          tracking:           newEvent.trackingCode ?? order.tracking,
        }))
      }, accDelay)
    }
  }, [updateOrderMessage])

  const sendMessage = useCallback(async (text: string) => {
    const sessionId      = getOrCreateSession()
    const idempotencyKey = newIdempotencyKey()
    pushMessage({ role: 'user', text, idempotencyKey })
    setIsTyping(true)
    try {
      const res = await postIntent(text, sessionId, idempotencyKey)
      const stepMap: Partial<Record<string, UcpStep>> = {
        search: 'search', cart_add: 'cart', cart_view: 'cart', cart_remove: 'cart',
        checkout: 'checkout', payment_pix: 'payment', payment_card: 'payment',
        payment_mock: 'payment', order_status: 'order', order_list: 'order',
      }
      const nextStep = stepMap[res.intent]
      if (nextStep) advanceStep(nextStep)
      if (res.data?.type === 'cart')  setSession(s => ({ ...s, cart: (res.data as any).cart }))
      if (res.data?.type === 'order') setSession(s => ({ ...s, currentOrder: (res.data as any).order }))

      const text2 = res.intent === 'search' && res.data?.type === 'search' && res.data.products.length === 0
        ? 'Produto não encontrado. Tente outro termo de busca.' : res.response

      const usesUpsert = ['cart_view', 'cart_remove', 'order_list', 'order_status'].includes(res.intent)
      if (usesUpsert) {
        upsertBotMessage({ role: 'bot', text: text2, intent: res.intent, data: res.data, idempotencyKey })
      } else {
        pushMessage({ role: 'bot', text: text2, intent: res.intent, data: res.data, idempotencyKey })
      }
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Erro desconhecido'
      pushMessage({ role: 'bot', text: `Ops! Algo deu errado: ${msg}. Tente novamente.`, idempotencyKey })
    } finally { setIsTyping(false) }
  }, [getOrCreateSession, pushMessage, upsertBotMessage, advanceStep])

  const handleAddToCart = useCallback(async (product: Product) => {
    if (addingProductsRef.current.has(product.id)) return
    addingProductsRef.current.add(product.id)

    const sessionId      = getOrCreateSession()
    const idempotencyKey = getIdempotencyKey(cartAddKey(sessionId, product.id))
    setIsTyping(true)
    try {
      await addToCart(sessionId, product, idempotencyKey)
      const raw  = await getCart(sessionId)
      const cart = normalizeCart(raw, sessionId)
      setSession(s => ({ ...s, cart, step: 'search' }))
      const count = cart.items.reduce((s, i) => s + i.quantity, 0)

      upsertBotMessage({
        role: 'bot',
        text: `✅ **${product.title}** adicionado! ${count} ${count === 1 ? 'item' : 'itens'} no carrinho.`,
        intent: 'cart_add', idempotencyKey,
      })
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Erro'
      pushMessage({ role: 'bot', text: `Não consegui adicionar ao carrinho: ${msg}`, idempotencyKey })
    } finally {
      setIsTyping(false)
      addingProductsRef.current.delete(product.id)
    }
  }, [getOrCreateSession, pushMessage, upsertBotMessage])

  const handleViewCart = useCallback(() => {
    const cart = session.cart
    if (!cart || !cart.items || cart.items.length === 0) {
      pushMessage({ role: 'bot', text: 'Seu carrinho está vazio. Busque produtos para adicionar! 🛍️' })
      return
    }
    advanceStep('cart')
    upsertBotMessage({
      role: 'bot', text: 'Aqui está seu carrinho:',
      intent: 'cart_view', data: { type: 'cart', cart },
    })
  }, [pushMessage, upsertBotMessage, advanceStep, session.cart])

  const handleCheckout = useCallback(async (
    customer: CustomerDto,
    shippingMethod: 'standard' | 'express',
    totalWithShipping: number,
  ) => {
    const sessionId      = getOrCreateSession()
    const idempotencyKey = getIdempotencyKey(checkoutKey(sessionId))
    pushMessage({ role: 'user', text: 'Confirmar dados e finalizar pedido', idempotencyKey })
    setIsTyping(true)
    try {
      const res = await createCheckout(sessionId, customer, shippingMethod, idempotencyKey)
      advanceStep('checkout')
      const orderId      = (res as any).orderId ?? ''
      const subtotal     = session.cart?.items.reduce((s, i) => s + i.price * i.quantity, 0) ?? 0
      const shippingCost = calcShipping(subtotal, shippingMethod)

      const checkoutData = {
        orderId, sessionId,
        items:          session.cart?.items ?? [],
        total:          totalWithShipping,
        shippingCost,
        isFreeShipping: shippingCost === 0,
        shippingMethod,
      }
      setSession(s => ({ ...s, confirmedTotal: totalWithShipping }))
      confirmedTotalRef.current = totalWithShipping
      pushMessage({
        role: 'bot', text: `Pedido **${orderId}** criado! Como você quer pagar?`,
        intent: 'checkout', data: { type: 'checkout', checkout: checkoutData }, idempotencyKey,
      })
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Erro'
      pushMessage({ role: 'bot', text: `Erro ao criar pedido: ${msg}`, idempotencyKey })
    } finally { setIsTyping(false) }
  }, [getOrCreateSession, pushMessage, advanceStep, session.cart])

  const handlePayment = useCallback(async (
    orderId: string, _provider: PaymentProvider, method: PaymentMethod
  ) => {
    const idempotencyKey = getIdempotencyKey(paymentKey(orderId, method))
    pushMessage({ role: 'user', text: method === 'pix' ? 'Pagar com Pix' : 'Pagar com cartão', idempotencyKey })
    setIsTyping(true)
    try {
      const amount = confirmedTotalRef.current > 0 ? confirmedTotalRef.current : (session.cart?.total ?? 0)
      const res    = await createPayment(orderId, method, amount, idempotencyKey)
      advanceStep('payment')
      const intent  = method === 'pix' ? 'payment_pix' : 'payment_card'
      const payment = {
        paymentId:    (res as any).paymentId ?? `pay-${Date.now()}`,
        orderId,
        status:       (res as any).status ?? 'pending',
        provider:     'mock' as PaymentProvider,
        method,
        pixQrCode:    (res as any).pixQrCode,
        pixCopyPaste: (res as any).pixCopyPaste ?? `00020126580014br.gov.bcb.pix0136${orderId}`,
        pixExpiresAt: (res as any).pixExpiresAt,
        amount,
      }
      pushMessage({
        role: 'bot',
        text: method === 'pix' ? 'Escaneie o QR Code. Expira em 15 minutos.' : 'Insira os dados do cartão.',
        intent, data: { type: 'payment', payment }, idempotencyKey,
      })
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Erro'
      pushMessage({ role: 'bot', text: `Erro ao iniciar pagamento: ${msg}`, idempotencyKey })
    } finally { setIsTyping(false) }
  }, [pushMessage, advanceStep, session.cart, session.confirmedTotal])

  const handlePaymentConfirmed = useCallback((orderId: string) => {
    advanceStep('order')
    const orderTotal = confirmedTotalRef.current > 0 ? confirmedTotalRef.current : 0

    confirmedTotalRef.current = 0
    setSession(s => ({
      ...s, cart: null, step: 'order', confirmedTotal: 0,
      currentOrder: s.currentOrder
        ? { ...s.currentOrder, status: 'payment_confirmed', total: orderTotal }
        : null,
    }))

    // Persiste no localStorage para "Ver pedido anterior"
    try {
      localStorage.setItem(LS_ORDER_KEY,   orderId)
      localStorage.setItem(LS_SESSION_KEY, sessionRef.current)
      setHasPreviousOrder(false) // pedido atual ativo — esconde botão
      // TODO V_DB_COMPRAI: migrar para PostgreSQL — fulfillment_event + order_history
    } catch { }

    const initialHistory: FulfillmentEvent[] = [{
      status: 'payment_confirmed',
      description: 'Pagamento confirmado — seu pedido está sendo preparado',
      occurredAt: new Date().toISOString(),
    }]

    pushMessage({
      role: 'bot',
      text: `🎉 Pagamento confirmado! Seu pedido **#${orderId}** está sendo preparado.`,
      intent: 'order_status',
      data: {
        type: 'order', order: {
          orderId,
          sessionId: sessionRef.current,
          status: 'payment_confirmed',
          fulfillmentStatus: 'payment_confirmed',
          items: [],
          total: orderTotal,
          createdAt: new Date().toISOString(),
          fulfillmentHistory: initialHistory,
        }
      },
    })

    startFulfillmentSimulation(orderId, orderTotal)
  }, [pushMessage, advanceStep, session.confirmedTotal, startFulfillmentSimulation])

  // Exibe o OrderTrackingCard — busca em todas as mensagens (não só na última)
  const handleViewOrders = useCallback(() => {
    // Busca a última mensagem order_status de qualquer posição
    const orderMsg = [...messages].reverse().find(
      m => m.role === 'bot' && m.intent === 'order_status' && m.data?.type === 'order'
    )

    if (orderMsg) {
      // Força re-render da mensagem para garantir visibilidade
      setMessages(prev => prev.map(m =>
        m.id === orderMsg.id ? { ...m, timestamp: new Date() } : m
      ))
      advanceStep('order')
    } else {
      // Tenta restaurar do localStorage se sessão anterior existir
      try {
        const savedOrderId   = localStorage.getItem(LS_ORDER_KEY)
        const savedSessionId = localStorage.getItem(LS_SESSION_KEY)
        if (savedOrderId && savedSessionId) {
          // Reconstrói uma mensagem sintética com o orderId salvo
          pushMessage({
            role: 'bot',
            text: `📦 Pedido anterior: **#${savedOrderId}**. Para detalhes completos, reinicie a sessão e consulte o pedido.`,
            intent: 'order_status',
            data: {
              type: 'order',
              order: {
                orderId:    savedOrderId,
                sessionId:  savedSessionId,
                status:     'delivered',
                fulfillmentStatus: 'delivered',
                items:      [],
                total:      0,
                createdAt:  new Date().toISOString(),
                fulfillmentHistory: [],
              }
            },
          })
          advanceStep('order')
        } else {
          pushMessage({ role: 'bot', text: 'Nenhum pedido encontrado nesta sessão. Faça uma compra para acompanhar!' })
        }
      } catch {
        pushMessage({ role: 'bot', text: 'Nenhum pedido encontrado nesta sessão. Faça uma compra para acompanhar!' })
      }
    }
  }, [messages, advanceStep, pushMessage])

  // "Ver pedido anterior" — restaura do localStorage ao reabrir
  const handleRestorePreviousOrder = useCallback(() => {
    try {
      const savedOrderId   = localStorage.getItem(LS_ORDER_KEY)
      const savedSessionId = localStorage.getItem(LS_SESSION_KEY)
      if (!savedOrderId || !savedSessionId) return

      const savedFulfillment = localStorage.getItem(LS_FULFILLMENT_KEY)
      const savedTracking    = localStorage.getItem(LS_TRACKING_KEY)
      const fulfillmentHistory = savedFulfillment ? JSON.parse(savedFulfillment) : []

      setHasPreviousOrder(false)
      pushMessage({
        role: 'bot',
        text: `📦 Seu pedido anterior **#${savedOrderId}** foi localizado.`,
        intent: 'order_status',
        data: {
          type: 'order',
          order: {
            orderId:    savedOrderId,
            sessionId:  savedSessionId,
            status:     'delivered',
            fulfillmentStatus: 'delivered',
            items:      [],
            total:      0,
            tracking:   savedTracking ?? undefined,
            createdAt:  new Date().toISOString(),
            fulfillmentHistory,
          }
        },
      })
      advanceStep('order')
    } catch { }
  }, [pushMessage, advanceStep])

  const handleQuantityChange = useCallback((productId: string, qty: number) => {
    setSession(s => {
      if (!s.cart) return s
      const items = s.cart.items.map(i =>
        i.productId === productId ? { ...i, quantity: qty } : i
      )
      const total = items.reduce((sum, i) => sum + i.price * i.quantity, 0)
      return { ...s, cart: { ...s.cart, items, total } }
    })
  }, [])

  return {
    messages, session, cartCount, isTyping, hasPreviousOrder, sendMessage,
    handleAddToCart, handleViewCart, handleViewOrders, handleCheckout,
    handlePayment, handlePaymentConfirmed, handleQuantityChange,
    handleRestorePreviousOrder,
  }
}
