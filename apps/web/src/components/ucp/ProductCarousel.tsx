'use client'

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
  return (
    <div className="mt-2 w-full">
      <div className="flex gap-2 overflow-x-auto pb-2 scrollbar-none">
        {products.map((p) => (
          <div key={p.id}
            className="flex-shrink-0 w-36 bg-zinc-800 border border-zinc-700 rounded-lg overflow-hidden flex flex-col">
            {/* imagem */}
            <div className="relative h-24 bg-zinc-900">
              {p.image ? (
                <Image src={p.image} alt={p.title} fill
                  className="object-contain p-1.5" unoptimized />
              ) : (
                <div className="flex items-center justify-center h-full text-2xl">🛍️</div>
              )}
              <span className="absolute top-1 left-1 text-[9px] bg-zinc-900/80 text-zinc-500 px-1 py-0.5 rounded">
                {p.source}
              </span>
            </div>
            {/* info */}
            <div className="p-2 flex flex-col gap-1 flex-1">
              <p className="text-[11px] text-zinc-200 font-medium leading-tight line-clamp-2">{p.title}</p>
              <div className="flex flex-col">
                {p.originalPrice && p.originalPrice > p.price && (
                  <span className="text-[9px] text-zinc-500 line-through font-mono">
                    {formatPrice(p.originalPrice)}
                  </span>
                )}
                <span className="text-xs font-bold text-green-400 font-mono">
                  {formatPrice(p.price)}
                </span>
              </div>
              <button
                onClick={() => onAddToCart(p)}
                disabled={!p.available}
                className="mt-auto w-full py-1 rounded text-[10px] font-semibold
                  bg-green-600 hover:bg-green-500 active:bg-green-700
                  disabled:bg-zinc-700 disabled:text-zinc-500
                  text-white transition-colors"
              >
                {p.available ? '+ Adicionar' : 'Indisponível'}
              </button>
            </div>
          </div>
        ))}
      </div>
      <p className="text-[10px] text-zinc-600 mt-0.5">
        {products.length} {products.length === 1 ? 'resultado' : 'resultados'}
      </p>
    </div>
  )
}
