'use client'

import { useState, useRef, useEffect, useCallback } from 'react'
import { getSessionId } from '@/lib/session'
import { apiSearch } from '@/lib/api'
import type { ChatMessage, Product } from '@/types'
import Button from '@/components/ui/Button'
import ProductCard from '@/components/search/ProductCard'

const SUGGESTIONS = [
  'Preciso de um presente até R$100',
  'Tênis para corrida',
  'Melhor notebook para estudantes',
  'Fones de ouvido Bluetooth',
]

interface Props {
  onAddToCart?: (product: Product) => void
}

export default function HomeChat({ onAddToCart }: Props) {
  const [messages, setMessages]   = useState<ChatMessage[]>([{
    id: '0', role: 'assistant',
    content: "Olá! Sou seu assistente de compras. Me diga o que você procura e vou encontrar as melhores opções para você. 🛍️",
    ts: Date.now(),
  }])
  const [input, setInput]   = useState('')
  const [loading, setLoading] = useState(false)
  const bottomRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages])

  const sendMessage = useCallback(async (text: string) => {
    if (!text.trim() || loading) return
    const sessionId = getSessionId()

    const userMsg: ChatMessage = {
      id: Date.now().toString(), role: 'user', content: text.trim(), ts: Date.now(),
    }
    setMessages(prev => [...prev, userMsg])
    setInput('')
    setLoading(true)

    try {
      const products = await apiSearch(text.trim(), sessionId)
      const assistantMsg: ChatMessage = {
        id: (Date.now() + 1).toString(),
        role: 'assistant',
        content: products.length
          ? `Encontrei ${products.length} opção${products.length > 1 ? 'ões' : ''} para "${text.trim()}":`
          : `Não encontrei nada para "${text.trim()}". Tente outras palavras.`,
        ts: Date.now(),
        products,
      }
      setMessages(prev => [...prev, assistantMsg])
    } catch {
      setMessages(prev => [...prev, {
        id: (Date.now() + 1).toString(), role: 'assistant',
        content: 'Algo deu errado. Tente novamente.',
        ts: Date.now(),
      }])
    } finally {
      setLoading(false)
    }
  }, [loading])

  return (
    <div className="flex flex-col h-full">
      {/* header */}
      <div className="px-4 pt-12 pb-4 bg-white border-b border-border">
        <div className="flex items-baseline gap-2">
          <h1 className="text-xl font-bold text-ink">Comprai</h1>
          <span className="text-[10px] font-medium text-brand bg-brand/10 rounded-full px-2 py-0.5">v{process.env.NEXT_PUBLIC_APP_VERSION ?? 'dev'} · {process.env.NEXT_PUBLIC_COMMIT_SHA ?? 'local'}</span>
        </div>
        <p className="text-xs text-muted mt-0.5">Assistente de Compras com IA</p>
      </div>

      {/* messages */}
      <div className="flex-1 overflow-y-auto px-4 py-4 space-y-4">
        {messages.map(msg => (
          <div key={msg.id} className={msg.role === 'user' ? 'flex justify-end' : 'flex justify-start'}>
            <div className={[
              'max-w-[80%] rounded-2xl px-4 py-3 text-sm',
              msg.role === 'user'
                ? 'bg-brand text-white rounded-tr-sm'
                : 'bg-surface text-ink rounded-tl-sm',
            ].join(' ')}>
              <p>{msg.content}</p>
              {msg.products && msg.products.length > 0 && (
                <div className="mt-3 grid grid-cols-2 gap-2">
                  {msg.products.slice(0, 4).map((p, i) => (
                    <ProductCard key={p.id ?? i} product={p} onAddToCart={onAddToCart} />
                  ))}
                </div>
              )}
            </div>
          </div>
        ))}

        {loading && (
          <div className="flex justify-start">
            <div className="bg-surface rounded-2xl rounded-tl-sm px-4 py-3">
              <div className="flex gap-1 items-center">
                {[0, 1, 2].map(i => (
                  <span key={i} className="w-2 h-2 rounded-full bg-brand/40 animate-bounce"
                    style={{ animationDelay: `${i * 0.15}s` }} />
                ))}
              </div>
            </div>
          </div>
        )}
        <div ref={bottomRef} />
      </div>

      {/* suggestions (only when first message) */}
      {messages.length === 1 && (
        <div className="px-4 pb-2 flex gap-2 overflow-x-auto no-scrollbar">
          {SUGGESTIONS.map(s => (
            <button key={s} onClick={() => sendMessage(s)}
              className="shrink-0 text-xs font-medium bg-brand-light text-brand px-3 py-2 rounded-xl whitespace-nowrap">
              {s}
            </button>
          ))}
        </div>
      )}

      {/* input */}
      <div className="px-4 py-3 bg-white border-t border-border flex gap-2 items-end">
        <textarea
          value={input}
          onChange={e => setInput(e.target.value)}
          onKeyDown={e => { if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); sendMessage(input) } }}
          placeholder="O que você está procurando?"
          rows={1}
          className="flex-1 resize-none rounded-2xl border border-border bg-surface px-4 py-3 text-sm
            text-ink placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-brand/30
            focus:border-brand transition-colors max-h-32 overflow-y-auto"
        />
        <Button
          onClick={() => sendMessage(input)}
          loading={loading}
          disabled={!input.trim()}
          className="shrink-0 !px-4 !py-3"
          aria-label="Send"
        >
          <svg width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2.2" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" d="M12 19l9 2-9-18-9 18 9-2zm0 0v-8"/>
          </svg>
        </Button>
      </div>
    </div>
  )
}
