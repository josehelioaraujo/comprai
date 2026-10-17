'use client'

import { useState, useEffect } from 'react'
import AppShell from '@/components/layout/AppShell'
import HomeChat from '@/components/home/HomeChat'
import { apiGetCart, apiAddToCart } from '@/lib/api'
import { getSessionId } from '@/lib/session'
import type { Product } from '@/types'

export default function HomePage() {
  const [cartCount, setCartCount] = useState(0)

  useEffect(() => {
    const sessionId = getSessionId()
    apiGetCart(sessionId).then(cart => setCartCount(cart.items.length)).catch(() => {})
  }, [])

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
      <div className="h-full flex flex-col">
        <HomeChat onAddToCart={handleAddToCart} />
      </div>
    </AppShell>
  )
}
