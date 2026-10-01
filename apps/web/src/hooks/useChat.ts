'use client'

import { useState, useCallback, useRef } from 'react'
import { getSessionId } from '@/lib/session'
import { newIdempotencyKey, getIdempotencyKey, cartAddKey, checkoutKey, paymentKey } from '@/lib/idempotency'
import { postIntent, addToCart, getCart, createCheckout, createPayment } from '@/lib/api'
import type { ChatMessage, SessionState, UcpStep, Product, PaymentMethod, PaymentProvider, Cart } from '@/types/ucp'
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
  const [messages, setMessages]   = useState<ChatMessage[]>([WELCOME])
  const [session, setSession]     = useState<SessionState>(INITIAL_SESSION)
  const [isTyping, setIsTyping]   = useState(false)
  const sessionRef                = useRef<string>('')

  const getOrCreateSession = useCallback((): string => {
    if (!sessionRef.current) {
      sessionRef.current = getSessionId()
      setSession(s => ({ ...s, sessionId: sessionRef.current }))
    }
    return sessionRef.current
  }, [])

  // Append simples — para mensagens do usuário e respostas novas
  const pushMessage = useCallback((msg: Omit<ChatMessage, 'id' | 'timestamp'>) => {
    const full: ChatMessage = { ...msg, id: makeId(), timestamp: new Date() }
    setMessages(prev => [...prev, full])
    return full
  }, [])

  // Upsert por intent — atualiza a última mensagem com esse intent em vez de duplicar
  const upsertBotMessage = useCallback((msg: Omit<ChatMessage, 'id' | 'timestamp'>) => {
    setMessages(prev => {
      // Procura a última mensagem bot com o mesmo intent
      const lastIdx = [...prev].reverse().findIndex(
        m => m.role === 'bot' && m.intent === msg.intent
      )
      if (lastIdx === -1) {
        // Não existe: append normal
        const full: ChatMessage = { ...msg, id: makeId(), timestamp: new Date() }
        return [...prev, full]
      }
      const realIdx = prev.length - 1 - lastIdx
      // Atualiza in-place mantendo o id e atualizando timestamp + data
      return prev.map((m, i) =>
        i === realIdx ? { ...m, text: msg.text, data: msg.data, timestamp: new Date() } : m
      )
    })
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
      if (res.data?.type === 'cart') setSession(s => ({ ...s, cart: (res.data as any).cart }))
      if (res.data?.type === 'order') setSession(s => ({ ...s, currentOrder: (res.data as any).order }))

      const text2 = res.intent === 'search' && res.data?.type === 'search' && res.data.products.length === 0
        ? 'Produto não encontrado. Tente outro termo de busca.' : res.response

      // cart_view e order_list fazem upsert; demais fazem push normal
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
    const sessionId      = getOrCreateSession()
    const idempotencyKey = getIdempotencyKey(cartAddKey(sessionId, product.id))
    setIsTyping(true)
    try {
      await addToCart(sessionId, product, idempotencyKey)
      const raw  = await getCart(sessionId)
      const cart = normalizeCart(raw, sessionId)
      setSession(s => ({ ...s, cart, step: 'search' }))
      const count = cart.items.reduce((s, i) => s + i.quantity, 0)
      pushMessage({
        role: 'bot',
        text: `✅ **${product.title}** adicionado! ${count} ${count === 1 ? 'item' : 'itens'} no carrinho.`,
        intent: 'cart_add', idempotencyKey,
      })
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Erro'
      pushMessage({ role: 'bot', text: `Não consegui adicionar ao carrinho: ${msg}`, idempotencyKey })
    } finally { setIsTyping(false) }
  }, [getOrCreateSession, pushMessage])

  // Abre carrinho — upsert: atualiza a tela existente em vez de duplicar
  // CRASH FIX: verifica cart null/vazio antes de renderizar
  // Assinatura () => void — compatível com onClick de botão
  const handleViewCart = useCallback(() => {
    const cart = session.cart
    if (!cart || !cart.items || cart.items.length === 0) {
      pushMessage({ role: 'bot', text: 'Seu carrinho está vazio. Busque produtos para adicionar!' })
      return
    }
    advanceStep('cart')
    upsertBotMessage({
      role: 'bot', text: 'Aqui está seu carrinho:',
      intent: 'cart_view', data: { type: 'cart', cart },
    })
  }, [pushMessage, upsertBotMessage, advanceStep, session.cart])

  const handleCheckout = useCallback(async (customer: CustomerDto, shippingMethod: 'standard' | 'express') => {
    const sessionId      = getOrCreateSession()
    const idempotencyKey = getIdempotencyKey(checkoutKey(sessionId))
    pushMessage({ role: 'user', text: 'Confirmar dados e finalizar pedido', idempotencyKey })
    setIsTyping(true)
    try {
      const res = await createCheckout(sessionId, customer, shippingMethod, idempotencyKey)
      advanceStep('checkout')
      const orderId      = (res as any).orderId ?? ''
      const checkoutData = {
        orderId, sessionId,
        items: session.cart?.items ?? [],
        total: session.cart?.total ?? 0,
        shippingMethod,
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

  const handlePayment = useCallback(async (orderId: string, _provider: PaymentProvider, method: PaymentMethod) => {
    const idempotencyKey = getIdempotencyKey(paymentKey(orderId, method))
    pushMessage({ role: 'user', text: method === 'pix' ? 'Pagar com Pix' : 'Pagar com cartão', idempotencyKey })
    setIsTyping(true)
    try {
      const amount = session.cart?.total ?? 0
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
  }, [pushMessage, advanceStep, session.cart])

  const handlePaymentConfirmed = useCallback((orderId: string) => {
    advanceStep('order')
    setSession(s => ({
      ...s, cart: null, step: 'order',
      currentOrder: s.currentOrder
        ? { ...s.currentOrder, status: 'payment_confirmed' }
        : null,
    }))
    // Mensagem de confirmação + intent order_status para exibir botão de histórico
    pushMessage({
      role: 'bot',
      text: `🎉 Pagamento confirmado! Seu pedido **#${orderId}** está sendo preparado.\nDeseja repetir a compra ou ver seus pedidos?`,
      intent: 'order_status',
      data: { type: 'order', order: {
        orderId,
        sessionId: sessionRef.current,
        status: 'payment_confirmed',
        items: [],
        total: 0,
        createdAt: new Date().toISOString(),
      }},
    })
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
    messages, session, cartCount, isTyping, sendMessage,
    handleAddToCart, handleViewCart, handleCheckout,
    handlePayment, handlePaymentConfirmed, handleQuantityChange,
  }
}
