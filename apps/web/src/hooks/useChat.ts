'use client'

import { useState, useCallback, useRef, useEffect } from 'react'
import { getSessionId, clearSession, createSession } from '@/lib/session'
import { newIdempotencyKey, getIdempotencyKey, cartAddKey, checkoutKey, paymentKey } from '@/lib/idempotency'
import { postIntent, addToCart, getCart, createCheckout, createPayment, getOrders, getFulfillmentTimeline } from '@/lib/api'
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

// Constantes de localStorage (mantidas como fallback quando UsarPostgres=false)
const LS_ORDER_KEY       = 'comprai_last_order_id'
const LS_SESSION_KEY     = 'comprai_last_session_id'
const LS_FULFILLMENT_KEY = 'comprai_last_fulfillment'
const LS_TRACKING_KEY    = 'comprai_last_tracking'
const LS_ORDERS_KEY      = 'comprai_orders_list'

// Intervalo de polling para fulfillment (ms)
const POLL_INTERVAL_MS   = 5_000
const POLL_MAX_ATTEMPTS  = 60 // 5 min

const INITIAL_SESSION: SessionState = {
  sessionId: '', step: 'idle', cart: null, currentOrder: null,
  lastIdempotencyKey: null, confirmedTotal: 0,
}

const WELCOME: ChatMessage = {
  id: 'welcome', role: 'bot',
  text: 'Olá! Sou o Comprai 🛍️ — seu assistente de compras com IA. O que você quer encontrar hoje?',
  timestamp: new Date(),
}

function loadOrdersList(): Order[] {
  try {
    const raw = localStorage.getItem(LS_ORDERS_KEY)
    return raw ? JSON.parse(raw) : []
  } catch { return [] }
}

function saveOrderToList(order: Order) {
  try {
    const list = loadOrdersList()
    const exists = list.findIndex(o => o.orderId === order.orderId)
    if (exists >= 0) list[exists] = order
    else list.unshift(order)
    localStorage.setItem(LS_ORDERS_KEY, JSON.stringify(list.slice(0, 20)))
  } catch { }
}

export function useChat() {
  const [messages, setMessages] = useState<ChatMessage[]>([WELCOME])
  const [session, setSession]   = useState<SessionState>(INITIAL_SESSION)
  const [isTyping, setIsTyping] = useState(false)
  const [hasPreviousOrder, setHasPreviousOrder] = useState(false)
  const sessionRef            = useRef<string>('')
  const fulfillmentHistoryRef = useRef<Record<string, FulfillmentEvent[]>>({})
  const pollTimersRef         = useRef<Record<string, ReturnType<typeof setTimeout>>>({})
  const pollAttemptsRef       = useRef<Record<string, number>>({})
  const addingProductsRef     = useRef<Set<string>>(new Set())
  const confirmedTotalRef     = useRef<number>(0)
  const shippingCostRef       = useRef<number>(0)
  const shippingMethodRef     = useRef<'standard' | 'express'>('standard')
  const paymentMethodRef      = useRef<PaymentMethod>('pix')
  const addressRef            = useRef<import('@/types/ucp').Address | null>(null)

  useEffect(() => {
    try {
      const savedOrderId   = localStorage.getItem(LS_ORDER_KEY)
      const savedSessionId = localStorage.getItem(LS_SESSION_KEY)
      if (savedOrderId && savedSessionId) setHasPreviousOrder(true)
    } catch { }
  }, [])

  // Limpa timers de polling ao desmontar
  useEffect(() => {
    return () => {
      Object.values(pollTimersRef.current).forEach(t => clearTimeout(t))
    }
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
      const updated = updater(m.data.order)
      saveOrderToList(updated)
      return { ...m, data: { type: 'order', order: updated }, timestamp: new Date() }
    }))
  }, [])

  const advanceStep = useCallback((step: UcpStep) => {
    setSession(s => ({ ...s, step }))
  }, [])

  const handleDismissMessage = useCallback((id: string) => {
    setMessages(prev => prev.filter(m => m.id !== id))
  }, [])

  const handleDismissAndReset = useCallback((id: string) => {
    setMessages(prev => {
      const msg = prev.find(m => m.id === id)
      if (msg?.intent === 'order_list') {
        setSession(s => ({ ...s, step: 'idle' }))
      }
      return prev.filter(m => m.id !== id)
    })
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

  /**
   * F5 — Polling real de fulfillment via GET /api/orders/{id}/fulfillment
   * Fallback: mantém simulação local se a API retornar vazio (UsarPostgres=false)
   */
  const startFulfillmentPolling = useCallback((orderId: string, initialHistory: FulfillmentEvent[]) => {
    fulfillmentHistoryRef.current[orderId] = initialHistory
    pollAttemptsRef.current[orderId] = 0

    const SIMULATION_PIPELINE: { status: FulfillmentStatus; description: string; delay: number; location?: string }[] = [
      { status: 'preparing',         description: 'Separando e embalando os itens',          delay: 8000  },
      { status: 'ready_to_ship',     description: 'Embalado — aguardando coleta',             delay: 6000  },
      { status: 'handed_to_carrier', description: 'Coletado pela transportadora',             delay: 5000  },
      { status: 'in_transit',        description: 'Em trânsito — Centro de Distribuição SP', delay: 8000, location: 'São Paulo, SP' },
      { status: 'out_for_delivery',  description: 'Saiu para entrega',                        delay: 6000  },
      { status: 'delivered',         description: 'Entregue ao destinatário',                 delay: 5000  },
    ]

    const orderStatusMap: Partial<Record<FulfillmentStatus, Order['status']>> = {
      preparing:         'preparing',
      handed_to_carrier: 'shipped',
      in_transit:        'shipped',
      out_for_delivery:  'shipped',
      delivered:         'delivered',
    }

    const poll = async () => {
      const attempt = (pollAttemptsRef.current[orderId] ?? 0) + 1
      pollAttemptsRef.current[orderId] = attempt

      if (attempt > POLL_MAX_ATTEMPTS) return // para de fazer polling

      try {
        const timeline = await getFulfillmentTimeline(orderId)

        if (timeline.length > 0) {
          // API retornou dados reais — usa polling real
          const prev = fulfillmentHistoryRef.current[orderId] ?? []
          if (timeline.length !== prev.length) {
            fulfillmentHistoryRef.current[orderId] = timeline
            const last = timeline[timeline.length - 1]
            updateOrderMessage(orderId, order => ({
              ...order,
              status:             orderStatusMap[last.status] ?? order.status,
              fulfillmentStatus:  last.status,
              fulfillmentHistory: timeline,
              tracking:           last.trackingCode ?? order.tracking,
            }))
          }
          // Continua polling até entregar
          const last = timeline[timeline.length - 1]
          if (last.status !== 'delivered' && last.status !== 'cancelled') {
            pollTimersRef.current[orderId] = setTimeout(poll, POLL_INTERVAL_MS)
          }
          return
        }
      } catch { /* API indisponível — cai no fallback */ }

      // Fallback: simulação local (UsarPostgres=false)
      if (attempt === 1) {
        let accDelay = 0
        const history = fulfillmentHistoryRef.current[orderId]
        for (const step of SIMULATION_PIPELINE) {
          accDelay += step.delay
          const stepCopy = { ...step }
          setTimeout(() => {
            const newEvent: FulfillmentEvent = {
              status:       stepCopy.status,
              description:  stepCopy.description,
              occurredAt:   new Date().toISOString(),
              location:     stepCopy.location,
              trackingCode: stepCopy.status === 'handed_to_carrier'
                ? `BR${orderId.slice(0, 6).toUpperCase()}001` : undefined,
            }
            history.push(newEvent)
            fulfillmentHistoryRef.current[orderId] = [...history]
            try { localStorage.setItem(LS_FULFILLMENT_KEY, JSON.stringify([...history])) } catch { }
            if (newEvent.trackingCode) {
              try { localStorage.setItem(LS_TRACKING_KEY, newEvent.trackingCode) } catch { }
            }
            updateOrderMessage(orderId, order => ({
              ...order,
              status:             orderStatusMap[stepCopy.status] ?? order.status,
              fulfillmentStatus:  stepCopy.status,
              fulfillmentHistory: [...history],
              tracking:           newEvent.trackingCode ?? order.tracking,
            }))
          }, accDelay)
        }
      }
    }

    // Primeira chamada após 3s (dá tempo do backend registrar o evento)
    pollTimersRef.current[orderId] = setTimeout(poll, 3000)
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
      shippingCostRef.current   = shippingCost
      shippingMethodRef.current = shippingMethod
      addressRef.current = {
        name:         customer.name,
        street:       customer.street,
        number:       customer.number,
        complement:   customer.complement,
        neighborhood: customer.neighborhood,
        city:         customer.city,
        state:        customer.state,
        zipCode:      customer.cep,
      }
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
    paymentMethodRef.current = method
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

  const handlePaymentConfirmed = useCallback((orderId: string, paymentDetail?: string) => {
    advanceStep('order')
    const orderTotal = confirmedTotalRef.current > 0 ? confirmedTotalRef.current : 0
    confirmedTotalRef.current = 0

    const cartSnapshot = session.cart?.items ?? []

    clearSession()
    const newSessionId = createSession()
    sessionRef.current = newSessionId

    setSession(s => ({
      ...s,
      cart: null,
      step: 'order',
      confirmedTotal: 0,
      sessionId: newSessionId,
      currentOrder: s.currentOrder
        ? { ...s.currentOrder, status: 'payment_confirmed', total: orderTotal }
        : null,
    }))

    try {
      localStorage.setItem(LS_ORDER_KEY,   orderId)
      localStorage.setItem(LS_SESSION_KEY, newSessionId)
      setHasPreviousOrder(false)
    } catch { }

    const initialHistory: FulfillmentEvent[] = [{
      status: 'payment_confirmed',
      description: 'Pagamento confirmado — seu pedido está sendo preparado',
      occurredAt: new Date().toISOString(),
    }]

    const newOrder: Order = {
      orderId,
      sessionId: newSessionId,
      status: 'payment_confirmed',
      fulfillmentStatus: 'payment_confirmed',
      items: cartSnapshot,
      total: orderTotal,
      shippingCost:    shippingCostRef.current,
      isFreeShipping:  shippingCostRef.current === 0,
      shippingMethod:  shippingMethodRef.current,
      paymentMethod:   paymentMethodRef.current,
      paymentDetail:   paymentDetail,
      address:         addressRef.current ?? undefined,
      createdAt: new Date().toISOString(),
      fulfillmentHistory: initialHistory,
    }

    saveOrderToList(newOrder)

    pushMessage({
      role: 'bot',
      text: `🎉 Pagamento confirmado! Seu pedido **#${orderId}** está sendo preparado.`,
      intent: 'order_status',
      data: { type: 'order', order: newOrder },
    })

    // F5: inicia polling real (com fallback local)
    startFulfillmentPolling(orderId, initialHistory)
  }, [pushMessage, advanceStep, startFulfillmentPolling, session.cart, session.confirmedTotal])

  /**
   * F5 — handleViewOrders: busca na API primeiro, fallback localStorage
   */
  const handleViewOrders = useCallback(async () => {
    const sessionId = getOrCreateSession()

    // Tenta API primeiro
    try {
      const apiOrders = await getOrders(sessionId)
      if (apiOrders.length > 0) {
        // Mescla com localStorage para manter dados de fulfillment local
        const localList = loadOrdersList()
        const merged = apiOrders.map(apiOrder => {
          const local = localList.find(o => o.orderId === apiOrder.orderId)
          return local ? {
            ...apiOrder,
            fulfillmentHistory: local.fulfillmentHistory ?? apiOrder.fulfillmentHistory,
            tracking:           local.tracking ?? apiOrder.tracking,
          } : apiOrder
        })
        upsertBotMessage({
          role: 'bot', text: '', intent: 'order_list',
          data: { type: 'orders', orders: merged },
        })
        advanceStep('order')
        return
      }
    } catch { /* fallback localStorage */ }

    // Fallback: localStorage
    const list = loadOrdersList()

    if (list.length === 0) {
      try {
        const savedOrderId     = localStorage.getItem(LS_ORDER_KEY)
        const savedSessionId   = localStorage.getItem(LS_SESSION_KEY)
        const savedFulfillment = localStorage.getItem(LS_FULFILLMENT_KEY)
        const savedTracking    = localStorage.getItem(LS_TRACKING_KEY)
        if (savedOrderId && savedSessionId) {
          const order: Order = {
            orderId:    savedOrderId,
            sessionId:  savedSessionId,
            status:     'delivered',
            fulfillmentStatus: 'delivered',
            items:      [],
            total:      0,
            tracking:   savedTracking ?? undefined,
            createdAt:  new Date().toISOString(),
            fulfillmentHistory: savedFulfillment ? JSON.parse(savedFulfillment) : [],
          }
          upsertBotMessage({
            role: 'bot', text: '', intent: 'order_list',
            data: { type: 'orders', orders: [order] },
          })
          advanceStep('order')
          return
        }
      } catch { }
      pushMessage({ role: 'bot', text: 'Nenhum pedido encontrado. Faça uma compra para acompanhar! 🛍️' })
      return
    }

    setMessages(prev => [...prev, {
      id: makeId(), role: 'bot' as const, timestamp: new Date(),
      text: '', intent: 'order_list',
      data: { type: 'orders', orders: list },
    }])
    advanceStep('order')
  }, [getOrCreateSession, pushMessage, upsertBotMessage, advanceStep])

  const handleRestorePreviousOrder = useCallback(() => {
    handleViewOrders()
    setHasPreviousOrder(false)
  }, [handleViewOrders])

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
    handleRestorePreviousOrder, handleDismissMessage, handleDismissAndReset,
  }
}
