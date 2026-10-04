'use client'

import { useState } from 'react'
import type { ChatMessage, Product, PaymentMethod, PaymentProvider, Order } from '@/types/ucp'
import type { CustomerDto } from '@/components/ucp/CartCard'

import MobileProductCarousel from './MobileProductCarousel'
import CartCard      from '@/components/ucp/CartCard'
import CheckoutCard  from '@/components/ucp/CheckoutCard'
import PixCard       from '@/components/ucp/PixCard'
import StripeCard    from '@/components/ucp/StripeCard'
import OrderTrackingCard from '@/components/ucp/OrderTrackingCard'

interface Props {
  message: ChatMessage
  onAddToCart: (product: Product) => void
  onCheckout: (customer: CustomerDto, shipping: 'standard' | 'express', total: number) => void
  onPayment: (orderId: string, provider: PaymentProvider, method: PaymentMethod) => void
  onPaymentConfirmed: (orderId: string) => void
  onQuantityChange?: (productId: string, qty: number) => void
  onClose?: () => void          // dismiss da mensagem inteira
}

// ── Lista de pedidos com drilldown ────────────────────────────────────────────
function OrderList({
  orders,
  onClose,
}: {
  orders: Order[]
  onClose?: () => void
}) {
  const [selected, setSelected] = useState<Order | null>(null)

  function fmt(v: number) {
    return v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
  }

  if (selected) {
    return (
      <div className="mt-2">
        <button
          onClick={() => setSelected(null)}
          className="mb-2 flex items-center gap-1.5 text-[11px] transition-opacity opacity-60 hover:opacity-100"
          style={{ color: 'var(--muted)' }}
        >
          ← Voltar para lista
        </button>
        <OrderTrackingCard order={selected} onClose={() => setSelected(null)} />
      </div>
    )
  }

  return (
    <div className="mt-2 rounded-xl overflow-hidden" style={{ background: 'var(--panel)', border: '1px solid var(--border)' }}>
      {/* Header */}
      <div className="px-4 py-3 flex items-center justify-between" style={{ borderBottom: '1px solid var(--border)' }}>
        <span className="text-sm font-semibold" style={{ color: 'var(--text)' }}>
          📋 Meus pedidos
        </span>
        <div className="flex items-center gap-2">
          <span className="text-[10px]" style={{ color: 'var(--muted)' }}>
            {orders.length} {orders.length === 1 ? 'pedido' : 'pedidos'}
          </span>
          {onClose && (
            <button
              onClick={onClose}
              className="w-6 h-6 rounded-full flex items-center justify-center opacity-40 hover:opacity-100 transition-opacity"
              style={{ background: 'var(--surface)', color: 'var(--muted)' }}
            >
              <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" className="w-3 h-3">
                <path d="M3 3l10 10M13 3L3 13" />
              </svg>
            </button>
          )}
        </div>
      </div>

      {/* Linhas: só nº pedido + valor */}
      {orders.map((order, i) => {
        const isLast = i === orders.length - 1
        return (
          <button
            key={order.orderId}
            onClick={() => setSelected(order)}
            className="w-full px-4 py-3.5 flex items-center justify-between gap-3 text-left transition-colors active:opacity-70"
            style={{
              borderBottom: isLast ? 'none' : '1px solid var(--border)',
              background: 'transparent',
            }}
          >
            <p className="text-[12px] font-mono" style={{ color: 'var(--text)' }}>
              #{order.orderId.length > 18 ? order.orderId.slice(0, 18) + '…' : order.orderId}
            </p>
            <div className="flex items-center gap-2 flex-shrink-0">
              <span className="text-sm font-bold font-mono" style={{ color: 'var(--accent)' }}>
                {order.total > 0 ? fmt(order.total) : '—'}
              </span>
              <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" className="w-3 h-3 opacity-30">
                <path d="M6 3l5 5-5 5" />
              </svg>
            </div>
          </button>
        )
      })}
    </div>
  )
}

// ── Renderer principal ────────────────────────────────────────────────────────
export default function MobileIntentRenderer({
  message, onAddToCart, onCheckout,
  onPayment, onPaymentConfirmed, onQuantityChange, onClose,
}: Props) {
  const { intent, data } = message
  if (!intent || !data) return null

  switch (intent) {
    case 'search':
      if (data.type === 'search' && data.products.length > 0)
        return <MobileProductCarousel products={data.products} onAddToCart={onAddToCart} />
      return null

    case 'cart_add':
    case 'cart_view':
    case 'cart_remove':
      if (data.type === 'cart')
        return (
          <CartCard
            cart={data.cart}
            onCheckout={onCheckout}
            onQuantityChange={onQuantityChange}
            onClose={onClose}
          />
        )
      return null

    case 'checkout':
      if (data.type === 'checkout')
        return (
          <CheckoutCard
            checkout={data.checkout}
            onPayment={(method) => onPayment(data.checkout.orderId, 'mock', method)}
            onClose={onClose}
          />
        )
      return null

    case 'payment_pix':
    case 'payment_mock':
      if (data.type === 'payment')
        return <PixCard payment={data.payment} onConfirmed={() => onPaymentConfirmed(data.payment.orderId)} />
      return null

    case 'payment_card':
      if (data.type === 'payment')
        return <StripeCard payment={data.payment} onConfirmed={() => onPaymentConfirmed(data.payment.orderId)} />
      return null

    case 'order_status':
      if (data.type === 'order')
        return <OrderTrackingCard order={data.order} onClose={onClose} />
      return null

    case 'order_list':
      if (data.type === 'orders')
        return <OrderList orders={data.orders} onClose={onClose} />
      if (data.type === 'order')
        return <OrderTrackingCard order={data.order} onClose={onClose} />
      return null

    default:
      return null
  }
}
