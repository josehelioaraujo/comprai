'use client'

import { useEffect, useState, useCallback } from 'react'
import {
  AreaChart, Area, BarChart, Bar,
  XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer
} from 'recharts'
import type {
  AdminSummary, AdminOrder, AdminSalesChartPoint,
  AdminTopProduct, AdminOrderDetail
} from '@/types'
import { formatCurrency, formatDate, STATUS_LABELS, STATUS_COLORS } from '@/lib/api'
import { fetchProxy } from '@/lib/fetch-proxy'

// ── fetch helpers ────────────────────────────────────────────────────────────

const fetchAdmin = fetchProxy

// ── KPI Card ─────────────────────────────────────────────────────────────────

function KpiCard({
  label, value, sub, color = 'purple',
}: {
  label: string
  value: string
  sub?: string
  color?: 'purple' | 'green' | 'blue' | 'red'
}) {
  const colors = {
    purple: 'from-purple-900/40 to-purple-800/10 border-purple-700/30',
    green:  'from-green-900/40  to-green-800/10  border-green-700/30',
    blue:   'from-blue-900/40   to-blue-800/10   border-blue-700/30',
    red:    'from-red-900/40    to-red-800/10    border-red-700/30',
  }
  return (
    <div className={`rounded-2xl border bg-gradient-to-br ${colors[color]} p-5`}>
      <p className="text-xs font-medium text-gray-400 uppercase tracking-wider mb-2">{label}</p>
      <p className="text-2xl font-bold text-white">{value}</p>
      {sub && <p className="text-xs text-gray-500 mt-1">{sub}</p>}
    </div>
  )
}

// ── Status Badge ──────────────────────────────────────────────────────────────

function StatusBadge({ status }: { status: string }) {
  return (
    <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${STATUS_COLORS[status] ?? 'bg-gray-500/20 text-gray-300'}`}>
      {STATUS_LABELS[status] ?? status}
    </span>
  )
}

// ── Order Modal ───────────────────────────────────────────────────────────────

function OrderModal({ orderId, onClose }: { orderId: string; onClose: () => void }) {
  const [detail, setDetail] = useState<AdminOrderDetail | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    fetchAdmin<AdminOrderDetail>(`/api/admin/orders/${orderId}`)
      .then(setDetail)
      .catch(console.error)
      .finally(() => setLoading(false))
  }, [orderId])

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70">
      <div className="w-full max-w-2xl max-h-[90vh] overflow-y-auto rounded-2xl bg-gray-900 border border-gray-700 shadow-2xl">
        <div className="flex items-center justify-between px-6 py-4 border-b border-gray-800">
          <h2 className="text-lg font-semibold text-white">Detalhe do Pedido</h2>
          <button onClick={onClose} className="text-gray-400 hover:text-white transition">
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        <div className="p-6">
          {loading && <p className="text-gray-400 text-center py-8">Carregando…</p>}
          {!loading && !detail && <p className="text-red-400 text-center py-8">Pedido não encontrado.</p>}
          {detail && (
            <div className="space-y-5">
              {/* Header */}
              <div className="flex items-start justify-between">
                <div>
                  <p className="text-xs text-gray-500">ID</p>
                  <p className="text-sm text-gray-300 font-mono">{detail.orderId.slice(0, 8)}…</p>
                </div>
                <StatusBadge status={detail.status} />
              </div>

              {/* Cliente */}
              <div className="rounded-xl bg-gray-800 p-4">
                <p className="text-xs text-gray-500 mb-2 uppercase tracking-wider">Cliente</p>
                <p className="text-sm text-white font-medium">{detail.customerName ?? '—'}</p>
                <p className="text-sm text-gray-400">{detail.customerEmail ?? '—'}</p>
                {detail.customerPhone && <p className="text-sm text-gray-400">{detail.customerPhone}</p>}
              </div>

              {/* Pagamento */}
              <div className="rounded-xl bg-gray-800 p-4">
                <p className="text-xs text-gray-500 mb-2 uppercase tracking-wider">Pagamento</p>
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm text-white">{detail.paymentMethod ?? '—'}</p>
                    {detail.paymentStatus && <StatusBadge status={detail.paymentStatus} />}
                  </div>
                  <p className="text-lg font-bold text-white">{formatCurrency(detail.total)}</p>
                </div>
              </div>

              {/* Entrega */}
              {detail.shippingCity && (
                <div className="rounded-xl bg-gray-800 p-4">
                  <p className="text-xs text-gray-500 mb-2 uppercase tracking-wider">Endereço de entrega</p>
                  <p className="text-sm text-white">
                    {detail.shippingStreet}{detail.shippingNumber ? `, ${detail.shippingNumber}` : ''}
                  </p>
                  <p className="text-sm text-gray-400">{detail.shippingCity} – {detail.shippingState} {detail.shippingZip}</p>
                  {detail.trackingCode && (
                    <p className="text-sm text-purple-400 mt-1">Rastreio: {detail.trackingCode}</p>
                  )}
                </div>
              )}

              {/* Itens */}
              <div>
                <p className="text-xs text-gray-500 mb-2 uppercase tracking-wider">Itens ({detail.items.length})</p>
                <div className="space-y-2">
                  {detail.items.map((item, i) => (
                    <div key={i} className="flex items-center gap-3 rounded-xl bg-gray-800 p-3">
                      {item.imageUrl && (
                        <img src={item.imageUrl} alt={item.productTitle} className="w-10 h-10 rounded-lg object-cover" />
                      )}
                      <div className="flex-1 min-w-0">
                        <p className="text-sm text-white truncate">{item.productTitle}</p>
                        {item.sku && <p className="text-xs text-gray-500">SKU: {item.sku}</p>}
                      </div>
                      <div className="text-right">
                        <p className="text-sm text-white">{item.quantity}×</p>
                        <p className="text-xs text-gray-400">{formatCurrency(item.unitPrice)}</p>
                      </div>
                    </div>
                  ))}
                </div>
              </div>

              <p className="text-xs text-gray-600 text-right">{formatDate(detail.createdAt)}</p>
            </div>
          )}
        </div>
      </div>
    </div>
  )
}

// ── Main Page ─────────────────────────────────────────────────────────────────

export default function DashboardPage() {
  const [summary, setSummary]         = useState<AdminSummary | null>(null)
  const [orders, setOrders]           = useState<AdminOrder[]>([])
  const [salesChart, setSalesChart]   = useState<AdminSalesChartPoint[]>([])
  const [topProducts, setTopProducts] = useState<AdminTopProduct[]>([])
  const [selectedOrder, setSelectedOrder] = useState<string | null>(null)
  const [statusFilter, setStatusFilter]   = useState<string>('')
  const [page, setPage]               = useState(1)
  const [loading, setLoading]         = useState(true)

  const load = useCallback(async () => {
    try {
      const [sum, ord, chart, top] = await Promise.all([
        fetchAdmin<AdminSummary>('/api/admin/stats/summary'),
        fetchAdmin<AdminOrder[]>(`/api/admin/orders?page=${page}&pageSize=20${statusFilter ? `&status=${statusFilter}` : ''}`),
        fetchAdmin<AdminSalesChartPoint[]>('/api/admin/charts/sales?days=30'),
        fetchAdmin<AdminTopProduct[]>('/api/admin/charts/top-products?limit=8'),
      ])
      setSummary(sum)
      setOrders(ord)
      setSalesChart(chart)
      setTopProducts(top)
    } catch (e) {
      console.error('Dashboard load error:', e)
    } finally {
      setLoading(false)
    }
  }, [page, statusFilter])

  useEffect(() => { load() }, [load])

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="w-8 h-8 border-2 border-purple-500 border-t-transparent rounded-full animate-spin" />
      </div>
    )
  }

  return (
    <>
      {selectedOrder && (
        <OrderModal orderId={selectedOrder} onClose={() => setSelectedOrder(null)} />
      )}

      {/* KPI Cards */}
      <div className="grid grid-cols-2 lg:grid-cols-3 xl:grid-cols-6 gap-4 mb-6">
        <KpiCard label="Pedidos" value={String(summary?.totalOrders ?? 0)} color="blue" />
        <KpiCard label="Receita Total" value={formatCurrency(summary?.totalRevenue ?? 0)} color="green" />
        <KpiCard label="Ticket Médio" value={formatCurrency(summary?.avgOrderValue ?? 0)} color="purple" />
        <KpiCard label="Clientes" value={String(summary?.totalCustomers ?? 0)} color="blue" />
        <KpiCard label="Pendentes" value={String(summary?.pendingOrders ?? 0)} color="red" />
        <KpiCard label="Cancelados" value={String(summary?.cancelledOrders ?? 0)} color="red" />
      </div>

      {/* Charts row */}
      <div className="grid grid-cols-1 xl:grid-cols-3 gap-4 mb-6">
        {/* Sales Area Chart */}
        <div className="xl:col-span-2 rounded-2xl bg-gray-900 border border-gray-800 p-5">
          <p className="text-sm font-semibold text-white mb-4">Vendas — últimos 30 dias</p>
          <ResponsiveContainer width="100%" height={220}>
            <AreaChart data={salesChart} margin={{ top: 0, right: 0, left: 0, bottom: 0 }}>
              <defs>
                <linearGradient id="gradRevenue" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="5%" stopColor="#7c3aed" stopOpacity={0.4} />
                  <stop offset="95%" stopColor="#7c3aed" stopOpacity={0} />
                </linearGradient>
              </defs>
              <CartesianGrid strokeDasharray="3 3" stroke="#1f2937" />
              <XAxis dataKey="date" tick={{ fill: '#6b7280', fontSize: 11 }} tickLine={false} axisLine={false}
                tickFormatter={v => v.slice(5)} />
              <YAxis tick={{ fill: '#6b7280', fontSize: 11 }} tickLine={false} axisLine={false}
                tickFormatter={v => `R$${(v / 1000).toFixed(0)}k`} />
              <Tooltip
                contentStyle={{ background: '#111827', border: '1px solid #374151', borderRadius: 8 }}
                labelStyle={{ color: '#9ca3af', fontSize: 12 }}
                formatter={(value: number) => [formatCurrency(value), 'Receita']}
              />
              <Area type="monotone" dataKey="revenue" stroke="#7c3aed" strokeWidth={2}
                fill="url(#gradRevenue)" />
            </AreaChart>
          </ResponsiveContainer>
        </div>

        {/* Top Products Bar Chart */}
        <div className="rounded-2xl bg-gray-900 border border-gray-800 p-5">
          <p className="text-sm font-semibold text-white mb-4">Top Produtos (qtd)</p>
          <ResponsiveContainer width="100%" height={220}>
            <BarChart data={topProducts} layout="vertical" margin={{ top: 0, right: 0, left: 0, bottom: 0 }}>
              <CartesianGrid strokeDasharray="3 3" stroke="#1f2937" horizontal={false} />
              <XAxis type="number" tick={{ fill: '#6b7280', fontSize: 11 }} tickLine={false} axisLine={false} />
              <YAxis type="category" dataKey="productTitle" width={90} tick={{ fill: '#6b7280', fontSize: 10 }}
                tickLine={false} axisLine={false}
                tickFormatter={v => v.length > 12 ? v.slice(0, 12) + '…' : v} />
              <Tooltip
                contentStyle={{ background: '#111827', border: '1px solid #374151', borderRadius: 8 }}
                labelStyle={{ color: '#9ca3af', fontSize: 12 }}
                formatter={(v: number) => [v, 'Unidades']}
              />
              <Bar dataKey="totalQuantity" fill="#7c3aed" radius={[0, 4, 4, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </div>
      </div>

      {/* Orders Table */}
      <div className="rounded-2xl bg-gray-900 border border-gray-800">
        <div className="flex items-center justify-between px-5 py-4 border-b border-gray-800">
          <p className="text-sm font-semibold text-white">Pedidos Recentes</p>
          <select
            value={statusFilter}
            onChange={e => { setStatusFilter(e.target.value); setPage(1) }}
            className="text-sm bg-gray-800 border border-gray-700 text-gray-300 rounded-lg px-3 py-1.5
                       focus:outline-none focus:border-purple-500"
          >
            <option value="">Todos os status</option>
            {Object.entries(STATUS_LABELS).map(([k, v]) => (
              <option key={k} value={k}>{v}</option>
            ))}
          </select>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full">
            <thead>
              <tr className="border-b border-gray-800">
                {['ID', 'Cliente', 'Status', 'Pagamento', 'Itens', 'Total', 'Data'].map(h => (
                  <th key={h} className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    {h}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {orders.length === 0 && (
                <tr>
                  <td colSpan={7} className="text-center py-10 text-gray-600">Nenhum pedido encontrado.</td>
                </tr>
              )}
              {orders.map(order => (
                <tr
                  key={order.orderId}
                  onClick={() => setSelectedOrder(order.orderId)}
                  className="border-b border-gray-800/50 hover:bg-gray-800/40 cursor-pointer transition"
                >
                  <td className="px-4 py-3 text-xs font-mono text-gray-400">{order.orderId.slice(0, 8)}…</td>
                  <td className="px-4 py-3">
                    <p className="text-sm text-white">{order.customerName ?? '—'}</p>
                    <p className="text-xs text-gray-500">{order.customerEmail ?? ''}</p>
                  </td>
                  <td className="px-4 py-3"><StatusBadge status={order.status} /></td>
                  <td className="px-4 py-3 text-sm text-gray-300">{order.paymentMethod ?? '—'}</td>
                  <td className="px-4 py-3 text-sm text-gray-300 text-center">{order.itemCount}</td>
                  <td className="px-4 py-3 text-sm font-semibold text-white">{formatCurrency(order.total)}</td>
                  <td className="px-4 py-3 text-xs text-gray-500">{formatDate(order.createdAt)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        {/* Pagination */}
        <div className="flex items-center justify-between px-5 py-3 border-t border-gray-800">
          <p className="text-xs text-gray-600">Página {page}</p>
          <div className="flex gap-2">
            <button
              disabled={page <= 1}
              onClick={() => setPage(p => p - 1)}
              className="px-3 py-1.5 rounded-lg bg-gray-800 text-gray-300 text-xs disabled:opacity-40 hover:bg-gray-700 transition"
            >
              ← Anterior
            </button>
            <button
              disabled={orders.length < 20}
              onClick={() => setPage(p => p + 1)}
              className="px-3 py-1.5 rounded-lg bg-gray-800 text-gray-300 text-xs disabled:opacity-40 hover:bg-gray-700 transition"
            >
              Próxima →
            </button>
          </div>
        </div>
      </div>
    </>
  )
}
