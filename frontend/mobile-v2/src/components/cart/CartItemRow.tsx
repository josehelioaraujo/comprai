'use client'

import Image from 'next/image'
import type { CartItem } from '@/types'

interface Props {
  item:       CartItem
  onIncrease: (id: string) => void
  onDecrease: (id: string) => void
  onRemove:   (id: string) => void
}

const fmt = (n: number) =>
  new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(n)

export default function CartItemRow({ item, onIncrease, onDecrease, onRemove }: Props) {
  return (
    <div className="flex gap-3 bg-white rounded-2xl p-3 shadow-card">
      {/* image */}
      <div className="w-20 h-20 rounded-xl bg-surface overflow-hidden shrink-0">
        {item.image ? (
          <Image src={item.image} alt={item.title} width={80} height={80}
            className="w-full h-full object-cover" unoptimized />
        ) : (
          <div className="w-full h-full flex items-center justify-center text-muted/30">
            <svg width="32" height="32" fill="currentColor" viewBox="0 0 20 20">
              <path fillRule="evenodd" d="M4 3a2 2 0 00-2 2v10a2 2 0 002 2h12a2 2 0 002-2V5a2 2 0 00-2-2H4zm12 12H4l4-8 3 6 2-4 3 6z" clipRule="evenodd"/>
            </svg>
          </div>
        )}
      </div>

      {/* info */}
      <div className="flex-1 min-w-0">
        <p className="text-sm font-medium text-ink line-clamp-2 leading-snug">{item.title}</p>
        <p className="text-xs text-muted mt-0.5">{item.source}</p>
        <p className="text-base font-bold text-ink mt-1">{fmt(item.price * item.quantity)}</p>

        <div className="flex items-center gap-3 mt-2">
          {/* qty controls */}
          <div className="flex items-center gap-2">
            <button onClick={() => onDecrease(item.productId)}
              className="w-7 h-7 rounded-lg bg-surface text-ink flex items-center justify-center
                hover:bg-brand-light active:scale-90 transition-all">
              <svg width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M20 12H4"/>
              </svg>
            </button>
            <span className="text-sm font-semibold w-5 text-center">{item.quantity}</span>
            <button onClick={() => onIncrease(item.productId)}
              className="w-7 h-7 rounded-lg bg-surface text-ink flex items-center justify-center
                hover:bg-brand-light active:scale-90 transition-all">
              <svg width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M12 4v16m8-8H4"/>
              </svg>
            </button>
          </div>

          {/* remove */}
          <button onClick={() => onRemove(item.productId)}
            className="ml-auto text-muted hover:text-red-500 transition-colors" aria-label="Remove">
            <svg width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round"
                d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"/>
            </svg>
          </button>
        </div>
      </div>
    </div>
  )
}
