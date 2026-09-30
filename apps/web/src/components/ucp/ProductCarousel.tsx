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
      {/* scroll horizontal em mobile, grid em desktop */}
      <div className="flex gap-3 overflow-x-auto pb-2 scrollbar-thin scrollbar-thumb-zinc-700">
        {products.map((p) => (
          <div
            key={p.id}
            className="flex-shrink-0 w-48 bg-zinc-800 border border-zinc-700 rounded-xl overflow-hidden flex flex-col"
          >
            {/* imagem */}
            <div className="relative h-36 bg-zinc-900">
              {p.image ? (
                <Image
                  src={p.image}
                  alt={p.title}
                  fill
                  className="object-contain p-2"
                  unoptimized
                />
              ) : (
                <div className="flex items-center justify-center h-full text-3xl">🛍️</div>
              )}
              {/* badge da fonte */}
              <span className="absolute top-2 left-2 text-[10px] bg-zinc-900/80 text-zinc-400 px-1.5 py-0.5 rounded">
                {p.source}
              </span>
            </div>

            {/* info */}
            <div className="p-3 flex flex-col gap-2 flex-1">
              <p className="text-xs text-zinc-200 font-medium leading-tight line-clamp-2">
                {p.title}
              </p>

              <div className="flex flex-col">
                {p.originalPrice && p.originalPrice > p.price && (
                  <span className="text-[10px] text-zinc-500 line-through font-mono">
                    {formatPrice(p.originalPrice)}
                  </span>
                )}
                <span className="text-sm font-bold text-green-400 font-mono">
                  {formatPrice(p.price)}
                </span>
              </div>

              {/* disponibilidade */}
              {!p.available && (
                <span className="text-[10px] text-red-400">Indisponível</span>
              )}

              {/* ação */}
              <button
                onClick={() => onAddToCart(p)}
                disabled={!p.available}
                className="mt-auto w-full py-1.5 rounded-lg text-xs font-semibold
                  bg-green-600 hover:bg-green-500 active:bg-green-700
                  disabled:bg-zinc-700 disabled:text-zinc-500 disabled:cursor-not-allowed
                  text-white transition-colors"
              >
                {p.available ? '+ Adicionar' : 'Indisponível'}
              </button>
            </div>
          </div>
        ))}
      </div>
      <p className="text-[10px] text-zinc-500 mt-1">
        {products.length} {products.length === 1 ? 'resultado' : 'resultados'}
      </p>
    </div>
  )
}
