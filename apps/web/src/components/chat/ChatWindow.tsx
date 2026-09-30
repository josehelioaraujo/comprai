'use client'

import { useEffect, useRef } from 'react'
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

// Renderiza markdown simples: **bold**
function renderText(text: string) {
  const parts = text.split(/(\*\*[^*]+\*\*)/)
  return parts.map((part, i) =>
    part.startsWith('**') && part.endsWith('**')
      ? <strong key={i}>{part.slice(2, -2)}</strong>
      : part
  )
}

export default function ChatWindow({
  messages,
  isTyping,
  onAddToCart,
  onCheckout,
  onPayment,
  onPaymentConfirmed,
}: Props) {
  const bottomRef = useRef<HTMLDivElement>(null)

  // scroll automático ao receber nova mensagem
  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages, isTyping])

  return (
    <div className="flex-1 overflow-y-auto px-4 py-4 flex flex-col gap-4">
      {messages.map((msg) => (
        <div key={msg.id} className={`flex flex-col ${msg.role === 'user' ? 'items-end' : 'items-start'}`}>
          {/* balão de texto */}
          <div
            className={`max-w-[80%] px-4 py-2.5 rounded-2xl text-sm leading-relaxed ${
              msg.role === 'user'
                ? 'bg-blue-600 text-white rounded-br-sm'
                : 'bg-zinc-800 text-zinc-200 rounded-bl-sm border border-zinc-700'
            }`}
          >
            {renderText(msg.text)}
          </div>

          {/* componente UCP (só em mensagens do bot com intent) */}
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

          {/* timestamp */}
          <span className="text-[10px] text-zinc-600 mt-1 px-1">
            {formatTime(msg.timestamp)}
            {msg.idempotencyKey && msg.role === 'user' && (
              <span className="ml-1 text-zinc-700" title={`Idempotency: ${msg.idempotencyKey}`}>
                · 🔑
              </span>
            )}
          </span>
        </div>
      ))}

      {/* typing indicator */}
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
