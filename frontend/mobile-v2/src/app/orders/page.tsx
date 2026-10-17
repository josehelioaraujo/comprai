import { auth } from '@/lib/auth'
import { redirect } from 'next/navigation'
import { apiGetOrders } from '@/lib/api'
import AppShell from '@/components/layout/AppShell'
import OrderListItem from '@/components/account/OrderListItem'
import Link from 'next/link'

export default async function OrdersPage() {
  const session = await auth()
  if (!session) redirect('/auth/login')

  let orders: import('@/types').OrderSummary[] = []
  try {
    orders = await apiGetOrders(undefined, session.user.apiToken)
  } catch { /* empty */ }

  return (
    <AppShell>
      <div className="px-4 pt-12 pb-3 bg-white border-b border-border flex items-center gap-3">
        <Link href="/account" className="text-muted hover:text-ink transition-colors">
          <svg width="22" height="22" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" d="M15 19l-7-7 7-7"/>
          </svg>
        </Link>
        <h1 className="text-xl font-bold text-ink">Meus Pedidos</h1>
      </div>

      {orders.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-20 px-8 text-center">
          <svg width="56" height="56" fill="none" stroke="currentColor" strokeWidth="1.2"
            className="text-brand/30 mb-4" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round"
              d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2"/>
          </svg>
          <p className="text-sm font-medium text-ink">Nenhum pedido ainda</p>
          <p className="text-xs text-muted mt-1">Conclua sua primeira compra para vê-la aqui</p>
        </div>
      ) : (
        <div className="p-4 space-y-3">
          {orders.map(order => (
            <OrderListItem key={order.orderId} order={order} />
          ))}
        </div>
      )}
    </AppShell>
  )
}
