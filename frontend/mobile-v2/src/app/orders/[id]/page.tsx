import { auth } from '@/lib/auth'
import { notFound } from 'next/navigation'
import { apiGetFulfillment, apiGetOrder } from '@/lib/api'
import AppShell from '@/components/layout/AppShell'
import TrackingTimeline from '@/components/tracking/TrackingTimeline'
import Link from 'next/link'

interface Props {
  params: Promise<{ id: string }>
}

const fmt = (n: number) =>
  new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(n)

export default async function OrderDetailPage({ params }: Props) {
  const { id } = await params
  const session = await auth()
  const token   = session?.user?.apiToken

  const [order, events] = await Promise.all([
    apiGetOrder(id, token),
    apiGetFulfillment(id, token),
  ])

  if (!order) notFound()

  return (
    <AppShell hideNav>
      <div className="px-4 pt-12 pb-3 bg-white border-b border-border flex items-center gap-3">
        <Link href="/orders" className="text-muted hover:text-ink transition-colors">
          <svg width="22" height="22" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" d="M15 19l-7-7 7-7"/>
          </svg>
        </Link>
        <div>
          <h1 className="text-lg font-bold text-ink leading-none">Detalhes do Pedido</h1>
          <p className="text-xs text-muted">#{order.orderId.slice(-8).toUpperCase()}</p>
        </div>
      </div>

      <div className="p-4 space-y-4">
        {/* order summary card */}
        <div className="bg-white rounded-2xl shadow-card p-4 space-y-2">
          <div className="flex justify-between text-sm">
            <span className="text-muted">Status</span>
            <span className="font-semibold text-ink capitalize">{order.status}</span>
          </div>
          <div className="flex justify-between text-sm">
            <span className="text-muted">Itens</span>
            <span className="font-medium text-ink">{order.itemCount}</span>
          </div>
          <div className="flex justify-between text-sm">
            <span className="text-muted">Total</span>
            <span className="font-bold text-ink">{fmt(order.total)}</span>
          </div>
          {order.paymentMethod && (
            <div className="flex justify-between text-sm">
              <span className="text-muted">Pagamento</span>
              <span className="font-medium text-ink capitalize">{order.paymentMethod}</span>
            </div>
          )}
          {(order.shippingCity || order.shippingState) && (
            <div className="flex justify-between text-sm">
              <span className="text-muted">Entregar em</span>
              <span className="font-medium text-ink">
                {[order.shippingCity, order.shippingState].filter(Boolean).join(', ')}
              </span>
            </div>
          )}
          {order.tracking && (
            <div className="flex justify-between text-sm">
              <span className="text-muted">Rastreio</span>
              <span className="font-mono font-medium text-brand text-xs">{order.tracking}</span>
            </div>
          )}
          <div className="flex justify-between text-sm">
            <span className="text-muted">Data</span>
            <span className="text-ink">
              {new Date(order.createdAt).toLocaleDateString('pt-BR', {
                year: 'numeric', month: 'short', day: 'numeric',
              })}
            </span>
          </div>
        </div>

        {/* tracking timeline */}
        <div className="bg-white rounded-2xl shadow-card p-4">
          <h3 className="text-sm font-semibold text-ink mb-4">Histórico de Rastreio</h3>
          <TrackingTimeline events={events} />
        </div>
      </div>
    </AppShell>
  )
}
