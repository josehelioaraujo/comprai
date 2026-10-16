'use client'

import { useState } from 'react'
import type { Order, FulfillmentStatus } from '@/types/ucp'
import OrderTrackingCard from './OrderTrackingCard'

interface Props {
  orders: Order[]
}

const STATUS_LABEL: Partial<Record<string, string>> = {
  payment_confirmed: 'Confirmado',
  preparing:         'Preparando',
  ready_to_ship:     'Pronto p/ envio',
  handed_to_carrier: 'Coletado',
  in_transit:        'Em trânsito',
  out_for_delivery:  'Saiu p/ entrega',
  delivered:         'Entregue',
  cancelled:         'Cancelado',
  payment_pending:   'Aguardando pag.',
  shipped:           'Enviado',
}

const STATUS_COLOR: Partial<Record<string, string>> = {
  delivered:         'text-emerald-400',
  out_for_delivery:  'text-emerald-300',
  in_transit:        'text-blue-400',
  handed_to_carrier: 'text-blue-300',
  preparing:         'text-amber-400',
  ready_to_ship:     'text-amber-300',
  payment_confirmed: 'text-zinc-300',
  cancelled:         'text-red-400',
  payment_pending:   'text-zinc-500',
}

function fmt(v: number) {
  return v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
}

function fmtDate(iso: string) {
  return new Date(iso).toLocaleString('pt-BR', {
    day: '2-digit', month: '2-digit',
    hour: '2-digit', minute: '2-digit',
  })
}

function getDisplayStatus(order: Order): string {
  return order.fulfillmentStatus ?? order.status
}

export default function OrderListCard({ orders }: Props) {
  const [selected, setSelected] = useState<string | null>(null)

  if (orders.length === 0) {
    return (
      <div className="mt-2 rounded-xl bg-zinc-900 border border-zinc-800 px-4 py-6 text-center">
        <p className="text-zinc-500 text-sm">Nenhum pedido encontrado.</p>
      </div>
    )
  }

  const selectedOrder = orders.find(o => o.orderId === selected)

  return (
    <div className="mt-2 flex flex-col gap-2">
      {/* Cabeçalho */}
      <div className="flex items-center justify-between px-1">
        <p className="text-xs font-semibold text-zinc-400">
          📋 Meus pedidos
        </p>
        <span className="text-[10px] bg-zinc-800 text-zinc-400 px-2 py-0.5 rounded-full font-mono">
          {orders.length} {orders.length === 1 ? 'pedido' : 'pedidos'}
        </span>
      </div>

      {/* Detalhe inline (se selecionado) */}
      {selectedOrder && (
        <div className="rounded-xl overflow-hidden border border-emerald-800/40">
          <button
            onClick={() => setSelected(null)}
            className="w-full flex items-center gap-2 px-3 py-2 bg-zinc-900 text-zinc-400 text-[10px] hover:text-zinc-200 transition-colors border-b border-zinc-800"
          >
            ← Voltar à lista
          </button>
          <OrderTrackingCard order={selectedOrder} />
        </div>
      )}

      {/* Lista de pedidos */}
      {orders.map(order => {
        const displayStatus = getDisplayStatus(order)
        const colorClass = STATUS_COLOR[displayStatus] ?? 'text-zinc-400'
        const isSelected = order.orderId === selected

        return (
          <button
            key={order.orderId}
            onClick={() => setSelected(isSelected ? null : order.orderId)}
            className={`w-full text-left rounded-xl border transition-all duration-150 active:scale-[0.98]
              ${isSelected
                ? 'border-emerald-700/60 bg-zinc-900'
                : 'border-zinc-800 bg-zinc-900/80 hover:border-zinc-700'
              }`}
          >
            <div className="px-4 py-3 flex items-center justify-between gap-3">
              {/* Lado esquerdo */}
              <div className="min-w-0">
                <p className="text-[11px] font-mono text-zinc-400 truncate">
                  #{order.orderId}
                </p>
                <p className={`text-xs font-semibold mt-0.5 ${colorClass}`}>
                  {STATUS_LABEL[displayStatus] ?? displayStatus}
                </p>
                <p className="text-[10px] text-zinc-600 mt-0.5">
                  {fmtDate(order.createdAt)}
                </p>
              </div>

              {/* Lado direito */}
              <div className="flex flex-col items-end gap-1 shrink-0">
                <span className="text-sm font-bold font-mono text-zinc-200">
                  {order.total > 0 ? fmt(order.total) : '—'}
                </span>
                <span className="text-[9px] text-zinc-600">
                  {isSelected ? '▲ fechar' : '▼ detalhes'}
                </span>
              </div>
            </div>
          </button>
        )
      })}
    </div>
  )
}
