'use client'

import { useState, useRef } from 'react'
import type { UcpStep, Cart } from '@/types/ucp'
import { useTheme } from '@/lib/theme'

interface Props {
  step: UcpStep
  cart: Cart | null
  sessionId: string
  version?: string
  gitSha?: string
  collapsed?: boolean
  onToggleCollapse?: () => void
  onViewCart?: () => void
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

export default function Sidebar({ step, cart, sessionId, version = '0.1.0', gitSha = 'dev', collapsed = false, onToggleCollapse, onViewCart }: Props) {
  const { theme, toggle } = useTheme()
  const [showAbout, setShowAbout] = useState(false)
  const [showCartPopup, setShowCartPopup] = useState(false)
  const logoRef = useRef<HTMLButtonElement>(null)
  const cartBtnRef = useRef<HTMLButtonElement>(null)
  const [modalPos, setModalPos] = useState({ top: 0, left: 0 })
  const [cartPopupPos, setCartPopupPos] = useState({ top: 0, left: 0 })

  const currentStep = UCP_STEPS.find(s => s.key === step)
  const cartCount = cart?.items.reduce((s, i) => s + i.quantity, 0) ?? 0
  const cartTotal = cart?.items.reduce((s, i) => s + i.price * i.quantity, 0) ?? 0

  function handleLogoClick() {
    if (logoRef.current) {
      const rect = logoRef.current.getBoundingClientRect()
      setModalPos({ top: rect.bottom + 8, left: rect.left })
    }
    setShowAbout(true)
  }

  function handleCartClick() {
    if (cartBtnRef.current) {
      const rect = cartBtnRef.current.getBoundingClientRect()
      setCartPopupPos({ top: rect.bottom + 8, left: rect.left })
    }
    setShowCartPopup(v => !v)
  }

  if (collapsed) {
    return (
      <aside className="flex-shrink-0 flex flex-col items-center py-3 gap-3 h-full"
        style={{ width:52, background:'var(--panel)', borderRight:'1px solid var(--border)' }}>
        <button onClick={onToggleCollapse} title="Expandir"
          className="w-8 h-8 rounded-lg flex items-center justify-center text-sm"
          style={{ background:'var(--surface)', border:'1px solid var(--border)', color:'var(--muted)' }}>
          ›
        </button>
        <span className="text-lg">🛍️</span>
        {cartCount > 0 && (
          <button ref={cartBtnRef} onClick={handleCartClick} className="relative">
            <span className="text-lg">🛒</span>
            <span className="absolute -top-1 -right-1 w-4 h-4 rounded-full text-[9px] font-bold flex items-center justify-center"
              style={{ background:'#ef4444', color:'#fff' }}>
              {cartCount > 9 ? '9+' : cartCount}
            </span>
          </button>
        )}
        <div className="flex-1" />
        <button onClick={toggle} className="w-8 h-8 rounded-lg flex items-center justify-center text-sm"
          style={{ background:'var(--surface)', border:'1px solid var(--border)' }}>
          {theme === 'dark' ? '☀️' : '🌙'}
        </button>
      </aside>
    )
  }

  return (
    <>
      <aside className="w-56 flex-shrink-0 flex flex-col h-full"
        style={{ background:'var(--panel)', borderRight:'1px solid var(--border)' }}>

        {/* header */}
        <div className="px-3 py-3 flex items-center gap-2"
          style={{ borderBottom:'1px solid var(--border)' }}>
          <button ref={logoRef} onClick={handleLogoClick}
            className="flex items-center gap-2 flex-1 text-left min-w-0 rounded-lg px-1 py-0.5">
            <span className="text-xl flex-shrink-0">🛍️</span>
            <div className="min-w-0">
              <p className="text-sm font-bold truncate" style={{ color:'var(--text)' }}>Comprai</p>
              <p className="text-[10px] font-mono truncate" style={{ color:'var(--muted)' }}>
                v{version} · {gitSha}
              </p>
            </div>
          </button>
          <button onClick={toggle}
            className="w-7 h-7 flex-shrink-0 rounded-lg flex items-center justify-center text-sm"
            style={{ background:'var(--surface)', border:'1px solid var(--border)' }}>
            {theme === 'dark' ? '☀️' : '🌙'}
          </button>
          <button onClick={onToggleCollapse}
            className="w-7 h-7 flex-shrink-0 rounded-lg flex items-center justify-center text-sm"
            style={{ background:'var(--surface)', border:'1px solid var(--border)', color:'var(--muted)' }}>
            ‹
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

        {/* etapa atual */}
        <div className="px-3 py-3" style={{ borderBottom:'1px solid var(--border)' }}>
          <div className="flex items-center gap-2 px-2 py-1">
            <span className="text-sm">{currentStep?.icon ?? '🔍'}</span>
            <span className="text-xs" style={{ color:'var(--muted)' }}>Etapa atual:</span>
            <span className="text-xs font-semibold" style={{ color:'var(--accent)' }}>
              {currentStep?.label ?? 'Busca'}
            </span>
          </div>
        </div>

        {/* ícone carrinho com badge */}
        <div className="px-3 py-3 flex-1" style={{ borderBottom:'1px solid var(--border)' }}>
          <button ref={cartBtnRef} onClick={handleCartClick}
            className="w-full flex items-center gap-3 px-2 py-2 rounded-lg transition-colors"
            style={{ background: cartCount > 0 ? 'var(--surface)' : 'transparent',
              border: cartCount > 0 ? '1px solid var(--border)' : '1px solid transparent' }}>
            <div className="relative flex-shrink-0">
              <span className="text-xl">🛒</span>
              {cartCount > 0 && (
                <span className="absolute -top-1.5 -right-1.5 w-4 h-4 rounded-full text-[9px] font-bold flex items-center justify-center"
                  style={{ background:'#ef4444', color:'#fff' }}>
                  {cartCount > 9 ? '9+' : cartCount}
                </span>
              )}
            </div>
            <div className="flex-1 text-left min-w-0">
              <p className="text-xs font-medium" style={{ color: cartCount > 0 ? 'var(--text)' : 'var(--muted)' }}>
                {cartCount > 0 ? `${cartCount} ${cartCount===1?'item':'itens'}` : 'Carrinho vazio'}
              </p>
              {cartCount > 0 && (
                <p className="text-[10px] font-mono" style={{ color:'var(--accent)' }}>
                  {formatPrice(cartTotal)}
                </p>
              )}
            </div>
            {cartCount > 0 && (
              <span className="text-[10px]" style={{ color:'var(--muted)' }}>›</span>
            )}
          </button>
        </div>

        {/* footer */}
        <div className="px-3 py-2 mt-auto" style={{ borderTop:'1px solid var(--border)' }}>
          <p className="text-[10px] font-mono truncate" style={{ color:'var(--border)' }}>{sessionId.slice(0,16)}...</p>
        </div>
      </aside>

      {/* popup do carrinho */}
      {showCartPopup && cart && cart.items.length > 0 && (
        <div className="fixed inset-0 z-50" onClick={() => setShowCartPopup(false)}>
          <div className="absolute rounded-xl p-4 w-72 shadow-2xl"
            style={{
              top: Math.min(cartPopupPos.top, window.innerHeight - 400),
              left: Math.min(cartPopupPos.left, window.innerWidth - 290),
              background:'var(--panel)', border:'1px solid var(--border)',
            }}
            onClick={e => e.stopPropagation()}>
            <div className="flex items-center justify-between mb-3">
              <p className="text-sm font-semibold" style={{ color:'var(--text)' }}>🛒 Carrinho</p>
              <button onClick={() => setShowCartPopup(false)}
                className="w-6 h-6 rounded-full flex items-center justify-center text-xs"
                style={{ background:'var(--surface)', border:'1px solid var(--border)', color:'var(--muted)' }}>
                ✕
              </button>
            </div>
            <div className="flex flex-col gap-1 mb-3">
              {cart.items.map(item => (
                <div key={item.productId} className="flex justify-between items-center text-xs py-1"
                  style={{ borderBottom:'1px solid var(--border)' }}>
                  <span className="truncate flex-1 mr-2" style={{ color:'var(--text)' }}>{item.title}</span>
                  <span className="flex-shrink-0 text-[10px]" style={{ color:'var(--muted)' }}>×{item.quantity}</span>
                  <span className="flex-shrink-0 font-mono ml-2" style={{ color:'var(--accent)' }}>
                    {formatPrice(item.price * item.quantity)}
                  </span>
                </div>
              ))}
            </div>
            <div className="flex justify-between mb-3">
              <span className="text-xs font-semibold" style={{ color:'var(--text)' }}>Total</span>
              <span className="text-sm font-bold font-mono" style={{ color:'var(--accent)' }}>
                {formatPrice(cartTotal)}
              </span>
            </div>
            <button onClick={() => { setShowCartPopup(false); onViewCart?.() }}
              className="w-full py-2 rounded-lg text-xs font-semibold"
              style={{ background:'var(--accent)', color:'#fff' }}>
              Fechar pedido →
            </button>
          </div>
        </div>
      )}

      {/* modal sobre */}
      {showAbout && (
        <div className="fixed inset-0 z-50" onClick={() => setShowAbout(false)}>
          <div className="absolute rounded-2xl p-5 w-72 shadow-2xl"
            style={{
              top: Math.min(modalPos.top, window.innerHeight - 480),
              left: Math.min(modalPos.left, window.innerWidth - 300),
              background:'var(--panel)', border:'1px solid var(--border)',
            }}
            onClick={e => e.stopPropagation()}>
            <div className="flex items-center gap-2 mb-3 px-2 py-1 rounded-lg"
              style={{ background:'rgba(34,197,94,0.1)', border:'1px solid rgba(34,197,94,0.2)' }}>
              <span className="text-xs font-bold flex-1" style={{ color:'var(--accent)' }}>⚡ Universal Commerce Protocol</span>
              <button onClick={() => setShowAbout(false)}
                className="w-5 h-5 rounded-full flex items-center justify-center text-[10px]"
                style={{ background:'var(--surface)', border:'1px solid var(--border)', color:'var(--muted)' }}>
                ✕
              </button>
            </div>
            <div className="flex items-center gap-3 mb-3">
              <span className="text-3xl">🛍️</span>
              <div>
                <h2 className="text-base font-bold" style={{ color:'var(--text)' }}>Comprai</h2>
                <p className="text-[10px] font-mono" style={{ color:'var(--muted)' }}>v{version} · {gitSha}</p>
              </div>
            </div>
            <p className="text-xs mb-4" style={{ color:'var(--muted)', lineHeight:1.6 }}>
              Assistente de compras com IA que guia do pedido à entrega em linguagem natural, usando o fluxo UCP de 5 etapas.
            </p>
            <div className="flex flex-col gap-1.5">
              {UCP_STEPS.map(s => (
                <div key={s.key} className="flex items-center gap-2.5 p-2 rounded-lg"
                  style={{
                    background: step===s.key ? 'rgba(34,197,94,0.1)' : 'var(--surface)',
                    border: step===s.key ? '1px solid rgba(34,197,94,0.3)' : '1px solid transparent',
                  }}>
                  <span className="text-base">{s.icon}</span>
                  <div>
                    <p className="text-[11px] font-medium" style={{ color: step===s.key ? 'var(--accent)' : 'var(--text)' }}>
                      {s.label}{step===s.key ? ' ← você está aqui' : ''}
                    </p>
                    <p className="text-[9px]" style={{ color:'var(--muted)' }}>{s.desc}</p>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>
      )}
    </>
  )
}
