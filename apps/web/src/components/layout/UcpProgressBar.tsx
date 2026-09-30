'use client'

import type { UcpStep } from '@/types/ucp'

interface Props { step: UcpStep }

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
    <div className="px-3 py-2" style={{ borderBottom:'1px solid var(--border)', background:'var(--panel)' }}>
      <div className="flex items-center justify-start gap-1">
        {STEPS.map((s, i) => {
          const stepIndex = ORDER.indexOf(s.key)
          const done = stepIndex < currentIndex
          const active = s.key === step

          return (
            <div key={s.key} className="flex items-center">
              <div className="flex flex-col items-center gap-0.5">
                <div className="w-7 h-7 rounded-full flex items-center justify-center text-xs transition-all"
                  style={{
                    background: active ? 'var(--accent)' : done ? 'rgba(34,197,94,0.2)' : 'var(--surface)',
                    color: active ? '#fff' : done ? 'var(--accent)' : 'var(--muted)',
                    boxShadow: active ? '0 0 0 2px rgba(34,197,94,0.3)' : 'none',
                    transform: active ? 'scale(1.1)' : 'scale(1)',
                  }}>
                  {done ? '✓' : s.icon}
                </div>
                <span className="text-[9px] leading-none hidden sm:block"
                  style={{ color: active ? 'var(--accent)' : 'var(--muted)' }}>
                  {s.label}
                </span>
              </div>
              {i < STEPS.length - 1 && (
                <div className="h-px w-5 mx-1 mb-3"
                  style={{ background: stepIndex < currentIndex ? 'var(--accent)' : 'var(--border)' }} />
              )}
            </div>
          )
        })}
      </div>
    </div>
  )
}
