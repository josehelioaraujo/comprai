'use client'

import type { UcpStep, Cart } from '@/types/ucp'
import { useTheme } from '@/lib/theme'

interface Props {
  step: UcpStep
  cart: Cart | null
  sessionId: string
}

const VERSION = process.env.NEXT_PUBLIC_APP_VERSION ?? '0.1.0'

function formatPrice(v: number) {
  return v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
}

export default function Sidebar({ step, cart, sessionId }: Props) {
  const { theme, toggle } = useTheme()

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
    <aside className="w-56 flex-shrink-0 flex flex-col h-full"
      style={{ background: 'var(--panel)', borderRight: '1px solid var(--border)' }}>
      {/* logo + version + theme toggle */}
      <div className="px-4 py-3 flex items-center justify-between"
        style={{ borderBottom: '1px solid var(--border)' }}>
        <div className="flex items-center gap-2">
          <span className="text-xl">🛍️</span>
          <div>
            <p className="text-sm font-bold" style={{ color: 'var(--text)' }}>Comprai</p>
            <p className="text-[10px]" style={{ color: 'var(--muted)' }}>v{VERSION}</p>
          </div>
        </div>
        <button onClick={toggle}
          className="w-7 h-7 rounded-lg flex items-center justify-center text-sm transition-colors"
          style={{ background: 'var(--surface)', border: '1px solid var(--border)' }}
          title={theme === 'dark' ? 'Modo claro' : 'Modo escuro'}>
          {theme === 'dark' ? '☀️' : '🌙'}
        </button>
      </div>

      {/* canal */}
      <div className="px-3 py-3" style={{ borderBottom: '1px solid var(--border)' }}>
        <p className="text-[10px] uppercase tracking-widest mb-2" style={{ color: 'var(--muted)' }}>Canal</p>
        <button className="w-full flex items-center gap-2 px-2 py-1.5 rounded-lg text-left"
          style={{ background: 'var(--surface)', border: '1px solid var(--border)' }}>
          <span>💬</span>
          <div>
            <p className="text-xs font-medium" style={{ color: 'var(--text)' }}>Web Chat</p>
            <p className="text-[10px]" style={{ color: 'var(--accent)' }}>● Online</p>
          </div>
        </button>
        <button className="w-full flex items-center gap-2 px-2 py-1.5 rounded-lg mt-1 text-left opacity-40 cursor-not-allowed" disabled>
          <span>📱</span>
          <div>
            <p className="text-xs" style={{ color: 'var(--muted)' }}>WhatsApp</p>
            <p className="text-[10px]" style={{ color: 'var(--muted)' }}>Em breve</p>
          </div>
        </button>
      </div>

      {/* funil UCP */}
      <div className="px-3 py-3 flex-1" style={{ borderBottom: '1px solid var(--border)' }}>
        <p className="text-[10px] uppercase tracking-widest mb-3" style={{ color: 'var(--muted)' }}>Fluxo UCP</p>
        <div className="flex flex-col gap-1">
          {steps.map((s, i) => {
            const sIndex = order.indexOf(s.key)
            const done = sIndex < currentIndex
            const active = s.key === step
            return (
              <div key={s.key} className="flex items-center gap-2">
                <div className="flex flex-col items-center">
                  <div className="w-6 h-6 rounded-full flex items-center justify-center text-xs flex-shrink-0"
                    style={{
                      background: active ? 'var(--accent)' : done ? 'rgba(34,197,94,0.2)' : 'var(--surface)',
                      color: active ? '#fff' : done ? 'var(--accent)' : 'var(--muted)',
                      boxShadow: active ? '0 0 0 2px rgba(34,197,94,0.3)' : 'none',
                    }}>
                    {done ? '✓' : s.icon}
                  </div>
                  {i < steps.length - 1 && (
                    <div className="w-px h-4" style={{ background: sIndex < currentIndex ? 'var(--accent)' : 'var(--border)' }} />
                  )}
                </div>
                <span className="text-xs" style={{ color: active ? 'var(--accent)' : 'var(--muted)', fontWeight: active ? 600 : 400 }}>
                  {s.label}
                </span>
              </div>
            )
          })}
        </div>
      </div>

      {/* carrinho resumido */}
      {cart && cart.items.length > 0 && (
        <div className="px-3 py-3" style={{ borderTop: '1px solid var(--border)' }}>
          <p className="text-[10px] uppercase tracking-widest mb-2" style={{ color: 'var(--muted)' }}>Carrinho</p>
          {cart.items.slice(0, 3).map(item => (
            <div key={item.productId} className="flex justify-between text-xs mb-1">
              <span className="truncate flex-1 mr-1" style={{ color: 'var(--muted)' }}>{item.title}</span>
              <span className="font-mono flex-shrink-0" style={{ color: 'var(--muted)' }}>{formatPrice(item.price)}</span>
            </div>
          ))}
          {cart.items.length > 3 && <p className="text-[10px]" style={{ color: 'var(--muted)' }}>+{cart.items.length - 3} itens</p>}
          <div className="flex justify-between mt-1 pt-1" style={{ borderTop: '1px solid var(--border)' }}>
            <span className="text-xs" style={{ color: 'var(--muted)' }}>Total</span>
            <span className="text-xs font-bold font-mono" style={{ color: 'var(--accent)' }}>{formatPrice(cart.total)}</span>
          </div>
        </div>
      )}

      {/* session footer */}
      <div className="px-3 py-2" style={{ borderTop: '1px solid var(--border)' }}>
        <p className="text-[10px] font-mono truncate" style={{ color: 'var(--border)' }}>{sessionId.slice(0,16)}...</p>
      </div>
    </aside>
  )
}
