'use client'

import { useState, useRef, KeyboardEvent } from 'react'

interface Props {
  onSend: (text: string) => void
  disabled?: boolean
  cartCount?: number
  onViewCart?: () => void
}

const SUGGESTIONS = [
  '🔍 Quero tênis Nike até R$400',
  '🎧 Fone bluetooth',
  '📦 Meus pedidos',
  '🛒 Ver carrinho',
]

export default function MobileChatFooter({ onSend, disabled, cartCount = 0, onViewCart }: Props) {
  const [text, setText] = useState('')
  const inputRef = useRef<HTMLTextAreaElement>(null)

  function handleSend() {
    const trimmed = text.trim()
    if (!trimmed || disabled) return
    onSend(trimmed)
    setText('')
    inputRef.current?.focus()
  }

  function handleKey(e: KeyboardEvent<HTMLTextAreaElement>) {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault()
      handleSend()
    }
  }

  function handleSuggestion(s: string) {
    // Remove emoji prefix antes de enviar
    const clean = s.replace(/^[\p{Emoji}\s]+/u, '').trim()
    onSend(clean)
  }

  return (
    <footer className="
      absolute bottom-0 left-0 right-0
      bg-zinc-950/80 backdrop-blur-lg
      border-t border-zinc-900
      p-3 space-y-2.5
      z-40
    ">
      {/* Tags de atalho rápido */}
      <div className="flex gap-2 overflow-x-auto pb-0.5 scrollbar-none">
        {SUGGESTIONS.map(s => (
          <button
            key={s}
            onClick={() => handleSuggestion(s)}
            disabled={disabled}
            className="
              flex-shrink-0 px-3 py-1.5
              bg-zinc-900 border border-zinc-800
              text-zinc-300 text-[11px] font-medium
              rounded-full whitespace-nowrap
              hover:border-emerald-700 hover:text-emerald-400
              transition-colors duration-150
              disabled:opacity-40
            "
          >
            {s}
          </button>
        ))}
      </div>

      {/* Linha de input */}
      <div className="flex items-end gap-2">
        <textarea
          ref={inputRef}
          value={text}
          onChange={e => setText(e.target.value)}
          onKeyDown={handleKey}
          disabled={disabled}
          rows={1}
          placeholder="O que você quer comprar?"
          className="
            flex-1 resize-none
            bg-zinc-900 border border-zinc-800
            text-zinc-100 placeholder-zinc-600
            rounded-2xl px-4 py-2.5
            text-sm leading-snug
            focus:outline-none focus:border-emerald-700
            transition-colors duration-150
            max-h-28 scrollbar-none
            disabled:opacity-50
          "
          style={{ minHeight: '42px' }}
          onInput={e => {
            const el = e.currentTarget
            el.style.height = 'auto'
            el.style.height = Math.min(el.scrollHeight, 112) + 'px'
          }}
        />

        {/* Botão carrinho — visível quando há itens */}
        {cartCount > 0 && onViewCart && (
          <button
            onClick={onViewCart}
            className="
              relative w-10 h-10 rounded-full
              bg-zinc-900 border border-zinc-700
              flex items-center justify-center
              text-zinc-300 text-base
              hover:border-emerald-600 hover:text-emerald-400
              transition-colors duration-150 shrink-0
            "
          >
            🛒
            <span className="
              absolute -top-1 -right-1
              w-4 h-4 rounded-full
              bg-emerald-500 text-black text-[9px] font-bold
              flex items-center justify-center
            ">
              {cartCount > 9 ? '9+' : cartCount}
            </span>
          </button>
        )}

        {/* Botão enviar */}
        <button
          onClick={handleSend}
          disabled={disabled || !text.trim()}
          className="
            w-10 h-10 rounded-full shrink-0
            bg-emerald-600 hover:bg-emerald-500
            disabled:opacity-40 disabled:cursor-not-allowed
            flex items-center justify-center
            text-white text-base
            transition-all duration-150 active:scale-95
            shadow-md shadow-emerald-900/30
          "
        >
          ↑
        </button>
      </div>
    </footer>
  )
}
