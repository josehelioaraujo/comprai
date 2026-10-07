'use client'

import { useEffect, useState } from 'react'
import { useRouter } from 'next/navigation'
import { getOrders, addToCart } from '@/lib/api'
import { getSessionId } from '@/lib/session'
import type { Order } from '@/types/ucp'
import { generateUuid } from '@/lib/session'

const STATUS_LABEL: Record<string, string> = {
  created:           'Criado',
  payment_pending:   'Aguardando pagamento',
  payment_confirmed: 'Pagamento confirmado',
  preparing:         'Preparando',
  shipped:           'Enviado',
  delivered:         'Entregue',
  cancelled:         'Cancelado',
}

const STATUS_COLOR: Record<string, string> = {
  created:           '#71717a',
  payment_pending:   '#f59e0b',
  payment_confirmed: '#22c55e',
  preparing:         '#3b82f6',
  shipped:           '#8b5cf6',
  delivered:         '#22c55e',
  cancelled:         '#ef4444',
}

export default function ProfilePage() {
  const [orders,  setOrders]  = useState<Order[]>([])
  const [loading, setLoading] = useState(true)
  const [reordering, setReordering] = useState<string | null>(null)
  const router = useRouter()

  useEffect(() => {
    const sessionId = getSessionId()
    getOrders(sessionId).then(o => { setOrders(o); setLoading(false) })
  }, [])

  async function handleReorder(order: Order) {
    setReordering(order.orderId)
    const sessionId = getSessionId()
    try {
      for (const item of order.items) {
        await addToCart(sessionId, item, generateUuid())
      }
      router.push('/chat')
    } catch {
      setReordering(null)
    }
  }

  return (
    <div>
      <h1 style={{ fontSize: 22, fontWeight: 700, marginBottom: '1.5rem', color: '#f4f4f5' }}>Meus Pedidos</h1>

      {loading && (
        <div style={{ color: '#a1a1aa', fontSize: 14 }}>Carregando pedidos...</div>
      )}

      {!loading && orders.length === 0 && (
        <div style={{ background: '#18181b', border: '1px solid #3f3f46', borderRadius: 12, padding: '2rem', textAlign: 'center', color: '#71717a' }}>
          Nenhum pedido encontrado.
        </div>
      )}

      <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
        {orders.map(order => (
          <div key={order.orderId} style={{
            background: '#18181b', border: '1px solid #3f3f46', borderRadius: 12, padding: '1.25rem',
          }}>
            {/* Header do pedido */}
            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '0.75rem' }}>
              <div>
                <div style={{ fontFamily: 'monospace', fontSize: 12, color: '#71717a' }}>#{order.orderId.slice(0, 8).toUpperCase()}</div>
                <div style={{ fontWeight: 600, fontSize: 15, marginTop: 2 }}>
                  R$ {(order.total ?? 0).toFixed(2).replace('.', ',')}
                </div>
              </div>
              <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
                <span style={{
                  padding: '3px 10px', borderRadius: 20, fontSize: 12, fontWeight: 600,
                  background: (STATUS_COLOR[order.status] ?? '#71717a') + '22',
                  color: STATUS_COLOR[order.status] ?? '#71717a',
                }}>
                  {STATUS_LABEL[order.status] ?? order.status}
                </span>
                {order.items?.length > 0 && (
                  <button
                    onClick={() => handleReorder(order)}
                    disabled={reordering === order.orderId}
                    style={{
                      padding: '4px 12px', borderRadius: 6, border: '1px solid #f97316',
                      background: 'transparent', color: '#f97316', fontSize: 12,
                      fontWeight: 600, cursor: 'pointer',
                    }}
                  >
                    {reordering === order.orderId ? '...' : '↩ Repetir'}
                  </button>
                )}
              </div>
            </div>

            {/* Itens */}
            {order.items?.slice(0, 3).map((item, i) => (
              <div key={i} style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', padding: '4px 0', borderTop: i === 0 ? '1px solid #3f3f46' : undefined }}>
                <span style={{ color: '#71717a', fontSize: 12 }}>×{item.quantity}</span>
                <span style={{ fontSize: 13, flex: 1, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{item.name ?? item.title}</span>
                <span style={{ fontSize: 12, color: '#a1a1aa' }}>R$ {((item.price ?? 0) * (item.quantity ?? 1)).toFixed(2).replace('.', ',')}</span>
              </div>
            ))}
            {(order.items?.length ?? 0) > 3 && (
              <div style={{ fontSize: 12, color: '#71717a', marginTop: 4 }}>
                +{order.items.length - 3} item(s)
              </div>
            )}
          </div>
        ))}
      </div>
    </div>
  )
}
