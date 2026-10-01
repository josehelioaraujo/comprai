'use client'

import type { UcpStep } from '@/types/ucp'

interface Props {
  step: UcpStep
  cartCount?: number
  onCartClick?: () => void
}

const STEPS: { key: UcpStep; label: string }[] = [
  { key: 'search',   label: 'Busca'     },
  { key: 'cart',     label: 'Carrinho'  },
  { key: 'checkout', label: 'Pedido'    },
  { key: 'payment',  label: 'Pagamento' },
  { key: 'order',    label: 'Entrega'   },
]

const ORDER: UcpStep[] = ['idle', 'search', 'cart', 'checkout', 'payment', 'order']

export default function MobileUcpProgressBar({ step, cartCount = 0, onCartClick }: Props) {
  const currentIndex = ORDER.indexOf(step)

  return (
    <header className="w-full bg-zinc-950/80 backdrop-blur-md border-b border-zinc-900 px-4 py-3 sticky top-0 z-50">
      <div className="flex items-center justify-between max-w-xs mx-auto relative px-2">

        {/* Linha de fundo */}
        <div className="absolute top-3 left-4 right-4 h-[2px] bg-zinc-800 z-0" />

        {STEPS.map((s, i) => {
          const stepIndex = ORDER.indexOf(s.key)
          const done   = stepIndex < currentIndex
          const active = s.key === step
          const isCart = s.key === 'cart'

          return (
            <div
              key={s.key}
              className="flex flex-col items-center relative z-10"
              onClick={isCart && onCartClick ? onCartClick : undefined}
              style={{ cursor: isCart && onCartClick ? 'pointer' : 'default' }}
            >
              {/* Círculo */}
              <div className={`
                w-6 h-6 rounded-full flex items-center justify-center text-[11px] font-bold
                transition-all duration-300
                ${active
                  ? 'bg-emerald-500 text-black shadow-[0_0_12px_rgba(16,185,129,0.5)]'
                  : done
                    ? 'bg-emerald-800 text-emerald-300'
                    : 'bg-zinc-900 text-zinc-500 border border-zinc-800'
                }
              `}>
                {done ? '✓' : i + 1}

                {/* Badge carrinho */}
                {isCart && cartCount > 0 && (
                  <span className="absolute -top-1.5 -right-1.5 w-4 h-4 rounded-full bg-emerald-500 text-black text-[9px] font-bold flex items-center justify-center">
                    {cartCount > 9 ? '9+' : cartCount}
                  </span>
                )}
              </div>

              {/* Label — só no step ativo */}
              {active && (
                <span className="absolute -bottom-5 text-[10px] font-medium text-emerald-400 whitespace-nowrap">
                  {s.label}
                </span>
              )}
            </div>
          )
        })}
      </div>
    </header>
  )
}
