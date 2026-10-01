'use client'

import { useState } from 'react'
import type { CheckoutData, PaymentMethod } from '@/types/ucp'

interface Props {
  checkout: CheckoutData
  onPayment: (method: PaymentMethod) => void
}

function fmt(v: number) {
  return v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
}

export default function CheckoutCard({ checkout, onPayment }: Props) {
  const [dismissed, setDismissed]   = useState(false)
  const [paying, setPaying]         = useState(false)
  const [paidMethod, setPaidMethod] = useState<PaymentMethod | null>(null)

  const orderId      = checkout?.orderId ?? ''
  const total        = checkout?.total ?? 0
  const isFree       = checkout?.isFreeShipping ?? false
  const shippingCost = checkout?.shippingCost ?? 0

  if (dismissed) return null

  function handlePayment(method: PaymentMethod) {
    if (paying || paidMethod) return   // bloqueia duplo clique
    setPaying(true)
    setPaidMethod(method)
    onPayment(method)
  }

  return (
    <div className="mt-2 rounded-xl overflow-hidden relative" style={{ background: 'var(--panel)', border: '1px solid var(--border)' }}>

      {/* ── Header com X ── */}
      <div className="px-4 py-3 flex items-start justify-between" style={{ borderBottom: '1px solid var(--border)' }}>
        <div>
          <p className="text-sm font-semibold" style={{ color: 'var(--text)' }}>📋 Pedido criado!</p>
          <p className="text-[10px] font-mono mt-0.5" style={{ color: 'var(--muted)' }}>#{orderId}</p>
        </div>
        <button
          onClick={() => setDismissed(true)}
          className="w-6 h-6 rounded-full flex items-center justify-center opacity-40 hover:opacity-100 transition-opacity flex-shrink-0 mt-0.5"
          style={{ background: 'var(--surface)', color: 'var(--muted)' }}
          title="Fechar"
        >
          <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" className="w-3 h-3">
            <path d="M3 3l10 10M13 3L3 13" />
          </svg>
        </button>
      </div>

      {/* ── Total ── */}
      <div className="px-4 py-3 flex items-center justify-between" style={{ borderBottom: '1px solid var(--border)' }}>
        <span className="text-sm" style={{ color: 'var(--muted)' }}>Total</span>
        <span className="text-base font-bold font-mono" style={{ color: 'var(--accent)' }}>
          {total > 0 ? fmt(total) : '—'}
        </span>
      </div>

      {/* ── Frete ── */}
      <div className="px-4 py-3" style={{ borderBottom: '1px solid var(--border)' }}>
        <p className="text-xs mb-1.5" style={{ color: 'var(--muted)' }}>🚚 Frete</p>
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <div className="w-3.5 h-3.5 rounded-full border-2 flex items-center justify-center flex-shrink-0"
              style={{ borderColor: 'var(--accent)', background: 'var(--accent)' }}>
              <div className="w-1 h-1 rounded-full bg-white" />
            </div>
            <span className="text-xs" style={{ color: 'var(--text)' }}>
              {isFree ? 'Grátis — Padrão (5–8 dias)' : `Padrão (5–8 dias) — ${fmt(shippingCost)}`}
            </span>
          </div>
          {isFree && (
            <span className="text-xs font-bold" style={{ color: 'var(--accent)' }}>GRÁTIS</span>
          )}
        </div>
      </div>

      {/* ── Pagamento ── */}
      <div className="px-4 py-4 flex flex-col gap-2">
        <p className="text-xs mb-1" style={{ color: 'var(--muted)' }}>
          {paidMethod ? '✅ Pagamento iniciado' : 'Como você quer pagar?'}
        </p>

        <button
          onClick={() => handlePayment('pix')}
          disabled={paying}
          className="w-full py-3 rounded-xl text-sm font-semibold flex items-center justify-center gap-2 transition-all"
          style={{
            background:  paidMethod === 'pix'   ? 'rgba(34,197,94,0.25)' : 'rgba(34,197,94,0.1)',
            border:      `1px solid ${paidMethod === 'pix' ? 'rgba(34,197,94,0.6)' : 'rgba(34,197,94,0.3)'}`,
            color:       'var(--accent)',
            opacity:     paying && paidMethod !== 'pix' ? 0.35 : 1,
            cursor:      paying ? 'not-allowed' : 'pointer',
          }}
        >
          {paidMethod === 'pix' ? '⏳ Processando Pix...' : '🟢 Pix — pagamento instantâneo'}
        </button>

        <button
          onClick={() => handlePayment('credit_card')}
          disabled={paying}
          className="w-full py-3 rounded-xl text-sm font-semibold flex items-center justify-center gap-2 transition-all"
          style={{
            background:  paidMethod === 'credit_card' ? 'rgba(37,99,235,0.25)' : 'rgba(37,99,235,0.1)',
            border:      `1px solid ${paidMethod === 'credit_card' ? 'rgba(37,99,235,0.6)' : 'rgba(37,99,235,0.3)'}`,
            color:       '#60a5fa',
            opacity:     paying && paidMethod !== 'credit_card' ? 0.35 : 1,
            cursor:      paying ? 'not-allowed' : 'pointer',
          }}
        >
          {paidMethod === 'credit_card' ? '⏳ Processando cartão...' : '💳 Cartão de crédito'}
        </button>
      </div>
    </div>
  )
}
