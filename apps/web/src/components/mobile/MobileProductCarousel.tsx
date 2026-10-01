'use client'

import { useRef, useState, useEffect } from 'react'
import type { Product } from '@/types/ucp'
import MobileProductCard from './MobileProductCard'

interface Props {
  products: Product[]
  onAddToCart: (product: Product) => void
}

export default function MobileProductCarousel({ products, onAddToCart }: Props) {
  const scrollRef = useRef<HTMLDivElement>(null)
  const [canLeft,  setCanLeft]  = useState(false)
  const [canRight, setCanRight] = useState(false)

  // drag-to-scroll
  const isDragging  = useRef(false)
  const startX      = useRef(0)
  const scrollStart = useRef(0)

  function updateArrows() {
    const el = scrollRef.current
    if (!el) return
    setCanLeft(el.scrollLeft > 4)
    setCanRight(el.scrollLeft < el.scrollWidth - el.clientWidth - 4)
  }

  useEffect(() => {
    updateArrows()
    const el = scrollRef.current
    el?.addEventListener('scroll', updateArrows, { passive: true })
    return () => el?.removeEventListener('scroll', updateArrows)
  }, [products])

  function scroll(dir: 'left' | 'right') {
    scrollRef.current?.scrollBy({ left: dir === 'right' ? 160 : -160, behavior: 'smooth' })
  }

  // Mouse drag handlers (desktop)
  function onMouseDown(e: React.MouseEvent) {
    isDragging.current  = true
    startX.current      = e.pageX
    scrollStart.current = scrollRef.current?.scrollLeft ?? 0
    if (scrollRef.current) scrollRef.current.style.cursor = 'grabbing'
  }

  function onMouseMove(e: React.MouseEvent) {
    if (!isDragging.current || !scrollRef.current) return
    e.preventDefault()
    const dx = e.pageX - startX.current
    scrollRef.current.scrollLeft = scrollStart.current - dx
  }

  function onMouseUp() {
    isDragging.current = false
    if (scrollRef.current) scrollRef.current.style.cursor = 'grab'
  }

  if (!products.length) return null

  return (
    <div className="w-full">
      {/* Contador + setas */}
      <div className="flex items-center justify-between mb-1.5 pr-1">
        <span className="text-[10px] text-zinc-600">
          {products.length} resultado{products.length > 1 ? 's' : ''}
        </span>
        <div className="flex gap-1">
          <button
            onClick={() => scroll('left')}
            disabled={!canLeft}
            className="w-6 h-6 rounded-full bg-zinc-800 border border-zinc-700 flex items-center justify-center text-zinc-300 disabled:opacity-20 hover:bg-zinc-700 transition-colors"
          >
            <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" className="w-3 h-3">
              <path d="M10 4L6 8l4 4" />
            </svg>
          </button>
          <button
            onClick={() => scroll('right')}
            disabled={!canRight}
            className="w-6 h-6 rounded-full bg-zinc-800 border border-zinc-700 flex items-center justify-center text-zinc-300 disabled:opacity-20 hover:bg-zinc-700 transition-colors"
          >
            <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" className="w-3 h-3">
              <path d="M6 4l4 4-4 4" />
            </svg>
          </button>
        </div>
      </div>

      {/* Trilho — touch nativo + drag mouse */}
      <div
        ref={scrollRef}
        className="flex gap-2 overflow-x-auto pb-2 snap-x snap-mandatory scrollbar-none"
        style={{ cursor: 'grab', WebkitOverflowScrolling: 'touch' }}
        onMouseDown={onMouseDown}
        onMouseMove={onMouseMove}
        onMouseUp={onMouseUp}
        onMouseLeave={onMouseUp}
      >
        {products.map(p => (
          <MobileProductCard key={p.id} product={p} onAddToCart={onAddToCart} />
        ))}
        <div className="w-1 shrink-0" />
      </div>
    </div>
  )
}
