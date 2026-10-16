'use client'

import { useRef } from 'react'
import Image from 'next/image'
import type { Product } from '@/types/ucp'

interface Props {
  products: Product[]
  onAddToCart: (product: Product) => void
}

function formatPrice(value: number) {
  return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
}

export default function ProductCarousel({ products, onAddToCart }: Props) {
  const scrollRef = useRef<HTMLDivElement>(null)

  function scroll(dir: 'left' | 'right') {
    if (!scrollRef.current) return
    scrollRef.current.scrollBy({ left: dir === 'right' ? 300 : -300, behavior: 'smooth' })
  }

  return (
    <div className="mt-2 w-full">
      <div className="relative">
        {/* seta esquerda */}
        <button onClick={() => scroll('left')}
          className="absolute left-0 top-1/2 -translate-y-1/2 z-10 w-7 h-7 rounded-full flex items-center justify-center text-sm shadow-lg transition-colors -ml-1"
          style={{ background:'var(--panel)', border:'1px solid var(--border)', color:'var(--text)' }}>
          ‹
        </button>

        {/* cards */}
        <div ref={scrollRef}
          className="flex gap-2 overflow-x-auto px-6 pb-2 scrollbar-none scroll-smooth">
          {products.map((p) => (
            <div key={p.id}
              className="flex-shrink-0 w-32 rounded-lg overflow-hidden flex flex-col"
              style={{ background:'var(--surface)', border:'1px solid var(--border)' }}>
              {/* imagem */}
              <div className="relative h-20" style={{ background:'var(--panel)' }}>
                {p.image ? (
                  <Image src={p.image} alt={p.title} fill className="object-contain p-1.5" unoptimized />
                ) : (
                  <div className="flex items-center justify-center h-full text-2xl">🛍️</div>
                )}
              </div>
              {/* info */}
              <div className="p-2 flex flex-col gap-1 flex-1">
                <p className="text-[11px] font-medium leading-tight line-clamp-2" style={{ color:'var(--text)' }}>
                  {p.title}
                </p>
                <div className="flex flex-col">
                  {p.originalPrice && p.originalPrice > p.price && (
                    <span className="text-[9px] line-through font-mono" style={{ color:'var(--muted)' }}>
                      {formatPrice(p.originalPrice)}
                    </span>
                  )}
                  <span className="text-xs font-bold font-mono" style={{ color:'var(--accent)' }}>
                    {formatPrice(p.price)}
                  </span>
                </div>
                <button onClick={() => onAddToCart(p)} disabled={!p.available}
                  className="mt-auto w-full py-1 rounded text-[10px] font-semibold transition-colors"
                  style={{
                    background: p.available ? 'var(--accent)' : 'var(--surface)',
                    color: p.available ? '#fff' : 'var(--muted)',
                    cursor: p.available ? 'pointer' : 'not-allowed'
                  }}>
                  {p.available ? '+ Adicionar' : 'Indisponível'}
                </button>
              </div>
            </div>
          ))}
        </div>

        {/* seta direita */}
        <button onClick={() => scroll('right')}
          className="absolute right-0 top-1/2 -translate-y-1/2 z-10 w-7 h-7 rounded-full flex items-center justify-center text-sm shadow-lg transition-colors -mr-1"
          style={{ background:'var(--panel)', border:'1px solid var(--border)', color:'var(--text)' }}>
          ›
        </button>
      </div>
      <p className="text-[10px] mt-1 px-1" style={{ color:'var(--muted)' }}>
        {products.length} {products.length === 1 ? 'resultado' : 'resultados'}
      </p>
    </div>
  )
}
