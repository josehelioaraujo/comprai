'use client'

import { useState, useRef, KeyboardEvent } from 'react'

interface Props {
  onSend: (text: string) => void
  disabled?: boolean
  cartCount?: number
  onViewCart?: () => void
}

const SUGGESTIONS = [
  'Quero tênis Nike até R$400',
  'Procuro fone de ouvido bluetooth',
  'Meus pedidos',
  'O que tenho no carrinho?',
]

export default function ChatInput({ onSend, disabled, cartCount = 0, onViewCart }: Props) {
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
    if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); handleSend() }
  }

  return (
    <div className="px-4 py-3 flex flex-col gap-2" style={{ borderTop:'1px solid var(--border)', background:'var(--panel)' }}>
      {/* sugestões */}
      <div className="flex gap-2 overflow-x-auto pb-1 scrollbar-none">
        {SUGGESTIONS.map(s => (
          <button key={s} onClick={() => onSend(s)} disabled={disabled}
            className="flex-shrink-0 px-3 py-1.5 rounded-full text-xs transition-colors"
            style={{ border:'1px solid var(--border)', color:'var(--muted)',
              background:'transparent', opacity: disabled ? 0.4 : 1, cursor: disabled ? 'not-allowed' : 'pointer' }}>
            {s}
          </button>
        ))}
      </div>

      {/* input + botões */}
      <div className="flex gap-2 items-end">
        {/* botão carrinho — visível quando tem itens */}
        {cartCount > 0 && (
          <button onClick={onViewCart}
            className="flex-shrink-0 h-12 px-3 rounded-2xl flex items-center gap-1.5 text-xs font-semibold transition-colors"
            style={{ background:'var(--accent)', color:'#fff', minWidth:80 }}>
            🛒 <span>{cartCount}</span>
            <span className="hidden sm:inline">Fechar pedido</span>
          </button>
        )}

        <textarea ref={inputRef} rows={1}
          className="flex-1 resize-none rounded-2xl px-4 py-3 text-sm leading-relaxed"
          style={{ background:'var(--surface)', border:'1px solid var(--border)', color:'var(--text)',
            outline:'none', maxHeight:128, minHeight:48, opacity: disabled ? 0.4 : 1 }}
          placeholder="O que você quer comprar?"
          value={text} onChange={e => setText(e.target.value)}
          onKeyDown={handleKey} disabled={disabled}
          onInput={e => {
            const el = e.currentTarget
            el.style.height = 'auto'
            el.style.height = Math.min(el.scrollHeight, 128) + 'px'
          }} />

        <button onClick={handleSend} disabled={!text.trim() || disabled}
          className="w-12 h-12 rounded-2xl flex items-center justify-center text-lg flex-shrink-0 transition-colors"
          style={{ background: !text.trim() || disabled ? 'var(--surface)' : 'var(--accent)',
            color: !text.trim() || disabled ? 'var(--muted)' : '#fff' }}>
          {disabled ? <span className="animate-spin text-sm">⏳</span> : '▶'}
        </button>
      </div>
    </div>
  )
}
