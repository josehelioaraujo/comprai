'use client'

import { useState } from 'react'
import { useTheme } from '@/lib/theme'
import { useChat } from '@/hooks/useChat'
import UcpProgressBar from '@/components/layout/UcpProgressBar'
import ChatWindow from '@/components/chat/ChatWindow'
import ChatInput from '@/components/chat/ChatInput'

export default function CompraiWidget() {
  const [open, setOpen] = useState(false)
  const { theme } = useTheme()
  const { messages, session, isTyping, sendMessage, handleAddToCart,
    handleCheckout, handlePayment, handlePaymentConfirmed } = useChat()

  const cartCount = session.cart?.items.reduce((s, i) => s + i.quantity, 0) ?? 0

  return (
    <>
      {/* painel do chat */}
      {open && (
        <div className="comprai-widget-panel" data-theme={theme}>
          <div className="flex flex-col h-full" style={{ background: 'var(--bg)' }}>
            {/* header do widget */}
            <div className="flex items-center justify-between px-4 py-3"
              style={{ background: 'var(--panel)', borderBottom: '1px solid var(--border)' }}>
              <div className="flex items-center gap-2">
                <span>🛍️</span>
                <div>
                  <p className="text-sm font-bold" style={{ color: 'var(--text)' }}>Comprai</p>
                  <p className="text-[10px]" style={{ color: 'var(--accent)' }}>● Online</p>
                </div>
              </div>
              <button onClick={() => setOpen(false)}
                className="text-sm px-2 py-1 rounded"
                style={{ color: 'var(--muted)', background: 'var(--surface)' }}>✕</button>
            </div>
            <UcpProgressBar step={session.step} />
            <ChatWindow messages={messages} isTyping={isTyping}
              onAddToCart={handleAddToCart} onCheckout={handleCheckout}
              onPayment={handlePayment} onPaymentConfirmed={handlePaymentConfirmed} />
            <ChatInput onSend={sendMessage} disabled={isTyping} />
          </div>
        </div>
      )}

      {/* botão flutuante */}
      <div className="comprai-widget">
        <button onClick={() => setOpen(o => !o)}
          className="w-14 h-14 rounded-full flex items-center justify-center text-2xl shadow-2xl relative transition-transform hover:scale-110"
          style={{ background: 'var(--accent)', color: '#fff' }}
          title="Abrir Comprai">
          {open ? '✕' : '🛍️'}
          {!open && cartCount > 0 && (
            <span className="absolute -top-1 -right-1 w-5 h-5 rounded-full text-[10px] font-bold flex items-center justify-center"
              style={{ background: '#ef4444', color: '#fff' }}>
              {cartCount}
            </span>
          )}
        </button>
      </div>
    </>
  )
}
