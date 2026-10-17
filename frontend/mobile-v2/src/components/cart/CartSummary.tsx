import type { Cart } from '@/types'
import Button from '@/components/ui/Button'
import Link from 'next/link'

interface Props {
  cart:      Cart
  disabled?: boolean
}

const fmt = (n: number) =>
  new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(n)

export default function CartSummary({ cart, disabled }: Props) {
  const subtotal  = cart.items.reduce((s, i) => s + i.price * i.quantity, 0)
  const shipping  = subtotal > 200 ? 0 : 19.9
  const total     = subtotal + shipping

  return (
    <div className="bg-white rounded-2xl shadow-card p-4 space-y-3">
      <h3 className="text-sm font-semibold text-ink">Order Summary</h3>

      <div className="space-y-2 text-sm">
        <div className="flex justify-between text-muted">
          <span>Subtotal ({cart.items.reduce((s, i) => s + i.quantity, 0)} items)</span>
          <span>{fmt(subtotal)}</span>
        </div>
        <div className="flex justify-between text-muted">
          <span>Shipping</span>
          <span>{shipping === 0 ? <span className="text-green-600 font-medium">Free</span> : fmt(shipping)}</span>
        </div>
        {shipping > 0 && (
          <p className="text-xs text-muted">Free shipping on orders over R$200</p>
        )}
        <div className="border-t border-border pt-2 flex justify-between font-semibold text-ink">
          <span>Total</span>
          <span>{fmt(total)}</span>
        </div>
      </div>

      <Link href="/checkout" className={disabled ? 'pointer-events-none' : ''}>
        <Button fullWidth disabled={disabled || cart.items.length === 0}>
          Proceed to Checkout
          <svg width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" d="M9 5l7 7-7 7"/>
          </svg>
        </Button>
      </Link>
    </div>
  )
}
