'use client'

import type { ChatMessage, Product, PaymentMethod, PaymentProvider } from '@/types/ucp'
import type { CustomerDto } from '@/components/ucp/CartCard'

import MobileProductCarousel from './MobileProductCarousel'

import CartCard from '@/components/ucp/CartCard'
import CheckoutCard from '@/components/ucp/CheckoutCard'
import PixCard from '@/components/ucp/PixCard'
import StripeCard from '@/components/ucp/StripeCard'
import OrderTrackingCard from '@/components/ucp/OrderTrackingCard'
import OrderListCard from '@/components/ucp/OrderListCard'

interface Props {
  message: ChatMessage
  onAddToCart: (product: Product) => void
  onCheckout: (customer: CustomerDto, shipping: 'standard' | 'express', total: number) => void
  onPayment: (orderId: string, provider: PaymentProvider, method: PaymentMethod) => void
  onPaymentConfirmed: (orderId: string) => void
  onQuantityChange?: (productId: string, qty: number) => void
}

export default function MobileIntentRenderer({
  message, onAddToCart, onCheckout,
  onPayment, onPaymentConfirmed, onQuantityChange
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
        return <CartCard cart={data.cart} onCheckout={onCheckout} onQuantityChange={onQuantityChange} />
      return null

    case 'checkout':
      if (data.type === 'checkout')
        return (
          <CheckoutCard
            checkout={data.checkout}
            onPayment={(method) => onPayment(data.checkout.orderId, 'mock', method)}
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
      if (data.type === 'order') return <OrderTrackingCard order={data.order} />
      return null

    case 'order_list':
      if (data.type === 'orders') return <OrderListCard orders={data.orders} />
      if (data.type === 'order')  return <OrderTrackingCard order={data.order} />
      return null

    default:
      return null
  }
}
