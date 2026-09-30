'use client'

import { useState } from 'react'
import type { UcpStep, Cart } from '@/types/ucp'
import { useTheme } from '@/lib/theme'

interface Props {
  step: UcpStep
  cart: Cart | null
  sessionId: string
  version?: string
  gitSha?: string
}

const UCP_STEPS = [
  { key: 'search',   icon: '🔍', label: 'Busca',     desc: 'Encontre produtos por texto livre' },
  { key: 'cart',     icon: '🛒', label: 'Carrinho',  desc: 'Adicione e gerencie itens' },
  { key: 'checkout', icon: '📋', label: 'Pedido',    desc: 'Informe dados e endereço' },
  { key: 'payment',  icon: '💳', label: 'Pagamento', desc: 'Pix ou cartão de crédito' },
  { key: 'order',    icon: '📦', label: 'Entrega',   desc: 'Acompanhe o status do pedido' },
]

function formatPrice(v: number) {
  return v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
}

export default function Sidebar({ step, cart, sessionId, version = '0.1.0', gitSha = 'dev' }: Props) {
  const { theme, toggle } = useTheme()
  const [showUcpInfo, setShowUcpInfo] = useState(false)

  return (
    <aside className="w-56 flex-shrink-0 flex flex-col h-full"
      style={{ background:'var(--panel)', borderRight:'1px solid var(--border)' }}>

      {/* logo */}
      <div className="px-4 py-3 flex items-center justify-between"
        style={{ borderBottom:'1px solid var(--border)' }}>
        <div className="flex items-center gap-2">
          <span className="text-xl">🛍️</span>
          <div>
            <p className="text-sm font-bold" style={{ color:'var(--text)' }}>Comprai</p>
            <p className="text-[10px] font-mono" style={{ color:'var(--muted)' }}>
              v{version} <span style={{ color:'var(--border)' }}>·</span> {gitSha}
            </p>
          </div>
        </div>
        <button onClick={toggle}
          className="w-7 h-7 rounded-lg flex items-center justify-center text-sm"
          style={{ background:'var(--surface)', border:'1px solid var(--border)' }}>
          {theme === 'dark' ? '☀️' : '🌙'}
        </button>
      </div>

      {/* canal */}
      <div className="px-3 py-3" style={{ borderBottom:'1px solid var(--border)' }}>
        <p className="text-[10px] uppercase tracking-widest mb-2" style={{ color:'var(--muted)' }}>Canal</p>
        <button className="w-full flex items-center gap-2 px-2 py-1.5 rounded-lg text-left"
          style={{ background:'var(--surface)', border:'1px solid var(--border)' }}>
          <span>💬</span>
          <div>
            <p className="text-xs font-medium" style={{ color:'var(--text)' }}>Web Chat</p>
            <p className="text-[10px]" style={{ color:'var(--accent)' }}>● Online</p>
          </div>
        </button>
        <button className="w-full flex items-center gap-2 px-2 py-1.5 rounded-lg mt-1 text-left opacity-40 cursor-not-allowed" disabled>
          <span>📱</span>
          <div>
            <p className="text-xs" style={{ color:'var(--muted)' }}>WhatsApp</p>
            <p className="text-[10px]" style={{ color:'var(--muted)' }}>Em breve</p>
          </div>
        </button>
      </div>

      {/* Fluxo UCP */}
      <div className="px-3 py-3 relative" style={{ borderBottom:'1px solid var(--border)' }}>
        <div className="flex items-center justify-between">
          <p className="text-[10px] uppercase tracking-widest" style={{ color:'var(--muted)' }}>Fluxo UCP</p>
          <button onClick={() => setShowUcpInfo(v => !v)}
            className="w-5 h-5 rounded-full flex items-center justify-center text-[10px] font-bold"
            style={{ background: showUcpInfo ? 'var(--accent)' : 'var(--surface)',
              border:'1px solid var(--border)', color: showUcpInfo ? '#fff' : 'var(--muted)' }}>
            ℹ
          </button>
        </div>
        <div className="mt-2 flex items-center gap-2 px-2 py-1.5 rounded-lg"
          style={{ background:'var(--surface)' }}>
          <span className="text-sm">{UCP_STEPS.find(s => s.key === step)?.icon ?? '🔍'}</span>
          <span className="text-xs font-medium" style={{ color:'var(--accent)' }}>
            {UCP_STEPS.find(s => s.key === step)?.label ?? 'Busca'}
          </span>
        </div>
        {showUcpInfo && (
          <div className="absolute left-3 right-3 z-50 mt-2 rounded-xl p-3 shadow-2xl"
            style={{ background:'var(--panel)', border:'1px solid var(--border)', top:'100%' }}>
            <p className="text-[10px] font-semibold mb-1" style={{ color:'var(--text)' }}>Universal Commerce Protocol</p>
            <p className="text-[10px] mb-2" style={{ color:'var(--muted)' }}>Fluxo conversacional de compras em 5 etapas.</p>
            {UCP_STEPS.map(s => (
              <div key={s.key} className="flex items-start gap-2 mb-1.5">
                <span className="text-sm flex-shrink-0">{s.icon}</span>
                <div>
                  <p className="text-[10px] font-medium" style={{ color: step===s.key ? 'var(--accent)' : 'var(--text)' }}>
                    {s.label}{step===s.key ? ' ← atual' : ''}
                  </p>
                  <p className="text-[9px]" style={{ color:'var(--muted)' }}>{s.desc}</p>
                </div>
              </div>
            ))}
            <button onClick={() => setShowUcpInfo(false)} className="mt-2 w-full text-[10px] py-1 rounded"
              style={{ background:'var(--surface)', color:'var(--muted)' }}>Fechar</button>
          </div>
        )}
      </div>

      {/* carrinho */}
      {cart && cart.items.length > 0 && (
        <div className="px-3 py-3 flex-1" style={{ borderBottom:'1px solid var(--border)' }}>
          <p className="text-[10px] uppercase tracking-widest mb-2" style={{ color:'var(--muted)' }}>Carrinho</p>
          {cart.items.slice(0, 3).map(item => (
            <div key={item.productId} className="flex justify-between text-xs mb-1">
              <span className="truncate flex-1 mr-1" style={{ color:'var(--muted)' }}>{item.title}</span>
              <span className="font-mono flex-shrink-0" style={{ color:'var(--muted)' }}>{formatPrice(item.price)}</span>
            </div>
          ))}
          {cart.items.length > 3 && <p className="text-[10px]" style={{ color:'var(--muted)' }}>+{cart.items.length-3} itens</p>}
          <div className="flex justify-between mt-1 pt-1" style={{ borderTop:'1px solid var(--border)' }}>
            <span className="text-xs" style={{ color:'var(--muted)' }}>Total</span>
            <span className="text-xs font-bold font-mono" style={{ color:'var(--accent)' }}>{formatPrice(cart.total)}</span>
          </div>
        </div>
      )}

      {/* footer */}
      <div className="px-3 py-2 mt-auto" style={{ borderTop:'1px solid var(--border)' }}>
        <p className="text-[10px] font-mono truncate" style={{ color:'var(--border)' }}>{sessionId.slice(0,16)}...</p>
      </div>
    </aside>
  )
}
