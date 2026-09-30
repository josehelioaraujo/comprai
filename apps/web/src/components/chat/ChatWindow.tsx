'use client'

import { useEffect, useRef, useState } from 'react'
import type { ChatMessage, Product, PaymentMethod, PaymentProvider } from '@/types/ucp'
import type { CustomerDto } from '@/components/ucp/CartCard'
import IntentRenderer from '@/components/IntentRenderer'

interface Props {
  messages: ChatMessage[]
  isTyping: boolean
  onAddToCart: (product: Product) => void
  onCheckout: (customer: CustomerDto, shipping: 'standard' | 'express') => void
  onPayment: (orderId: string, provider: PaymentProvider, method: PaymentMethod) => void
  onPaymentConfirmed: (orderId: string) => void
  onQuantityChange?: (productId: string, qty: number) => void
}

function formatTime(date: Date) {
  return date.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })
}

function renderText(text: string) {
  const parts = text.split(/(\*\*[^*]+\*\*)/)
  return parts.map((part, i) =>
    part.startsWith('**') && part.endsWith('**')
      ? <strong key={i}>{part.slice(2,-2)}</strong> : part)
}

function isErrorMsg(text: string) {
  return text.toLowerCase().startsWith('ops!') || text.toLowerCase().startsWith('erro') ||
    text.toLowerCase().startsWith('não consegui')
}

export default function ChatWindow({
  messages, isTyping,
  onAddToCart, onCheckout, onPayment, onPaymentConfirmed, onQuantityChange,
}: Props) {
  const bottomRef = useRef<HTMLDivElement>(null)
  const [mounted, setMounted] = useState(false)
  const [showLogs, setShowLogs] = useState(false)

  useEffect(() => { setMounted(true) }, [])
  useEffect(() => { bottomRef.current?.scrollIntoView({ behavior: 'smooth' }) }, [messages, isTyping])

  const logMessages = messages.filter(m => m.idempotencyKey)

  if (!mounted) return null

  return (
    <div className="flex-1 overflow-y-auto px-4 py-4 flex flex-col gap-4 relative">
      {/* botão logs */}
      <button onClick={() => setShowLogs(v => !v)}
        className="fixed bottom-20 right-4 z-50 w-8 h-8 rounded-full flex items-center justify-center text-xs shadow-lg transition-all"
        style={{ background:'var(--surface)', border:'1px solid var(--border)', color:'var(--muted)' }}>
        🔍
      </button>

      {/* painel de logs */}
      {showLogs && (
        <div className="fixed bottom-28 right-4 z-50 w-96 max-h-96 rounded-xl overflow-hidden shadow-2xl flex flex-col"
          style={{ background:'var(--panel)', border:'1px solid var(--border)' }}>
          <div className="px-3 py-2 flex items-center justify-between" style={{ borderBottom:'1px solid var(--border)' }}>
            <span className="text-xs font-semibold" style={{ color:'var(--text)' }}>🔍 Request / Response Logs</span>
            <button onClick={() => setShowLogs(false)} className="text-xs" style={{ color:'var(--muted)' }}>✕</button>
          </div>
          <div className="overflow-y-auto p-3 flex flex-col gap-3 text-[10px] font-mono">
            {logMessages.length === 0 && <p style={{ color:'var(--muted)' }}>Nenhuma requisição ainda.</p>}
            {logMessages.map(m => {
              const isError = m.role === 'bot' && isErrorMsg(m.text)
              return (
                <div key={m.id} className="rounded-lg p-2" style={{
                  background: isError ? 'rgba(127,29,29,0.3)' : m.role === 'user' ? 'rgba(37,99,235,0.15)' : 'var(--surface)',
                  border: `1px solid ${isError ? 'rgba(153,27,27,0.5)' : m.role === 'user' ? 'rgba(37,99,235,0.3)' : 'var(--border)'}`,
                }}>
                  <div className="flex items-center justify-between mb-1">
                    <span style={{ color: isError ? '#f87171' : m.role === 'user' ? '#60a5fa' : 'var(--accent)' }}>
                      {isError ? '❌ ERROR' : m.role === 'user' ? '→ REQUEST' : '← RESPONSE'}
                    </span>
                    <span style={{ color:'var(--muted)' }}>{formatTime(m.timestamp)}</span>
                  </div>
                  {m.role === 'user' && (
                    <div style={{ color:'var(--muted)' }}>
                      <span style={{ color:'var(--border)' }}>text: </span>"{m.text}"<br/>
                      <span style={{ color:'var(--border)' }}>key: </span>{m.idempotencyKey?.slice(0,16)}...
                    </div>
                  )}
                  {m.role === 'bot' && !isError && (
                    <div style={{ color:'var(--muted)' }}>
                      <span style={{ color:'var(--border)' }}>intent: </span>
                      <span style={{ color:'#fbbf24' }}>{m.intent ?? 'unknown'}</span><br/>
                      <span style={{ color:'var(--border)' }}>msg: </span>"{m.text?.slice(0,60)}"
                      {m.data && (<><br/><span style={{ color:'var(--border)' }}>data: </span>
                        <span style={{ color:'var(--accent)' }}>{m.data.type}</span>
                        {m.data.type === 'search' && <> ({m.data.products.length} produtos)</>}</>)}
                    </div>
                  )}
                  {isError && <div style={{ color:'#fca5a5' }}>{m.text}</div>}
                </div>
              )
            })}
          </div>
        </div>
      )}

      {messages.map((msg) => (
        <div key={msg.id} className={`flex flex-col ${msg.role === 'user' ? 'items-end' : 'items-start'}`}>
          <div className="max-w-[80%] px-4 py-2.5 rounded-2xl text-sm leading-relaxed"
            style={{
              background: msg.role === 'user' ? 'var(--user-msg)' :
                isErrorMsg(msg.text) ? 'rgba(127,29,29,0.3)' : 'var(--surface)',
              color: msg.role === 'user' ? '#fff' : isErrorMsg(msg.text) ? '#fca5a5' : 'var(--text)',
              border: msg.role === 'bot' ? `1px solid ${isErrorMsg(msg.text) ? 'rgba(153,27,27,0.5)' : 'var(--border)'}` : 'none',
              borderRadius: msg.role === 'user' ? '1rem 1rem 0.25rem 1rem' : '1rem 1rem 1rem 0.25rem',
            }}>
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
                onQuantityChange={onQuantityChange}
              />
            </div>
          )}
          <span className="text-[10px] mt-1 px-1" style={{ color:'var(--muted)' }}>
            {formatTime(msg.timestamp)}
            {msg.idempotencyKey && msg.role === 'user' && (
              <span className="ml-1" style={{ color:'var(--border)' }} title={`Key: ${msg.idempotencyKey}`}>· 🔑</span>
            )}
          </span>
        </div>
      ))}

      {isTyping && (
        <div className="flex items-start">
          <div className="rounded-2xl rounded-bl-sm px-4 py-3"
            style={{ background:'var(--surface)', border:'1px solid var(--border)' }}>
            <div className="flex gap-1 items-center h-4">
              <span className="w-2 h-2 rounded-full animate-bounce [animation-delay:0ms]" style={{ background:'var(--muted)' }} />
              <span className="w-2 h-2 rounded-full animate-bounce [animation-delay:150ms]" style={{ background:'var(--muted)' }} />
              <span className="w-2 h-2 rounded-full animate-bounce [animation-delay:300ms]" style={{ background:'var(--muted)' }} />
            </div>
          </div>
        </div>
      )}
      <div ref={bottomRef} />
    </div>
  )
}
