'use client'

import type { ChatMessage, Product, Address, PaymentMethod, PaymentProvider } from '@/types/ucp'
import ProductCarousel from './ucp/ProductCarousel'
import CartCard from './ucp/CartCard'
import CheckoutCard from './ucp/CheckoutCard'
import PixCard from './ucp/PixCard'
import StripeCard from './ucp/StripeCard'
import OrderTrackingCard from './ucp/OrderTrackingCard'

interface Props {
  message: ChatMessage
  onAddToCart: (product: Product) => void
  onCheckout: (address: Address, shipping: 'standard' | 'express') => void
  onPayment: (orderId: string, provider: PaymentProvider, method: PaymentMethod) => void
  onPaymentConfirmed: (orderId: string) => void
}

export default function IntentRenderer({
  message,
  onAddToCart,
  onCheckout,
  onPayment,
  onPaymentConfirmed,
}: Props) {
  const { intent, data } = message

  // sem intent ou sem data = só texto, nada a renderizar aqui
  if (!intent || !data) return null

  switch (intent) {
    // ── SEARCH → carousel de produtos ──────────────────────────────────────
    case 'search':
      if (data.type === 'search' && data.products.length > 0) {
        return <ProductCarousel products={data.products} onAddToCart={onAddToCart} />
      }
      return null

    // ── CART → resumo do carrinho ───────────────────────────────────────────
    case 'cart_add':
    case 'cart_view':
    case 'cart_remove':
      if (data.type === 'cart') {
        return (
          <CartCard
            cart={data.cart}
            onCheckout={(address, shipping) => onCheckout(address, shipping)}
          />
        )
      }
      return null

    // ── CHECKOUT → endereço + método de envio ──────────────────────────────
    case 'checkout':
      if (data.type === 'checkout') {
        return (
          <CheckoutCard
            checkout={data.checkout}
            onPayment={(method) =>
              onPayment(data.checkout.orderId, 'mock', method)
            }
          />
        )
      }
      return null

    // ── PAYMENT PIX → QR Code ──────────────────────────────────────────────
    case 'payment_pix':
    case 'payment_mock':
      if (data.type === 'payment') {
        return (
          <PixCard
            payment={data.payment}
            onConfirmed={() => onPaymentConfirmed(data.payment.orderId)}
          />
        )
      }
      return null

    // ── PAYMENT CARD → form Stripe ─────────────────────────────────────────
    case 'payment_card':
      if (data.type === 'payment') {
        return (
          <StripeCard
            payment={data.payment}
            onConfirmed={() => onPaymentConfirmed(data.payment.orderId)}
          />
        )
      }
      return null

    // ── ORDER STATUS → timeline de rastreio ────────────────────────────────
    case 'order_status':
    case 'order_list':
      if (data.type === 'order') {
        return <OrderTrackingCard order={data.order} />
      }
      if (data.type === 'orders') {
        return (
          <div className="flex flex-col gap-2">
            {data.orders.map((o) => (
              <OrderTrackingCard key={o.orderId} order={o} compact />
            ))}
          </div>
        )
      }
      return null

    default:
      return null
  }
}
