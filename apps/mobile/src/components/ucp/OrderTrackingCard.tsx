'use client'

import type { Order, OrderStatus, OrderEvent } from '@/types/ucp'

interface Props {
  order: Order
  compact?: boolean
}

const STATUS_FLOW: { status: OrderStatus; label: string; icon: string }[] = [
  { status: 'created',           label: 'Pedido criado',         icon: '📝' },
  { status: 'payment_pending',   label: 'Aguardando pagamento',  icon: '⏳' },
  { status: 'payment_confirmed', label: 'Pagamento confirmado',  icon: '✅' },
  { status: 'preparing',         label: 'Preparando pedido',     icon: '📦' },
  { status: 'shipped',           label: 'Enviado',               icon: '🚚' },
  { status: 'delivered',         label: 'Entregue',              icon: '🎉' },
]

const STATUS_INDEX: Record<OrderStatus, number> = {
  created: 0,
  payment_pending: 1,
  payment_confirmed: 2,
  preparing: 3,
  shipped: 4,
  delivered: 5,
  cancelled: -1,
}

function formatPrice(v: number) {
  return v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
}

function formatDate(iso: string) {
  return new Date(iso).toLocaleString('pt-BR', {
    day: '2-digit', month: '2-digit', year: 'numeric',
    hour: '2-digit', minute: '2-digit',
  })
}

export default function OrderTrackingCard({ order, compact = false }: Props) {
  const currentIndex = STATUS_INDEX[order.status] ?? 0

  if (order.status === 'cancelled') {
    return (
      <div className="mt-2 bg-red-950/30 border border-red-700/40 rounded-xl p-4">
        <p className="text-sm font-semibold text-red-400">❌ Pedido cancelado</p>
        <p className="text-xs text-zinc-500 font-mono mt-1">#{order.orderId}</p>
      </div>
    )
  }

  if (compact) {
    return (
      <div className="bg-zinc-800 border border-zinc-700 rounded-xl px-4 py-3 flex items-center justify-between gap-3">
        <div>
          <p className="text-xs font-mono text-zinc-400">#{order.orderId.slice(0, 8)}...</p>
          <p className="text-sm text-zinc-200 font-medium mt-0.5">
            {STATUS_FLOW[Math.max(0, currentIndex)]?.icon}{' '}
            {STATUS_FLOW[Math.max(0, currentIndex)]?.label}
          </p>
        </div>
        <span className="text-sm font-bold text-green-400 font-mono">{formatPrice(order.total)}</span>
      </div>
    )
  }

  return (
    <div className="mt-2 bg-zinc-800 border border-zinc-700 rounded-xl overflow-hidden">
      {/* header */}
      <div className="px-4 py-3 border-b border-zinc-700">
        <div className="flex items-center justify-between">
          <p className="text-sm font-semibold text-zinc-200">📦 Acompanhar pedido</p>
          <span className="text-sm font-bold text-green-400 font-mono">{formatPrice(order.total)}</span>
        </div>
        <p className="text-[10px] text-zinc-500 font-mono mt-0.5">#{order.orderId}</p>
        <p className="text-[10px] text-zinc-600 mt-0.5">Criado em {formatDate(order.createdAt)}</p>
      </div>

      {/* timeline */}
      <div className="px-4 py-4 flex flex-col gap-0">
        {STATUS_FLOW.map((step, i) => {
          const done = i <= currentIndex
          const current = i === currentIndex
          const isLast = i === STATUS_FLOW.length - 1

          return (
            <div key={step.status} className="flex gap-3">
              {/* coluna esquerda: ícone + linha */}
              <div className="flex flex-col items-center">
                <div
                  className={`w-8 h-8 rounded-full flex items-center justify-center text-sm flex-shrink-0
                    ${current ? 'bg-green-600 ring-2 ring-green-400/40' : done ? 'bg-green-900/60' : 'bg-zinc-700'}
                  `}
                >
                  {done ? step.icon : '○'}
                </div>
                {!isLast && (
                  <div className={`w-0.5 h-6 mt-0.5 ${done && i < currentIndex ? 'bg-green-700' : 'bg-zinc-700'}`} />
                )}
              </div>

              {/* coluna direita: label */}
              <div className="pb-4 pt-1.5">
                <p className={`text-sm ${current ? 'text-green-400 font-semibold' : done ? 'text-zinc-300' : 'text-zinc-600'}`}>
                  {step.label}
                </p>
                {current && (
                  <p className="text-[10px] text-zinc-500 mt-0.5 flex items-center gap-1">
                    <span className="w-1.5 h-1.5 rounded-full bg-green-500 animate-pulse inline-block" />
                    Status atual
                  </p>
                )}
              </div>
            </div>
          )
        })}
      </div>

      {/* rastreio */}
      {order.tracking && (
        <div className="px-4 pb-4 border-t border-zinc-700 pt-3">
          <p className="text-[10px] text-zinc-500 mb-1">Código de rastreio</p>
          <code className="text-xs font-mono text-blue-400">{order.tracking}</code>
        </div>
      )}
    </div>
  )
}
