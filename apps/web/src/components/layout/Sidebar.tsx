'use client'

import type { UcpStep, Cart } from '@/types/ucp'

interface Props {
  step: UcpStep
  cart: Cart | null
  sessionId: string
}

function formatPrice(v: number) {
  return v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
}

export default function Sidebar({ step, cart, sessionId }: Props) {
  const steps: { key: UcpStep; label: string; icon: string }[] = [
    { key: 'search',   label: 'Busca',     icon: '🔍' },
    { key: 'cart',     label: 'Carrinho',  icon: '🛒' },
    { key: 'checkout', label: 'Pedido',    icon: '📋' },
    { key: 'payment',  label: 'Pagamento', icon: '💳' },
    { key: 'order',    label: 'Entrega',   icon: '📦' },
  ]

  const order: UcpStep[] = ['idle', 'search', 'cart', 'checkout', 'payment', 'order']
  const currentIndex = order.indexOf(step)

  return (
    <aside className="w-64 flex-shrink-0 bg-zinc-900 border-r border-zinc-800 flex flex-col h-full">
      {/* logo */}
      <div className="px-5 py-4 border-b border-zinc-800">
        <div className="flex items-center gap-2">
          <span className="text-2xl">🛍️</span>
          <div>
            <p className="text-sm font-bold text-zinc-100">Comprai</p>
            <p className="text-[10px] text-zinc-500">Você pede. A IA compra.</p>
          </div>
        </div>
      </div>

      {/* canal */}
      <div className="px-4 py-3 border-b border-zinc-800">
        <p className="text-[10px] text-zinc-600 uppercase tracking-widest mb-2">Canal</p>
        <button className="w-full flex items-center gap-2 px-3 py-2 rounded-lg bg-zinc-800 border border-zinc-700 text-left">
          <span className="text-base">💬</span>
          <div>
            <p className="text-xs font-medium text-zinc-200">Web Chat</p>
            <p className="text-[10px] text-green-400 flex items-center gap-1">
              <span className="w-1.5 h-1.5 rounded-full bg-green-500 inline-block" />
              Online
            </p>
          </div>
        </button>
        <button
          className="w-full flex items-center gap-2 px-3 py-2 rounded-lg mt-1 text-left opacity-40 cursor-not-allowed"
          disabled
        >
          <span className="text-base">📱</span>
          <div>
            <p className="text-xs font-medium text-zinc-400">WhatsApp</p>
            <p className="text-[10px] text-zinc-600">Em breve</p>
          </div>
        </button>
      </div>

      {/* funil UCP */}
      <div className="px-4 py-3 border-b border-zinc-800 flex-1">
        <p className="text-[10px] text-zinc-600 uppercase tracking-widest mb-3">Fluxo UCP</p>
        <div className="flex flex-col gap-1">
          {steps.map((s, i) => {
            const sIndex = order.indexOf(s.key)
            const done = sIndex < currentIndex
            const active = s.key === step

            return (
              <div key={s.key} className="flex items-center gap-3">
                {/* linha vertical */}
                <div className="flex flex-col items-center">
                  <div className={`w-6 h-6 rounded-full flex items-center justify-center text-xs flex-shrink-0
                    ${active ? 'bg-green-600 ring-2 ring-green-500/30' :
                      done  ? 'bg-green-900/60 text-green-600' :
                              'bg-zinc-800 text-zinc-600'}`}>
                    {done ? '✓' : s.icon}
                  </div>
                  {i < steps.length - 1 && (
                    <div className={`w-px h-5 ${sIndex < currentIndex ? 'bg-green-800' : 'bg-zinc-800'}`} />
                  )}
                </div>
                <span className={`text-xs ${active ? 'text-green-400 font-semibold' : done ? 'text-zinc-400' : 'text-zinc-600'}`}>
                  {s.label}
                </span>
              </div>
            )
          })}
        </div>
      </div>

      {/* carrinho resumido */}
      {cart && cart.items.length > 0 && (
        <div className="px-4 py-3 border-t border-zinc-800">
          <p className="text-[10px] text-zinc-600 uppercase tracking-widest mb-2">Carrinho</p>
          <div className="flex flex-col gap-1">
            {cart.items.slice(0, 3).map((item) => (
              <div key={item.productId} className="flex justify-between text-xs">
                <span className="text-zinc-400 truncate flex-1 mr-1">{item.title}</span>
                <span className="text-zinc-500 font-mono flex-shrink-0">{formatPrice(item.price)}</span>
              </div>
            ))}
            {cart.items.length > 3 && (
              <p className="text-[10px] text-zinc-600">+{cart.items.length - 3} itens</p>
            )}
            <div className="border-t border-zinc-800 mt-1 pt-1 flex justify-between">
              <span className="text-xs text-zinc-500">Total</span>
              <span className="text-xs font-bold text-green-400 font-mono">{formatPrice(cart.total)}</span>
            </div>
          </div>
        </div>
      )}

      {/* session id (debug) */}
      <div className="px-4 py-3 border-t border-zinc-800">
        <p className="text-[10px] text-zinc-700 font-mono truncate" title={sessionId}>
          session: {sessionId.slice(0, 16)}...
        </p>
      </div>
    </aside>
  )
}
