'use client'

import type { CheckoutData, PaymentMethod } from '@/types/ucp'

interface Props {
  checkout: CheckoutData
  onPayment: (method: PaymentMethod) => void
}

function formatPrice(v: number) {
  return v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
}

export default function CheckoutCard({ checkout, onPayment }: Props) {
  const orderId = checkout?.orderId ?? ''
  const total = checkout?.total ?? 0

  return (
    <div className="mt-2 rounded-xl overflow-hidden" style={{ background:'var(--panel)', border:'1px solid var(--border)' }}>
      {/* header */}
      <div className="px-4 py-3" style={{ borderBottom:'1px solid var(--border)' }}>
        <p className="text-sm font-semibold" style={{ color:'var(--text)' }}>📋 Pedido criado!</p>
        <p className="text-[10px] font-mono mt-0.5" style={{ color:'var(--muted)' }}>#{orderId}</p>
      </div>

      {/* total */}
      <div className="px-4 py-3 flex items-center justify-between" style={{ borderBottom:'1px solid var(--border)' }}>
        <span className="text-sm" style={{ color:'var(--muted)' }}>Total</span>
        <span className="text-base font-bold font-mono" style={{ color:'var(--accent)' }}>
          {total > 0 ? formatPrice(total) : '—'}
        </span>
      </div>

      {/* frete — radio buttons */}
      <div className="px-4 py-3" style={{ borderBottom:'1px solid var(--border)' }}>
        <p className="text-xs mb-2" style={{ color:'var(--muted)' }}>🚚 Frete</p>
        <div className="flex flex-col gap-1.5">
          <label className="flex items-center gap-2 cursor-pointer">
            <div className="w-4 h-4 rounded-full border-2 flex items-center justify-center"
              style={{ borderColor:'var(--accent)', background:'var(--accent)' }}>
              <div className="w-1.5 h-1.5 rounded-full bg-white" />
            </div>
            <span className="text-xs" style={{ color:'var(--text)' }}>Grátis — Padrão (5–8 dias)</span>
            <span className="ml-auto text-xs font-bold" style={{ color:'var(--accent)' }}>GRÁTIS</span>
          </label>
        </div>
      </div>

      {/* pagamento */}
      <div className="px-4 py-4 flex flex-col gap-2">
        <p className="text-xs mb-1" style={{ color:'var(--muted)' }}>Como você quer pagar?</p>
        <button onClick={() => onPayment('pix')}
          className="w-full py-3 rounded-xl text-sm font-semibold flex items-center justify-center gap-2 transition-colors"
          style={{ background:'rgba(34,197,94,0.1)', border:'1px solid rgba(34,197,94,0.3)', color:'var(--accent)' }}>
          🟢 Pix — pagamento instantâneo
        </button>
        <button onClick={() => onPayment('credit_card')}
          className="w-full py-3 rounded-xl text-sm font-semibold flex items-center justify-center gap-2 transition-colors"
          style={{ background:'rgba(37,99,235,0.1)', border:'1px solid rgba(37,99,235,0.3)', color:'#60a5fa' }}>
          💳 Cartão de crédito
        </button>
      </div>
    </div>
  )
}
