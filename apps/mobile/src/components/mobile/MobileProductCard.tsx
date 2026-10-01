'use client'

import Image from 'next/image'
import { useState } from 'react'
import type { Product } from '@/types/ucp'

interface Props {
  product: Product
  onAddToCart: (product: Product) => void | Promise<void>
}

function formatPrice(value: number) {
  return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
}

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
  const [adding, setAdding] = useState(false)
  const hasDiscount = product.originalPrice && product.originalPrice > product.price

  async function handleAdd() {
    if (adding || !product.available) return
    setAdding(true)
    try { await onAddToCart(product) }
    finally { setAdding(false) }
  }

  return (
    <div className="
      flex flex-col w-36 rounded-xl shrink-0 snap-start
      bg-zinc-900 border border-zinc-800
      p-2.5 transition-all duration-200
      hover:border-zinc-700
    ">
      {/* Imagem */}
      <div className="h-24 w-full flex items-center justify-center bg-zinc-950 rounded-lg overflow-hidden mb-2">
        {product.image ? (
          <Image
            src={product.image}
            alt={product.title}
            width={96}
            height={96}
            className="object-contain w-full h-full"
            unoptimized
          />
        ) : (
          <span className="text-2xl">📦</span>
        )}
      </div>

      {/* Badge fonte UCP */}
      <span className="text-[8px] uppercase tracking-wider text-zinc-600 font-medium mb-0.5 truncate">
        {sourceLabel(product.source)}
      </span>

      {/* Nome — 2 linhas max */}
      <h3
        className="text-[11px] font-semibold text-zinc-100 mb-1.5 leading-tight"
        style={{ display: '-webkit-box', WebkitLineClamp: 2, WebkitBoxOrient: 'vertical', overflow: 'hidden' }}
        title={product.title}
      >
        {product.title}
      </h3>

      {/* Preços */}
      <div className="flex flex-col mb-2">
        {hasDiscount && (
          <span className="text-[9px] text-zinc-500 line-through leading-none">
            {formatPrice(product.originalPrice!)}
          </span>
        )}
        <span className="text-xs font-bold text-emerald-400">
          {formatPrice(product.price)}
        </span>
      </div>

      {/* Rating */}
      {product.rating && (
        <div className="flex items-center gap-1 mb-1.5">
          <span className="text-yellow-400 text-[10px]">★</span>
          <span className="text-[9px] text-zinc-500">{product.rating.toFixed(1)}</span>
        </div>
      )}

      {/* Botão */}
      <button
        onClick={handleAdd}
        disabled={!product.available || adding}
        className="
          w-full py-1.5 rounded-lg text-[10px] font-semibold
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
