'use client'

import Image from 'next/image'
import type { Product } from '@/types/ucp'

interface Props {
  product: Product
  onAddToCart: (product: Product) => void
}

function formatPrice(value: number) {
  return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
}

// Mapeia source do backend para label legível
function sourceLabel(source: string): string {
  const map: Record<string, string> = {
    MercadoLivre: 'Mercado Livre',
    Shopify:      'Shopify',
    DummyJSON:    'Demo Store',
    Mock:         'Demo Store',
    VtexCatalog:  'VTEX',
    VtexSearch:   'VTEX',
    OpenFoodFacts:'Open Food Facts',
  }
  return map[source] ?? source
}

export default function MobileProductCard({ product, onAddToCart }: Props) {
  const hasDiscount = product.originalPrice && product.originalPrice > product.price

  return (
    <div className="
      flex flex-col w-44 rounded-xl shrink-0 snap-start
      bg-zinc-900 border border-zinc-800
      p-3 transition-all duration-200
      hover:border-zinc-700 hover:shadow-lg hover:shadow-black/30
    ">
      {/* Imagem */}
      <div className="h-28 w-full flex items-center justify-center bg-zinc-950 rounded-lg overflow-hidden mb-2.5">
        {product.image ? (
          <Image
            src={product.image}
            alt={product.title}
            width={112}
            height={112}
            className="object-contain w-full h-full"
            unoptimized
          />
        ) : (
          <div className="w-10 h-14 bg-zinc-800 rounded border border-zinc-700 flex items-center justify-center">
            <span className="text-[10px] text-zinc-600">📦</span>
          </div>
        )}
      </div>

      {/* Badge da fonte UCP */}
      <span className="text-[9px] uppercase tracking-wider text-zinc-500 font-medium mb-0.5 truncate">
        Via: {sourceLabel(product.source)}
      </span>

      {/* Nome */}
      <h3
        className="text-xs font-semibold text-zinc-100 mb-1.5 leading-tight"
        style={{ display: '-webkit-box', WebkitLineClamp: 2, WebkitBoxOrient: 'vertical', overflow: 'hidden' }}
        title={product.title}
      >
        {product.title}
      </h3>

      {/* Preços */}
      <div className="flex items-baseline gap-1.5 mb-2.5">
        {hasDiscount && (
          <span className="text-[10px] text-zinc-500 line-through">
            {formatPrice(product.originalPrice!)}
          </span>
        )}
        <span className="text-xs font-bold text-emerald-400">
          {formatPrice(product.price)}
        </span>
      </div>

      {/* Rating */}
      {product.rating && (
        <div className="flex items-center gap-1 mb-2 -mt-1">
          <span className="text-yellow-400 text-[10px]">★</span>
          <span className="text-[10px] text-zinc-400">{product.rating.toFixed(1)}</span>
        </div>
      )}

      {/* Botão */}
      <button
        onClick={() => onAddToCart(product)}
        disabled={!product.available}
        className="
          w-full py-1.5 rounded-lg text-[11px] font-medium
          flex items-center justify-center gap-1
          transition-all duration-150 active:scale-95
          disabled:opacity-40 disabled:cursor-not-allowed
          bg-emerald-600 hover:bg-emerald-500 text-white
        "
      >
        {product.available ? '+ Adicionar' : 'Indisponível'}
      </button>
    </div>
  )
}
