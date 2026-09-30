'use client'

import type { UcpStep } from '@/types/ucp'

interface Props {
  step: UcpStep
}

const STEPS: { key: UcpStep; label: string; icon: string }[] = [
  { key: 'search',   label: 'Busca',     icon: '🔍' },
  { key: 'cart',     label: 'Carrinho',  icon: '🛒' },
  { key: 'checkout', label: 'Pedido',    icon: '📋' },
  { key: 'payment',  label: 'Pagamento', icon: '💳' },
  { key: 'order',    label: 'Entrega',   icon: '📦' },
]

const ORDER: UcpStep[] = ['idle', 'search', 'cart', 'checkout', 'payment', 'order']

export default function UcpProgressBar({ step }: Props) {
  const currentIndex = ORDER.indexOf(step)

  return (
    <div className="px-4 py-2 border-b border-zinc-800 bg-zinc-900/50">
      <div className="flex items-center justify-between">
        {STEPS.map((s, i) => {
          const stepIndex = ORDER.indexOf(s.key)
          const done = stepIndex < currentIndex
          const active = s.key === step

          return (
            <div key={s.key} className="flex items-center">
              {/* step */}
              <div className="flex flex-col items-center gap-0.5">
                <div
                  className={`w-7 h-7 rounded-full flex items-center justify-center text-sm transition-all
                    ${active ? 'bg-green-600 ring-2 ring-green-500/30 scale-110' :
                      done  ? 'bg-green-900/60 text-green-600' :
                              'bg-zinc-800 text-zinc-600'}
                  `}
                >
                  {done ? '✓' : s.icon}
                </div>
                <span className={`text-[9px] hidden sm:block ${active ? 'text-green-400' : done ? 'text-zinc-500' : 'text-zinc-700'}`}>
                  {s.label}
                </span>
              </div>

              {/* conector */}
              {i < STEPS.length - 1 && (
                <div
                  className={`h-0.5 w-8 sm:w-12 mx-1 rounded transition-colors ${
                    stepIndex < currentIndex ? 'bg-green-700' : 'bg-zinc-800'
                  }`}
                />
              )}
            </div>
          )
        })}
      </div>
    </div>
  )
}
