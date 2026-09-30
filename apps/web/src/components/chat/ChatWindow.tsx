'use client'

import { useEffect, useRef, useState } from 'react'
import type { ChatMessage, Product, Address, PaymentMethod, PaymentProvider } from '@/types/ucp'
import IntentRenderer from '@/components/IntentRenderer'

interface Props {
  messages: ChatMessage[]
  isTyping: boolean
  onAddToCart: (product: Product) => void
  onCheckout: (address: Address, shipping: 'standard' | 'express') => void
  onPayment: (orderId: string, provider: PaymentProvider, method: PaymentMethod) => void
  onPaymentConfirmed: (orderId: string) => void
}

function formatTime(date: Date) {
  return date.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })
}

function renderText(text: string) {
  const parts = text.split(/(\*\*[^*]+\*\*)/)
  return parts.map((part, i) =>
    part.startsWith('**') && part.endsWith('**')
      ? <strong key={i}>{part.slice(2, -2)}</strong>
      : part
  )
}

export default function ChatWindow({
  messages, isTyping,
  onAddToCart, onCheckout, onPayment, onPaymentConfirmed,
}: Props) {
  const bottomRef = useRef<HTMLDivElement>(null)
  const [mounted, setMounted] = useState(false)
  const [showLogs, setShowLogs] = useState(false)

  // fix hydration — só renderiza no cliente
  useEffect(() => { setMounted(true) }, [])

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages, isTyping])

  // logs de request/response — filtra mensagens com idempotencyKey
  const logMessages = messages.filter(m => m.idempotencyKey)

  if (!mounted) return null

  return (
    <div className="flex-1 overflow-y-auto px-4 py-4 flex flex-col gap-4 relative">

      {/* botão de logs discreto */}
      <button
        onClick={() => setShowLogs(v => !v)}
        className="fixed bottom-20 right-4 z-50 w-8 h-8 rounded-full bg-zinc-800 border border-zinc-700
          text-zinc-500 hover:text-zinc-300 hover:border-zinc-500 text-xs flex items-center justify-center
          transition-all shadow-lg"
        title="Ver logs de request/response"
      >
        🔍
      </button>

      {/* painel de logs */}
      {showLogs && (
        <div className="fixed bottom-28 right-4 z-50 w-96 max-h-80 bg-zinc-900 border border-zinc-700
          rounded-xl overflow-hidden shadow-2xl flex flex-col">
          <div className="px-3 py-2 border-b border-zinc-800 flex items-center justify-between">
            <span className="text-xs font-semibold text-zinc-300">🔍 Request / Response Logs</span>
            <button onClick={() => setShowLogs(false)} className="text-zinc-500 hover:text-zinc-300 text-xs">✕</button>
          </div>
          <div className="overflow-y-auto p-3 flex flex-col gap-3 text-[10px] font-mono">
            {logMessages.length === 0 && (
              <p className="text-zinc-600">Nenhuma requisição ainda.</p>
            )}
            {logMessages.map(m => (
              <div key={m.id} className={`rounded-lg p-2 border ${
                m.role === 'user'
                  ? 'bg-blue-950/40 border-blue-800/40'
                  : 'bg-zinc-800 border-zinc-700'
              }`}>
                <div className="flex items-center justify-between mb-1">
                  <span className={m.role === 'user' ? 'text-blue-400' : 'text-green-400'}>
                    {m.role === 'user' ? '→ REQUEST' : '← RESPONSE'}
                  </span>
                  <span className="text-zinc-600">{formatTime(m.timestamp)}</span>
                </div>
                {m.role === 'user' && (
                  <div className="text-zinc-400">
                    <span className="text-zinc-500">text: </span>"{m.text}"
                    <br/>
                    <span className="text-zinc-500">key: </span>
                    <span className="text-zinc-600">{m.idempotencyKey?.slice(0,16)}...</span>
                  </div>
                )}
                {m.role === 'bot' && (
                  <div className="text-zinc-400">
                    <span className="text-zinc-500">intent: </span>
                    <span className="text-yellow-400">{m.intent ?? 'unknown'}</span>
                    <br/>
                    <span className="text-zinc-500">msg: </span>"{m.text?.slice(0,60)}"
                    {m.data && (
                      <>
                        <br/>
                        <span className="text-zinc-500">data.type: </span>
                        <span className="text-green-400">{m.data.type}</span>
                        {m.data.type === 'search' && (
                          <> <span className="text-zinc-500">({m.data.products.length} produtos)</span></>
                        )}
                      </>
                    )}
                  </div>
                )}
              </div>
            ))}
          </div>
        </div>
      )}

      {messages.map((msg) => (
        <div key={msg.id} className={`flex flex-col ${msg.role === 'user' ? 'items-end' : 'items-start'}`}>
          <div className={`max-w-[80%] px-4 py-2.5 rounded-2xl text-sm leading-relaxed ${
            msg.role === 'user'
              ? 'bg-blue-600 text-white rounded-br-sm'
              : 'bg-zinc-800 text-zinc-200 rounded-bl-sm border border-zinc-700'
          }`}>
            {renderText(msg.text)}
          </div>

          {msg.role === 'bot' && (
            <div className="w-full max-w-[90%]">
              <IntentRenderer
                message={msg}
                onAddToCart={onAddToCart}
                onCheckout={onCheckout}
                onPayment={onPayment}
                onPaymentConfirmed={onPaymentConfirmed}
              />
            </div>
          )}

          <span className="text-[10px] text-zinc-600 mt-1 px-1">
            {formatTime(msg.timestamp)}
            {msg.idempotencyKey && msg.role === 'user' && (
              <span className="ml-1 text-zinc-700" title={`Key: ${msg.idempotencyKey}`}>· 🔑</span>
            )}
          </span>
        </div>
      ))}

      {isTyping && (
        <div className="flex items-start">
          <div className="bg-zinc-800 border border-zinc-700 rounded-2xl rounded-bl-sm px-4 py-3">
            <div className="flex gap-1 items-center h-4">
              <span className="w-2 h-2 rounded-full bg-zinc-500 animate-bounce [animation-delay:0ms]" />
              <span className="w-2 h-2 rounded-full bg-zinc-500 animate-bounce [animation-delay:150ms]" />
              <span className="w-2 h-2 rounded-full bg-zinc-500 animate-bounce [animation-delay:300ms]" />
            </div>
          </div>
        </div>
      )}

      <div ref={bottomRef} />
    </div>
  )
}
