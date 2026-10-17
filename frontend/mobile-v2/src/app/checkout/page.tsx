'use client'

export const dynamic = 'force-dynamic'

import { useState, useEffect } from 'react'
import { useSession } from 'next-auth/react'
import { useRouter } from 'next/navigation'
import AppShell from '@/components/layout/AppShell'
import Input  from '@/components/ui/Input'
import Button from '@/components/ui/Button'
import Spinner from '@/components/ui/Spinner'
import { apiGetCart } from '@/lib/api'
import { getSessionId } from '@/lib/session'
import type { Cart } from '@/types'
import Link from 'next/link'

const fmt = (n: number) =>
  new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(n)

type PaymentMethod = 'pix' | 'credit_card' | 'boleto'

export default function CheckoutPage() {
  const { data: session, status } = useSession()
  const router = useRouter()

  const [cart,    setCart]    = useState<Cart>({ items: [] })
  const [loading, setLoading] = useState(true)
  const [placing, setPlacing] = useState(false)
  const [error,   setError]   = useState('')

  // delivery form
  const [street,     setStreet]     = useState('')
  const [city,       setCity]       = useState('')
  const [state,      setState]      = useState('')
  const [zipCode,    setZipCode]    = useState('')
  const [payment,    setPayment]    = useState<PaymentMethod>('pix')

  useEffect(() => {
    const sessionId = getSessionId()
    apiGetCart(sessionId)
      .then(c => setCart(c))
      .catch(() => {})
      .finally(() => setLoading(false))
  }, [])

  async function handlePlaceOrder(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    setPlacing(true)

    const sessionId = getSessionId()
    const subtotal  = cart.items.reduce((s, i) => s + i.price * i.quantity, 0)
    const shipping  = subtotal > 200 ? 0 : 19.9
    const total     = subtotal + shipping

    try {
      // Step 1: Create checkout → get orderId
      const checkoutRes = await fetch(`/proxy/api/checkout/${sessionId}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          name:    session?.user?.name  ?? 'Guest',
          email:   session?.user?.email ?? 'guest@example.com',
          phone:   '(11) 99999-0000',
          address: `${street}, ${city} - ${state}, ${zipCode}`,
          cep:     zipCode,
        }),
      })
      if (!checkoutRes.ok) {
        const msg = await checkoutRes.text()
        throw new Error(msg || 'Checkout failed')
      }
      const checkout = await checkoutRes.json()
      const orderId: string = checkout.orderId ?? checkout.id

      // Step 2: Process payment
      const paymentRes = await fetch(`/proxy/api/payment/${orderId}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          amount:    total.toFixed(2),
          currency:  'BRL',
          sessionId,
          method: {
            provider:   'mock',
            cardToken:  payment === 'credit_card' ? 'tok_test_mock' : null,
            pixKey:     payment === 'pix'         ? 'mock-pix-key'  : null,
          },
        }),
      })
      if (!paymentRes.ok) {
        const msg = await paymentRes.text()
        throw new Error(msg || 'Payment failed')
      }

      router.push(`/orders/${orderId}`)
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Falha ao realizar o pedido')
    } finally {
      setPlacing(false)
    }
  }

  if (loading) {
    return <AppShell hideNav><div className="flex justify-center py-20"><Spinner size={32} /></div></AppShell>
  }

  const subtotal = cart.items.reduce((s, i) => s + i.price * i.quantity, 0)
  const shipping = subtotal > 200 ? 0 : 19.9
  const total    = subtotal + shipping

  return (
    <AppShell hideNav>
      <div className="px-4 pt-12 pb-3 bg-white border-b border-border flex items-center gap-3">
        <Link href="/cart" className="text-muted hover:text-ink transition-colors">
          <svg width="22" height="22" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" d="M15 19l-7-7 7-7"/>
          </svg>
        </Link>
        <h1 className="text-xl font-bold text-ink">Finalizar Pedido</h1>
      </div>

      <form onSubmit={handlePlaceOrder} className="p-4 space-y-4 pb-8">
        {/* order items preview */}
        <div className="bg-white rounded-2xl shadow-card p-4 space-y-2">
          <h3 className="text-sm font-semibold text-ink">Seu Pedido</h3>
          {cart.items.map(item => (
            <div key={item.productId} className="flex justify-between text-sm">
              <span className="text-muted line-clamp-1 flex-1 mr-2">
                {item.title} <span className="text-xs">×{item.quantity}</span>
              </span>
              <span className="font-medium text-ink shrink-0">
                {fmt(item.price * item.quantity)}
              </span>
            </div>
          ))}
          <div className="border-t border-border pt-2 flex justify-between text-sm">
            <span className="text-muted">Frete</span>
            <span className={shipping === 0 ? 'text-green-600 font-medium' : 'text-ink'}>
              {shipping === 0 ? 'Grátis' : fmt(shipping)}
            </span>
          </div>
          <div className="flex justify-between font-bold text-ink">
            <span>Total</span><span>{fmt(total)}</span>
          </div>
        </div>

        {/* delivery */}
        <div className="bg-white rounded-2xl shadow-card p-4 space-y-3">
          <h3 className="text-sm font-semibold text-ink">Endereço de Entrega</h3>
          <Input label="Rua e Número" value={street} onChange={e => setStreet(e.target.value)}
            placeholder="Rua Exemplo, 123" required />
          <div className="grid grid-cols-2 gap-3">
            <Input label="Cidade" value={city} onChange={e => setCity(e.target.value)}
              placeholder="São Paulo" required />
            <Input label="Estado" value={state} onChange={e => setState(e.target.value)}
              placeholder="SP" maxLength={2} required />
          </div>
          <Input label="CEP" value={zipCode} onChange={e => setZipCode(e.target.value)}
            placeholder="00000-000" required />
        </div>

        {/* payment */}
        <div className="bg-white rounded-2xl shadow-card p-4 space-y-3">
          <h3 className="text-sm font-semibold text-ink">Forma de Pagamento</h3>
          {(['pix', 'credit_card', 'boleto'] as PaymentMethod[]).map(m => (
            <label key={m} className={[
              'flex items-center gap-3 p-3 rounded-xl border cursor-pointer transition-colors',
              payment === m ? 'border-brand bg-brand-light' : 'border-border',
            ].join(' ')}>
              <input type="radio" name="payment" value={m} checked={payment === m}
                onChange={() => setPayment(m)} className="accent-brand" />
              <span className="text-sm font-medium capitalize text-ink">
                {m === 'credit_card' ? 'Cartão de Crédito' : m === 'boleto' ? 'Boleto' : 'Pix'}
              </span>
            </label>
          ))}
        </div>

        {error && <p className="text-sm text-red-500 text-center">{error}</p>}

        <Button type="submit" fullWidth loading={placing} disabled={cart.items.length === 0}>
          Confirmar Pedido · {fmt(total)}
        </Button>
      </form>
    </AppShell>
  )
}
