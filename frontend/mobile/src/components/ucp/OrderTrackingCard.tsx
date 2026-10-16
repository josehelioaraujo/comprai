'use client'

import { useState } from 'react'
import type { Order, FulfillmentStatus, FulfillmentEvent } from '@/types/ucp'

interface Props {
  order: Order
  compact?: boolean
}

// ── Mapeamento de status do fulfillment ───────────────────────────────────────

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

// Mapeia orderStatus → fulfillmentStatus quando não há fulfillmentStatus
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

// ── Componente principal ──────────────────────────────────────────────────────

export default function OrderTrackingCard({ order, compact = false }: Props) {
  const [showHistory, setShowHistory] = useState(false)

  const currentStatus = inferFulfillmentStatus(order)
  const currentIndex  = STATUS_INDEX[currentStatus] ?? 0
  const history       = order.fulfillmentHistory ?? []

  if (order.status === 'cancelled') {
    return (
      <div className="mt-2 bg-red-950/30 border border-red-700/40 rounded-xl p-4">
        <p className="text-sm font-semibold text-red-400">❌ Pedido cancelado</p>
        <p className="text-xs text-zinc-500 font-mono mt-1">#{order.orderId}</p>
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
        <span className="text-sm font-bold text-green-400 font-mono">
          {order.total > 0 ? fmt(order.total) : '—'}
        </span>
      </div>
    )
  }

  return (
    <div className="mt-2 rounded-xl overflow-hidden" style={{ background: 'var(--panel)', border: '1px solid var(--border)' }}>

      {/* ── Header ── */}
      <div className="px-4 py-3" style={{ borderBottom: '1px solid var(--border)' }}>
        <div className="flex items-center justify-between">
          <p className="text-sm font-semibold" style={{ color: 'var(--text)' }}>
            📦 Acompanhar pedido
          </p>
          <span className="text-sm font-bold font-mono" style={{ color: 'var(--accent)' }}>
            {order.total > 0 ? fmt(order.total) : '—'}
          </span>
        </div>
        <p className="text-[10px] font-mono mt-0.5" style={{ color: 'var(--muted)' }}>
          #{order.orderId}
        </p>
        <p className="text-[10px] mt-0.5" style={{ color: 'var(--muted)' }}>
          Criado em {fmtDatetime(order.createdAt)}
        </p>
      </div>

      {/* ── Timeline de fulfillment ── */}
      <div className="px-4 py-3" style={{ borderBottom: '1px solid var(--border)' }}>
        {FULFILLMENT_FLOW.map((step, i) => {
          const done    = i <= currentIndex
          const current = i === currentIndex
          const isLast  = i === FULFILLMENT_FLOW.length - 1
          // Busca evento correspondente no histórico
          const evt = history.find(h => h.status === step.status)

          return (
            <div key={step.status} className="flex gap-3">
              {/* Coluna esquerda: ícone + linha */}
              <div className="flex flex-col items-center">
                <div className={`
                  w-7 h-7 rounded-full flex items-center justify-center text-sm flex-shrink-0
                  transition-all duration-500
                  ${current
                    ? 'shadow-[0_0_10px_rgba(34,197,94,0.4)]'
                    : ''}
                `}
                style={{
                  background: current
                    ? 'var(--accent)'
                    : done
                      ? 'rgba(34,197,94,0.2)'
                      : 'var(--surface)',
                  border: `1px solid ${done ? 'rgba(34,197,94,0.5)' : 'var(--border)'}`,
                }}>
                  {done ? step.icon : <span className="text-[10px]" style={{ color: 'var(--muted)' }}>○</span>}
                </div>
                {!isLast && (
                  <div className="w-px h-5 mt-0.5 transition-all duration-500"
                    style={{ background: done && i < currentIndex ? 'rgba(34,197,94,0.4)' : 'var(--border)' }} />
                )}
              </div>

              {/* Coluna direita: label + timestamp */}
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

      {/* ── Código de rastreio ── */}
      {order.tracking && (
        <div className="px-4 py-2.5 flex items-center justify-between"
          style={{ borderBottom: '1px solid var(--border)', background: 'rgba(34,197,94,0.04)' }}>
          <span className="text-[10px]" style={{ color: 'var(--muted)' }}>Rastreio</span>
          <code className="text-[11px] font-mono font-bold" style={{ color: 'var(--accent)' }}>
            {order.tracking}
          </code>
        </div>
      )}

      {/* ── Botão histórico ── */}
      {history.length > 0 && (
        <div>
          <button
            onClick={() => setShowHistory(h => !h)}
            className="w-full px-4 py-2.5 flex items-center justify-between text-[11px] transition-colors"
            style={{
              color: 'var(--muted)',
              borderBottom: showHistory ? '1px solid var(--border)' : 'none',
            }}
          >
            <span className="flex items-center gap-1.5">
              🕐 Histórico de entrega
              <span className="px-1.5 py-0.5 rounded-full text-[9px] font-bold"
                style={{ background: 'var(--surface)', color: 'var(--muted)' }}>
                {history.length}
              </span>
            </span>
            <span>{showHistory ? '▲' : '▼'}</span>
          </button>

          {/* ── Painel de histórico ── */}
          {showHistory && (
            <div className="px-4 py-3 flex flex-col gap-2.5">
              <p className="text-[10px] font-semibold mb-1" style={{ color: 'var(--muted)' }}>
                HISTÓRICO COMPLETO DE FULFILLMENT
              </p>
              {[...history].reverse().map((evt, i) => {
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
              })}
            </div>
          )}
        </div>
      )}
    </div>
  )
}
