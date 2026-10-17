import type { Product } from '@/types'
import ProductCard from './ProductCard'

interface Props {
  products:     Product[]
  onAddToCart?: (product: Product) => void
  loading?:     boolean
}

function SkeletonCard() {
  return (
    <div className="bg-white rounded-2xl shadow-card overflow-hidden animate-pulse">
      <div className="aspect-square bg-surface" />
      <div className="p-3 space-y-2">
        <div className="h-3 bg-surface rounded w-1/2" />
        <div className="h-4 bg-surface rounded w-full" />
        <div className="h-4 bg-surface rounded w-3/4" />
        <div className="h-5 bg-surface rounded w-1/3 mt-2" />
      </div>
    </div>
  )
}

export default function ProductGrid({ products, onAddToCart, loading }: Props) {
  if (loading) {
    return (
      <div className="grid grid-cols-2 gap-3 p-4">
        {Array.from({ length: 6 }).map((_, i) => <SkeletonCard key={i} />)}
      </div>
    )
  }

  if (!products.length) {
    return (
      <div className="flex flex-col items-center justify-center py-20 px-8 text-center">
        <svg width="48" height="48" fill="none" stroke="currentColor" strokeWidth="1.5"
          className="text-muted/40 mb-4" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round"
            d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"/>
        </svg>
        <p className="text-sm font-medium text-ink">No results found</p>
        <p className="text-xs text-muted mt-1">Try different keywords or browse categories</p>
      </div>
    )
  }

  return (
    <div className="grid grid-cols-2 gap-3 p-4">
      {products.map((p, i) => (
        <ProductCard key={p.id ?? i} product={p} onAddToCart={onAddToCart} />
      ))}
    </div>
  )
}
