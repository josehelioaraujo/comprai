'use client'

import Image  from 'next/image'
import type { Product } from '@/types'

interface Props {
  product:    Product
  onAddToCart?: (product: Product) => void
}

function StarIcon() {
  return (
    <svg width="12" height="12" viewBox="0 0 20 20" fill="#FBBF24">
      <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z"/>
    </svg>
  )
}

export default function ProductCard({ product, onAddToCart }: Props) {
  const discountPct = product.originalPrice && product.originalPrice > product.price
    ? Math.round((1 - product.price / product.originalPrice) * 100)
    : null

  return (
    <div className="bg-white rounded-2xl shadow-card overflow-hidden flex flex-col group">
      {/* image */}
      <div className="relative aspect-square bg-surface overflow-hidden">
        {product.image ? (
          <Image
            src={product.image} alt={product.title}
            fill className="object-cover group-hover:scale-105 transition-transform duration-300"
            sizes="(max-width: 640px) 50vw, 180px"
            unoptimized
          />
        ) : (
          <div className="w-full h-full flex items-center justify-center text-muted/30">
            <svg width="48" height="48" fill="currentColor" viewBox="0 0 20 20">
              <path fillRule="evenodd" d="M4 3a2 2 0 00-2 2v10a2 2 0 002 2h12a2 2 0 002-2V5a2 2 0 00-2-2H4zm12 12H4l4-8 3 6 2-4 3 6z" clipRule="evenodd"/>
            </svg>
          </div>
        )}
        {discountPct && (
          <span className="absolute top-2 left-2 bg-brand text-white text-[10px] font-bold px-1.5 py-0.5 rounded-lg">
            -{discountPct}%
          </span>
        )}
        {!product.available && (
          <div className="absolute inset-0 bg-white/60 flex items-center justify-center">
            <span className="text-xs font-semibold text-muted">Out of stock</span>
          </div>
        )}
      </div>

      {/* info */}
      <div className="p-3 flex flex-col gap-1.5 flex-1">
        <p className="text-xs text-muted truncate">{product.source ?? 'catalog'}</p>
        <h3 className="text-sm font-medium text-ink line-clamp-2 leading-snug">{product.title}</h3>

        {product.rating != null && (
          <div className="flex items-center gap-1">
            <StarIcon />
            <span className="text-xs text-muted">{product.rating.toFixed(1)}</span>
          </div>
        )}

        <div className="flex items-end justify-between mt-auto pt-1">
          <div>
            <p className="text-base font-bold text-ink">
              {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(product.price)}
            </p>
            {product.originalPrice && product.originalPrice > product.price && (
              <p className="text-xs text-muted line-through">
                {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(product.originalPrice)}
              </p>
            )}
          </div>

          {product.available && onAddToCart && (
            <button
              onClick={() => onAddToCart(product)}
              className="w-8 h-8 rounded-xl bg-brand text-white flex items-center justify-center hover:bg-brand-dark active:scale-90 transition-all"
              aria-label="Add to cart"
            >
              <svg width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M12 4v16m8-8H4"/>
              </svg>
            </button>
          )}
        </div>
      </div>
    </div>
  )
}
