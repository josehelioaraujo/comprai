'use client'

import { useState, useEffect, useCallback } from 'react'
import AppShell from '@/components/layout/AppShell'
import SearchBar from '@/components/search/SearchBar'
import ProductGrid from '@/components/search/ProductGrid'
import { apiSearch, apiGetCart, apiAddToCart } from '@/lib/api'
import { getSessionId } from '@/lib/session'
import type { Product } from '@/types'

export default function SearchPage() {
  const [query,     setQuery]     = useState('')
  const [products,  setProducts]  = useState<Product[]>([])
  const [loading,   setLoading]   = useState(false)
  const [searched,  setSearched]  = useState(false)
  const [cartCount, setCartCount] = useState(0)

  useEffect(() => {
    const sessionId = getSessionId()
    apiGetCart(sessionId).then(c => setCartCount(c.items.length)).catch(() => {})
  }, [])

  const doSearch = useCallback(async () => {
    if (!query.trim()) return
    const sessionId = getSessionId()
    setLoading(true)
    setSearched(true)
    try {
      const results = await apiSearch(query.trim(), sessionId)
      setProducts(results)
    } catch {
      setProducts([])
    } finally {
      setLoading(false)
    }
  }, [query])

  async function handleAddToCart(product: Product) {
    const sessionId = getSessionId()
    try {
      const cart = await apiAddToCart(product.id, 1, sessionId, {
        title: product.title, price: product.price,
        image: product.image, source: product.source,
      })
      setCartCount(cart.items.length)
    } catch { /* silent */ }
  }

  return (
    <AppShell cartCount={cartCount}>
      <div className="sticky top-0 z-10 bg-white border-b border-border px-4 pt-12 pb-3">
        <SearchBar
          value={query}
          onChange={setQuery}
          onSubmit={doSearch}
          autoFocus
        />
      </div>

      {!searched ? (
        <div className="flex flex-col items-center justify-center py-20 px-8 text-center">
          <svg width="56" height="56" fill="none" stroke="currentColor" strokeWidth="1.2"
            className="text-brand/30 mb-4" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round"
              d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"/>
          </svg>
          <p className="text-sm font-medium text-ink">Busque qualquer produto</p>
          <p className="text-xs text-muted mt-1">Eletrônicos, moda, casa e muito mais</p>
        </div>
      ) : (
        <ProductGrid products={products} onAddToCart={handleAddToCart} loading={loading} />
      )}
    </AppShell>
  )
}
