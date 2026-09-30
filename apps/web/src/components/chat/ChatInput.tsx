'use client'

import { useState, useRef, KeyboardEvent } from 'react'

interface Props {
  onSend: (text: string) => void
  disabled?: boolean
}

// sugestões de início rápido
const SUGGESTIONS = [
  'Quero tênis Nike até R$400',
  'Procuro fone de ouvido bluetooth',
  'Meus pedidos',
  'O que tenho no carrinho?',
]

export default function ChatInput({ onSend, disabled }: Props) {
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
    onSend(s)
  }

  return (
    <div className="border-t border-zinc-800 bg-zinc-900 px-4 py-3 flex flex-col gap-2">
      {/* sugestões rápidas */}
      <div className="flex gap-2 overflow-x-auto pb-1 scrollbar-none">
        {SUGGESTIONS.map((s) => (
          <button
            key={s}
            onClick={() => handleSuggestion(s)}
            disabled={disabled}
            className="flex-shrink-0 px-3 py-1.5 rounded-full text-xs border border-zinc-700
              text-zinc-400 hover:border-zinc-500 hover:text-zinc-200
              disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
          >
            {s}
          </button>
        ))}
      </div>

      {/* campo de texto */}
      <div className="flex gap-2 items-end">
        <textarea
          ref={inputRef}
          rows={1}
          className="flex-1 resize-none bg-zinc-800 border border-zinc-700 rounded-2xl
            px-4 py-3 text-sm text-zinc-200 placeholder-zinc-600
            focus:outline-none focus:border-zinc-500
            disabled:opacity-40 disabled:cursor-not-allowed
            max-h-32 leading-relaxed"
          placeholder="O que você quer comprar?"
          value={text}
          onChange={(e) => setText(e.target.value)}
          onKeyDown={handleKey}
          disabled={disabled}
          style={{ height: 'auto', minHeight: '48px' }}
          onInput={(e) => {
            const el = e.currentTarget
            el.style.height = 'auto'
            el.style.height = Math.min(el.scrollHeight, 128) + 'px'
          }}
        />
        <button
          onClick={handleSend}
          disabled={!text.trim() || disabled}
          className="w-12 h-12 rounded-2xl bg-green-600 hover:bg-green-500
            disabled:bg-zinc-700 disabled:cursor-not-allowed
            text-white text-lg flex items-center justify-center
            transition-colors flex-shrink-0"
          aria-label="Enviar"
        >
          {disabled ? (
            <span className="animate-spin text-sm">⏳</span>
          ) : (
            '▶'
          )}
        </button>
      </div>
    </div>
  )
}
