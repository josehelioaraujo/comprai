'use client'

import type { UcpStep } from '@/types/ucp'

interface Props {
  step: UcpStep
  cartCount?: number
  onCartClick?: () => void
  onOrderClick?: () => void
}

const STEPS: { key: UcpStep; label: string; icon: React.ReactNode }[] = [
  {
    key: 'search', label: 'Busca',
    icon: (
      <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" className="w-3.5 h-3.5">
        <circle cx="6.5" cy="6.5" r="4" /><path d="M11 11l2.5 2.5" />
      </svg>
    )
  },
  {
    key: 'cart', label: 'Carrinho',
    icon: (
      <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" className="w-3.5 h-3.5">
        <path d="M1 1h2l1.5 7h7l1.5-5H4" />
        <circle cx="6.5" cy="13" r="1" fill="currentColor" stroke="none" />
        <circle cx="11" cy="13" r="1" fill="currentColor" stroke="none" />
      </svg>
    )
  },
  {
    key: 'checkout', label: 'Pedido',
    icon: (
      <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" className="w-3.5 h-3.5">
        <rect x="2" y="1" width="10" height="13" rx="1" /><path d="M5 5h5M5 8h5M5 11h3" />
      </svg>
    )
  },
  {
    key: 'payment', label: 'Pagamento',
    icon: (
      <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" className="w-3.5 h-3.5">
        <rect x="1" y="3" width="14" height="10" rx="1.5" /><path d="M1 6h14" /><path d="M4 10h2" strokeWidth="2" />
      </svg>
    )
  },
  {
    key: 'order', label: 'Entrega',
    icon: (
      <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" className="w-3.5 h-3.5">
        <path d="M1 4l7-3 7 3v8l-7 3-7-3V4z" /><path d="M8 1v14M1 4l7 3 7-3" />
      </svg>
    )
  },
]

const ORDER: UcpStep[] = ['idle', 'search', 'cart', 'checkout', 'payment', 'order']

export default function MobileUcpProgressBar({ step, cartCount = 0, onCartClick, onOrderClick }: Props) {
  const currentIndex = ORDER.indexOf(step)

  return (
    <nav className="w-full bg-zinc-950/95 backdrop-blur-md border-t border-zinc-900 z-50 px-2 py-2">
      <div className="flex items-center justify-around max-w-xs mx-auto relative">

        {/* Linha de progresso */}
        <div className="absolute top-[18px] left-8 right-8 h-[1.5px] bg-zinc-800 z-0" />

        {STEPS.map((s) => {
          const stepIndex = ORDER.indexOf(s.key)
          const done   = stepIndex < currentIndex
          const active = s.key === step
          const isCart  = s.key === 'cart'
          const isOrder = s.key === 'order'
          const clickable = (isCart && onCartClick) || (isOrder && onOrderClick)

          return (
            <div
              key={s.key}
              className="flex flex-col items-center relative z-10 gap-1 px-1"
              onClick={isCart && onCartClick ? onCartClick : isOrder && onOrderClick ? onOrderClick : undefined}
              style={{ cursor: clickable ? 'pointer' : 'default' }}
            >
              <div className={`
                relative w-9 h-9 rounded-full
                flex items-center justify-center
                transition-all duration-300
                ${active
                  ? 'bg-emerald-500 text-black shadow-[0_0_12px_rgba(16,185,129,0.6)]'
                  : done
                    ? 'bg-zinc-800 text-emerald-400'
                    : 'bg-zinc-900 text-zinc-600 border border-zinc-800'
                }
              `}>
                {done
                  ? <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round" className="w-3.5 h-3.5"><path d="M3 8l3.5 3.5L13 5" /></svg>
                  : s.icon
                }
                {isCart && cartCount > 0 && (
                  <span className="absolute -top-1 -right-1 w-4 h-4 rounded-full bg-emerald-500 text-black text-[8px] font-bold flex items-center justify-center leading-none">
                    {cartCount > 9 ? '9+' : cartCount}
                  </span>
                )}
              </div>
              <span className={`text-[9px] whitespace-nowrap font-medium transition-colors duration-200 ${
                active ? 'text-emerald-400' : done ? 'text-zinc-500' : 'text-zinc-700'
              }`}>
                {s.label}
              </span>
            </div>
          )
        })}
      </div>
    </nav>
  )
}
