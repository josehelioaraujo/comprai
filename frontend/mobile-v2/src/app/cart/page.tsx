'use client'

import { useState, useEffect } from 'react'
import AppShell from '@/components/layout/AppShell'
import CartItemRow from '@/components/cart/CartItemRow'
import CartSummary from '@/components/cart/CartSummary'
import Spinner from '@/components/ui/Spinner'
import { apiGetCart, apiAddToCart, apiRemoveFromCart } from '@/lib/api'
import { getSessionId } from '@/lib/session'
import type { Cart } from '@/types'

const EMPTY_CART: Cart = { items: [] }

export default function CartPage() {
  const [cart,    setCart]    = useState<Cart>(EMPTY_CART)
  const [loading, setLoading] = useState(true)

  async function refresh() {
    const sessionId = getSessionId()
    try {
      const c = await apiGetCart(sessionId)
      setCart(c)
    } catch {
      setCart(EMPTY_CART)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { refresh() }, [])

  async function handleIncrease(itemId: string) {
    const sessionId = getSessionId()
    const item = cart.items.find(i => i.itemId === itemId)
    if (!item) return
    try {
      // Backend accumulates: same product → qty + 1
      const updated = await apiAddToCart(item.productId, 1, sessionId, {
        title: item.title, price: item.price, image: item.image, source: item.source, url: item.url,
      })
      setCart(updated)
    } catch { /* silent */ }
  }

  async function handleDecrease(itemId: string) {
    const sessionId = getSessionId()
    const item = cart.items.find(i => i.itemId === itemId)
    if (!item) return
    try {
      if (item.quantity <= 1) {
        // Remove entirely
        const updated = await apiRemoveFromCart(itemId, sessionId)
        setCart(updated)
      } else {
        // Remove then re-add with (qty - 1)
        await apiRemoveFromCart(itemId, sessionId)
        const updated = await apiAddToCart(item.productId, item.quantity - 1, sessionId, {
          title: item.title, price: item.price, image: item.image, source: item.source, url: item.url,
        })
        setCart(updated)
      }
    } catch { /* silent */ }
  }

  async function handleRemove(itemId: string) {
    const sessionId = getSessionId()
    try {
      const updated = await apiRemoveFromCart(itemId, sessionId)
      setCart(updated)
    } catch { /* silent */ }
  }

  const cartCount = cart.items.reduce((s, i) => s + i.quantity, 0)

  return (
    <AppShell cartCount={cartCount}>
      <div className="px-4 pt-12 pb-3 bg-white border-b border-border">
        <h1 className="text-xl font-bold text-ink">Carrinho</h1>
        {!loading && cartCount > 0 && (
          <p className="text-xs text-muted mt-0.5">{cartCount} iten{cartCount > 1 ? 's' : ''}</p>
        )}
      </div>

      {loading ? (
        <div className="flex justify-center py-20"><Spinner size={32} /></div>
      ) : cart.items.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-20 px-8 text-center">
          <svg width="56" height="56" fill="none" stroke="currentColor" strokeWidth="1.2"
            className="text-brand/30 mb-4" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round"
              d="M3 3h2l.4 2M7 13h10l4-8H5.4M7 13L5.4 5M7 13l-2.293 2.293c-.63.63-.184 1.707.707 1.707H17m0 0a2 2 0 100 4 2 2 0 000-4zm-8 2a2 2 0 11-4 0 2 2 0 014 0z"/>
          </svg>
          <p className="text-sm font-medium text-ink">Seu carrinho está vazio</p>
          <p className="text-xs text-muted mt-1">Adicione produtos pela Busca ou pelo assistente</p>
        </div>
      ) : (
        <div className="p-4 space-y-3">
          {cart.items.map(item => (
            <CartItemRow
              key={item.itemId}
              item={item}
              onIncrease={handleIncrease}
              onDecrease={handleDecrease}
              onRemove={handleRemove}
            />
          ))}
          <CartSummary cart={cart} />
        </div>
      )}
    </AppShell>
  )
}
