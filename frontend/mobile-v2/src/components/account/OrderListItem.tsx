import Link from 'next/link'
import type { OrderSummary } from '@/types'

interface Props {
  order: OrderSummary
}

const STATUS_STYLE: Record<string, string> = {
  pending:    'bg-amber-50 text-amber-700',
  confirmed:  'bg-blue-50 text-blue-700',
  shipped:    'bg-indigo-50 text-indigo-700',
  delivered:  'bg-green-50 text-green-700',
  cancelled:  'bg-red-50 text-red-600',
}

const STATUS_LABEL: Record<string, string> = {
  pending:   'Pendente',
  confirmed: 'Confirmado',
  shipped:   'Enviado',
  delivered: 'Entregue',
  cancelled: 'Cancelado',
}

const fmt = (n: number) =>
  new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(n)

export default function OrderListItem({ order }: Props) {
  const statusStyle = STATUS_STYLE[order.status.toLowerCase()] ?? 'bg-surface text-muted'
  const statusLabel = STATUS_LABEL[order.status.toLowerCase()] ?? order.status

  return (
    <Link href={`/orders/${order.orderId}`}
      className="block bg-white rounded-2xl shadow-card p-4 active:bg-surface transition-colors">
      <div className="flex items-start justify-between gap-2">
        <div className="flex-1 min-w-0">
          <p className="text-xs text-muted">Order #{order.orderId.slice(-8).toUpperCase()}</p>
          <p className="text-sm font-semibold text-ink mt-0.5">
            {order.itemCount} iten{order.itemCount > 1 ? 's' : ''}
            {order.shippingCity && ` · ${order.shippingCity}, ${order.shippingState}`}
          </p>
          <p className="text-xs text-muted mt-0.5">
            {new Date(order.createdAt).toLocaleDateString('pt-BR', {
              year: 'numeric', month: 'short', day: 'numeric',
            })}
          </p>
        </div>
        <div className="flex flex-col items-end gap-1.5 shrink-0">
          <span className={['text-xs font-semibold px-2 py-0.5 rounded-lg', statusStyle].join(' ')}>
            {statusLabel}
          </span>
          <p className="text-base font-bold text-ink">{fmt(order.total)}</p>
        </div>
      </div>

      {order.tracking && (
        <div className="mt-2 flex items-center gap-1.5 text-xs text-brand font-medium">
          <svg width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round"
              d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z"/>
            <path strokeLinecap="round" strokeLinejoin="round" d="M15 11a3 3 0 11-6 0 3 3 0 016 0z"/>
          </svg>
          Rastrear: {order.tracking}
        </div>
      )}
    </Link>
  )
}
