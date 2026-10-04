'use client'

import { useState } from 'react'
import type { Order, FulfillmentStatus, FulfillmentEvent } from '@/types/ucp'

interface Props {
  order: Order
  compact?: boolean
  onClose?: () => void
}

const FULFILLMENT_FLOW: {
  status: FulfillmentStatus
  label: string
  icon: string
  group: 'payment' | 'fulfillment' | 'logistics' | 'done'
}[] = [
  { status: 'payment_confirmed',  label: 'Pagamento confirmado', icon: '✅', group: 'payment'     },
  { status: 'preparing',          label: 'Separando itens',      icon: '📦', group: 'fulfillment' },
  { status: 'ready_to_ship',      label: 'Pronto para envio',    icon: '🗃️', group: 'fulfillment' },
  { status: 'handed_to_carrier',  label: 'Coletado',             icon: '🚛', group: 'logistics'   },
  { status: 'in_transit',         label: 'Em trânsito',          icon: '🗺️', group: 'logistics'   },
  { status: 'out_for_delivery',   label: 'Saiu p/ entrega',      icon: '🛵', group: 'logistics'   },
  { status: 'delivered',          label: 'Entregue',             icon: '🎉', group: 'done'        },
]

const STATUS_INDEX: Record<FulfillmentStatus, number> = {
  payment_confirmed: 0,
  preparing:         1,
  ready_to_ship:     2,
  handed_to_carrier: 3,
  in_transit:        4,
  out_for_delivery:  5,
  delivered:         6,
  cancelled:         -1,
}

function inferFulfillmentStatus(order: Order): FulfillmentStatus {
  if (order.fulfillmentStatus) return order.fulfillmentStatus
  const map: Partial<Record<string, FulfillmentStatus>> = {
    payment_confirmed: 'payment_confirmed',
    preparing:         'preparing',
    shipped:           'in_transit',
    delivered:         'delivered',
    cancelled:         'cancelled',
  }
  return map[order.status] ?? 'payment_confirmed'
}

function fmt(v: number) {
  return v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
}

function fmtDatetime(iso: string) {
  return new Date(iso).toLocaleString('pt-BR', {
    day: '2-digit', month: '2-digit',
    hour: '2-digit', minute: '2-digit',
  })
}

function BtnClose({ onClose }: { onClose: () => void }) {
  return (
    <button
      onClick={onClose}
      className="w-6 h-6 rounded-full flex items-center justify-center opacity-40 hover:opacity-100 transition-opacity flex-shrink-0"
      style={{ background: 'var(--surface)', color: 'var(--muted)' }}
      title="Fechar"
    >
      <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" className="w-3 h-3">
        <path d="M3 3l10 10M13 3L3 13" />
      </svg>
    </button>
  )
}

// Bloco de rastreio colapsável — reutilizado em entregue e em andamento
function TrackingCollapsible({ history, tracking }: { history: FulfillmentEvent[]; tracking?: string }) {
  const [open, setOpen] = useState(false)
  if (history.length === 0 && !tracking) return null
  return (
    <div style={{ borderBottom: '1px solid var(--border)' }}>
      <button
        onClick={() => setOpen(o => !o)}
        className="w-full px-4 py-2.5 flex items-center justify-between text-[11px] transition-colors"
        style={{ color: 'var(--muted)' }}
      >
        <span className="flex items-center gap-1.5">
          📦 Rastreio da entrega
          {history.length > 0 && (
            <span className="px-1.5 py-0.5 rounded-full text-[9px] font-bold"
              style={{ background: 'var(--surface)', color: 'var(--muted)' }}>
              {history.length}
            </span>
          )}
        </span>
        <span>{open ? '▲' : '▼'}</span>
      </button>

      {open && (
        <div className="px-4 pb-3 flex flex-col gap-2.5"
          style={{ borderTop: '1px solid var(--border)', paddingTop: '12px' }}>
          {tracking && (
            <div className="flex items-center justify-between mb-1">
              <span className="text-[10px]" style={{ color: 'var(--muted)' }}>Código</span>
              <code className="text-[11px] font-mono font-bold" style={{ color: 'var(--accent)' }}>
                {tracking}
              </code>
            </div>
          )}
          {history.length > 0 ? [...history].reverse().map((evt, i) => {
            const step = FULFILLMENT_FLOW.find(s => s.status === evt.status)
            return (
              <div key={i} className="flex gap-3">
                <div className="flex flex-col items-center pt-0.5">
                  <div className="w-5 h-5 rounded-full flex items-center justify-center text-[10px] flex-shrink-0"
                    style={{ background: 'var(--surface)', border: '1px solid var(--border)' }}>
                    {step?.icon ?? '•'}
                  </div>
                  {i < history.length - 1 && (
                    <div className="w-px flex-1 mt-1" style={{ background: 'var(--border)' }} />
                  )}
                </div>
                <div className="pb-2">
                  <p className="text-[11px] font-medium" style={{ color: 'var(--text)' }}>
                    {step?.label ?? evt.status}
                  </p>
                  <p className="text-[10px] mt-0.5" style={{ color: 'var(--muted)' }}>
                    {evt.description}
                  </p>
                  {evt.location && (
                    <p className="text-[9px] mt-0.5" style={{ color: 'var(--muted)' }}>
                      📍 {evt.location}
                    </p>
                  )}
                  {evt.trackingCode && (
                    <p className="text-[9px] mt-0.5 font-mono" style={{ color: 'var(--accent)' }}>
                      🏷️ {evt.trackingCode}
                    </p>
                  )}
                  <p className="text-[9px] mt-0.5" style={{ color: 'var(--border)' }}>
                    {fmtDatetime(evt.occurredAt)}
                  </p>
                </div>
              </div>
            )
          }) : (
            <p className="text-[11px]" style={{ color: 'var(--muted)' }}>
              Histórico não disponível.
            </p>
          )}
        </div>
      )}
    </div>
  )
}

export default function OrderTrackingCard({ order, compact = false, onClose }: Props) {
  const currentStatus = inferFulfillmentStatus(order)
  const currentIndex  = STATUS_INDEX[currentStatus] ?? 0
  const history       = order.fulfillmentHistory ?? []
  const isDelivered   = currentStatus === 'delivered'

  if (order.status === 'cancelled') {
    return (
      <div className="mt-2 bg-red-950/30 border border-red-700/40 rounded-xl p-4">
        <div className="flex items-center justify-between">
          <div>
            <p className="text-sm font-semibold text-red-400">❌ Pedido cancelado</p>
            <p className="text-xs text-zinc-500 font-mono mt-1">#{order.orderId}</p>
          </div>
          {onClose && <BtnClose onClose={onClose} />}
        </div>
      </div>
    )
  }

  if (compact) {
    const step = FULFILLMENT_FLOW[Math.max(0, currentIndex)]
    return (
      <div className="bg-zinc-800 border border-zinc-700 rounded-xl px-4 py-3 flex items-center justify-between gap-3">
        <div>
          <p className="text-xs font-mono text-zinc-400">#{order.orderId.slice(0, 8)}...</p>
          <p className="text-sm text-zinc-200 font-medium mt-0.5">
            {step?.icon} {step?.label}
          </p>
        </div>
        <div className="flex items-center gap-2">
          <span className="text-sm font-bold text-green-400 font-mono">
            {order.total > 0 ? fmt(order.total) : '—'}
          </span>
          {onClose && <BtnClose onClose={onClose} />}
        </div>
      </div>
    )
  }

  return (
    <div className="mt-2 rounded-xl overflow-hidden" style={{ background: 'var(--panel)', border: '1px solid var(--border)' }}>

      {/* Banner pós-entrega */}
      {isDelivered && (
        <div className="px-4 py-3 flex items-center gap-3"
          style={{ background: 'rgba(34,197,94,0.08)', borderBottom: '1px solid rgba(34,197,94,0.2)' }}>
          <span className="text-2xl">🎉</span>
          <div className="flex-1">
            <p className="text-sm font-bold" style={{ color: 'var(--accent)' }}>
              Seu pedido foi entregue!
            </p>
            <p className="text-[11px] mt-0.5" style={{ color: 'var(--muted)' }}>
              Esperamos que você aproveite sua compra.
            </p>
          </div>
          {onClose && <BtnClose onClose={onClose} />}
        </div>
      )}

      {/* Header */}
      <div className="px-4 py-3" style={{ borderBottom: '1px solid var(--border)' }}>
        <div className="flex items-center justify-between">
          <p className="text-sm font-semibold" style={{ color: 'var(--text)' }}>
            📦 Acompanhar pedido
          </p>
          <div className="flex items-center gap-2">
            <span className="text-sm font-bold font-mono" style={{ color: 'var(--accent)' }}>
              {order.total > 0 ? fmt(order.total) : '—'}
            </span>
            {onClose && <BtnClose onClose={onClose} />}
          </div>
        </div>
        <p className="text-[10px] font-mono mt-0.5" style={{ color: 'var(--muted)' }}>
          #{order.orderId}
        </p>
        <p className="text-[10px] mt-0.5" style={{ color: 'var(--muted)' }}>
          Criado em {fmtDatetime(order.createdAt)}
        </p>
      </div>

      {/* Produtos */}
      {order.items && order.items.length > 0 && (
        <div className="px-4 py-3" style={{ borderBottom: '1px solid var(--border)' }}>
          <p className="text-[10px] font-semibold mb-2 uppercase tracking-wide" style={{ color: 'var(--muted)' }}>
            Produtos
          </p>
          {order.items.map((item, i) => (
            <div key={i} className="flex items-center justify-between gap-2 mb-1.5">
              <div className="flex-1 min-w-0">
                <p className="text-[12px] truncate" style={{ color: 'var(--text)' }}>{item.title}</p>
                <p className="text-[10px]" style={{ color: 'var(--muted)' }}>Qtd: {item.quantity}</p>
              </div>
              <span className="text-[12px] font-mono font-semibold flex-shrink-0" style={{ color: 'var(--accent)' }}>
                {fmt(item.price * item.quantity)}
              </span>
            </div>
          ))}
          {/* Valores */}
          <div className="mt-2 pt-2" style={{ borderTop: '1px solid var(--border)' }}>
            {order.shippingCost !== undefined && (
              <div className="flex justify-between text-[11px] mb-1">
                <span style={{ color: 'var(--muted)' }}>Frete</span>
                <span style={{ color: order.isFreeShipping ? 'var(--accent)' : 'var(--text)' }}>
                  {order.isFreeShipping ? 'Grátis' : fmt(order.shippingCost ?? 0)}
                </span>
              </div>
            )}
            <div className="flex justify-between text-[12px] font-bold mt-1">
              <span style={{ color: 'var(--text)' }}>Total</span>
              <span style={{ color: 'var(--accent)' }}>{fmt(order.total)}</span>
            </div>
            {order.paymentMethod && (
              <div className="flex justify-between text-[10px] mt-1.5">
                <span style={{ color: 'var(--muted)' }}>Forma de pagamento</span>
                <span style={{ color: 'var(--text)' }}>
                  {order.paymentMethod === 'pix' ? '🔵 Pix' : '💳 Cartão de crédito'}
                </span>
              </div>
            )}
            {order.paymentDetail && (
              <div className="flex justify-between text-[10px] mt-0.5">
                <span style={{ color: 'var(--muted)' }}>
                  {order.paymentMethod === 'pix' ? 'Chave Pix' : 'Cartão final'}
                </span>
                <span className="font-mono text-[10px] truncate max-w-[160px]" style={{ color: 'var(--muted)' }}>
                  {order.paymentMethod === 'pix'
                    ? order.paymentDetail.slice(0, 40) + (order.paymentDetail.length > 40 ? '…' : '')
                    : `•••• ${order.paymentDetail}`}
                </span>
              </div>
            )}
          </div>
        </div>
      )}

      {/* Endereço de entrega */}
      {order.address && (
        <div className="px-4 py-3" style={{ borderBottom: '1px solid var(--border)' }}>
          <p className="text-[10px] font-semibold mb-1.5 uppercase tracking-wide" style={{ color: 'var(--muted)' }}>
            Endereço de entrega
          </p>
          <p className="text-[12px]" style={{ color: 'var(--text)' }}>
            {order.address.street}, {order.address.number}
            {order.address.complement ? ` — ${order.address.complement}` : ''}
          </p>
          <p className="text-[11px] mt-0.5" style={{ color: 'var(--muted)' }}>
            {order.address.neighborhood} · {order.address.city}/{order.address.state}
          </p>
          <p className="text-[10px] mt-0.5 font-mono" style={{ color: 'var(--muted)' }}>
            CEP {order.address.zipCode}
          </p>
        </div>
      )}

      {/* Timeline em andamento (quando não entregue) */}
      {!isDelivered && (
        <div className="px-4 py-3" style={{ borderBottom: '1px solid var(--border)' }}>
          {FULFILLMENT_FLOW.map((step, i) => {
            const done    = i <= currentIndex
            const current = i === currentIndex
            const isLast  = i === FULFILLMENT_FLOW.length - 1
            const evt = history.find(h => h.status === step.status)

            return (
              <div key={step.status} className="flex gap-3">
                <div className="flex flex-col items-center">
                  <div className={`
                    w-7 h-7 rounded-full flex items-center justify-center text-sm flex-shrink-0
                    transition-all duration-500
                    ${current ? 'shadow-[0_0_10px_rgba(34,197,94,0.4)]' : ''}
                  `}
                  style={{
                    background: current ? 'var(--accent)' : done ? 'rgba(34,197,94,0.2)' : 'var(--surface)',
                    border: `1px solid ${done ? 'rgba(34,197,94,0.5)' : 'var(--border)'}`,
                  }}>
                    {done ? step.icon : <span className="text-[10px]" style={{ color: 'var(--muted)' }}>○</span>}
                  </div>
                  {!isLast && (
                    <div className="w-px h-5 mt-0.5 transition-all duration-500"
                      style={{ background: done && i < currentIndex ? 'rgba(34,197,94,0.4)' : 'var(--border)' }} />
                  )}
                </div>
                <div className="pb-3 pt-1 flex-1">
                  <div className="flex items-center justify-between">
                    <p className="text-xs font-medium transition-colors duration-300"
                      style={{ color: current ? 'var(--accent)' : done ? 'var(--text)' : 'var(--muted)' }}>
                      {step.label}
                      {current && (
                        <span className="ml-1.5 inline-flex items-center gap-0.5 text-[9px]" style={{ color: 'var(--accent)' }}>
                          <span className="w-1 h-1 rounded-full animate-pulse inline-block" style={{ background: 'var(--accent)' }} />
                          agora
                        </span>
                      )}
                    </p>
                    {evt && (
                      <span className="text-[9px]" style={{ color: 'var(--muted)' }}>
                        {fmtDatetime(evt.occurredAt)}
                      </span>
                    )}
                  </div>
                  {evt?.location && (
                    <p className="text-[9px] mt-0.5" style={{ color: 'var(--muted)' }}>
                      📍 {evt.location}
                    </p>
                  )}
                </div>
              </div>
            )
          })}
        </div>
      )}

      {/* Rastreio colapsável — entregue: histórico completo / em andamento: histórico parcial */}
      <TrackingCollapsible history={history} tracking={order.tracking} />

    </div>
  )
}
