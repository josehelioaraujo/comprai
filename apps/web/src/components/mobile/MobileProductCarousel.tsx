'use client'

import type { Product } from '@/types/ucp'
import MobileProductCard from './MobileProductCard'

interface Props {
  products: Product[]
  onAddToCart: (product: Product) => void
}

export default function MobileProductCarousel({ products, onAddToCart }: Props) {
  if (!products.length) return null

  return (
    <div className="w-full">
      {/* Hint de scroll — some após 1s com CSS */}
      {products.length > 1 && (
        <p className="text-[10px] text-zinc-600 mb-1.5 pl-0.5">
          {products.length} produtos · deslize para ver →
        </p>
      )}

      {/* Trilho de scroll touch-nativo */}
      <div className="
        flex gap-3
        overflow-x-auto pb-3
        snap-x snap-mandatory
        scrollbar-none
        scroll-smooth
      ">
        {products.map(p => (
          <MobileProductCard
            key={p.id}
            product={p}
            onAddToCart={onAddToCart}
          />
        ))}

        {/* Espaçador final — garante que o último card não fique colado na borda */}
        <div className="w-3 shrink-0" />
      </div>
    </div>
  )
}
