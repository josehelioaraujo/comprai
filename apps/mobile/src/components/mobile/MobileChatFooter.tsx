'use client'

import { useState, useRef, KeyboardEvent } from 'react'
import type { UcpStep } from '@/types/ucp'

interface Props {
  onSend: (text: string) => void
  onViewOrders?: () => void
  disabled?: boolean
  cartCount?: number
  onViewCart?: () => void
  step?: UcpStep
}

const SUGGESTIONS_BY_STEP: Partial<Record<UcpStep | 'idle', { label: string; msg: string | null; action?: 'viewOrders' }[]>> = {
  idle:     [{ label: 'Buscar produto', msg: null }],
  search:   [{ label: 'Buscar produto', msg: null }],
  cart:     [{ label: 'Finalizar compra', msg: 'Quero finalizar minha compra' }],
  checkout: [{ label: 'Finalizar compra', msg: 'Quero finalizar minha compra' }],
  payment:  [],
  order:    [
    { label: 'Nova busca',   msg: null },
    { label: 'Meus pedidos', msg: null, action: 'viewOrders' },
  ],
}

export default function MobileChatFooter({ onSend, onViewOrders, disabled, cartCount = 0, onViewCart, step = 'idle' }: Props) {
  const [text, setText] = useState('')
  const textareaRef     = useRef<HTMLTextAreaElement>(null)

  const suggestions = SUGGESTIONS_BY_STEP[step] ?? SUGGESTIONS_BY_STEP['idle']!

  function handleSend() {
    const trimmed = text.trim()
    if (!trimmed || disabled) return
    onSend(trimmed)
    setText('')
    if (textareaRef.current) textareaRef.current.style.height = 'auto'
    textareaRef.current?.focus()
  }

  function handleKey(e: KeyboardEvent<HTMLTextAreaElement>) {
    if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); handleSend() }
  }

  function handleSuggestion(msg: string | null, action?: string) {
    if (disabled) return
    if (action === 'viewOrders') { onViewOrders?.(); return }
    if (msg === null) {
      const trimmed = text.trim()
      if (trimmed) { onSend(trimmed); setText(''); if (textareaRef.current) textareaRef.current.style.height = 'auto' }
      else { textareaRef.current?.focus() }
    } else {
      onSend(msg)
    }
  }

  return (
    <footer className="w-full bg-zinc-950/90 backdrop-blur-lg border-t border-zinc-900 px-3 pt-2 pb-3 flex flex-col gap-2 shrink-0">
      {suggestions.length > 0 && (
        <div className="flex gap-1.5 overflow-x-auto scrollbar-none">
          {suggestions.map(s => (
            <button key={s.label} onClick={() => handleSuggestion(s.msg, s.action)} disabled={disabled}
              className="flex-shrink-0 px-2.5 py-1 bg-zinc-900 border border-zinc-800 text-zinc-400 text-[10px] font-medium rounded-full whitespace-nowrap hover:border-zinc-600 hover:text-zinc-200 active:scale-95 transition-all duration-150 disabled:opacity-30">
              {s.label}
            </button>
          ))}
        </div>
      )}
      <div className="flex items-end gap-2">
        <textarea ref={textareaRef} value={text} onChange={e => setText(e.target.value)}
          onKeyDown={handleKey} disabled={disabled} rows={1}
          placeholder="O que você quer comprar?"
          className="flex-1 resize-none bg-zinc-900 border border-zinc-800 text-zinc-100 placeholder-zinc-600 rounded-2xl px-3.5 py-2 text-[13px] leading-snug focus:outline-none focus:border-zinc-600 transition-colors duration-150 scrollbar-none disabled:opacity-50"
          style={{ minHeight: '38px', maxHeight: '96px' }}
          onInput={e => { const el = e.currentTarget; el.style.height = 'auto'; el.style.height = Math.min(el.scrollHeight, 96) + 'px' }}
        />
        {cartCount > 0 && onViewCart && (
          <button onClick={onViewCart} className="relative w-9 h-9 rounded-full shrink-0 bg-zinc-900 border border-zinc-700 flex items-center justify-center transition-colors hover:border-emerald-600">
            <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" className="w-4 h-4 text-zinc-300">
              <path d="M1 1h2l1.5 7h7l1.5-5H4" />
              <circle cx="6.5" cy="13" r="1" fill="currentColor" stroke="none" />
              <circle cx="11" cy="13" r="1" fill="currentColor" stroke="none" />
            </svg>
            <span className="absolute -top-1 -right-1 w-4 h-4 rounded-full bg-emerald-500 text-black text-[8px] font-bold flex items-center justify-center">
              {cartCount > 9 ? '9+' : cartCount}
            </span>
          </button>
        )}
        <button onClick={handleSend} disabled={disabled || !text.trim()}
          className="w-9 h-9 rounded-full shrink-0 bg-emerald-600 hover:bg-emerald-500 disabled:opacity-30 disabled:cursor-not-allowed flex items-center justify-center transition-all duration-150 active:scale-95">
          <svg viewBox="0 0 16 16" fill="none" stroke="white" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className="w-3.5 h-3.5">
            <path d="M8 13V3M3 8l5-5 5 5" />
          </svg>
        </button>
      </div>
    </footer>
  )
}
