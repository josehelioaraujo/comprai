'use client'

import { useState } from 'react'
import { useChat } from '@/hooks/useChat'
import { useTheme } from '@/lib/theme'
import Sidebar from '@/components/layout/Sidebar'
import UcpProgressBar from '@/components/layout/UcpProgressBar'
import ChatWindow from '@/components/chat/ChatWindow'
import ChatInput from '@/components/chat/ChatInput'

const VERSION = process.env.NEXT_PUBLIC_APP_VERSION ?? '0.1.0'
const GIT_SHA = process.env.NEXT_PUBLIC_GIT_SHA ?? 'dev'

export default function ChatPage() {
  const { messages, session, cartCount, isTyping, sendMessage,
    handleAddToCart, handleViewCart, handleCheckout, handlePayment, handlePaymentConfirmed, handleQuantityChange } = useChat()
  const { theme, toggle } = useTheme()
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false)

  return (
    <div className="flex h-screen overflow-hidden" style={{ background:'var(--bg)', color:'var(--text)' }}>
      <div className="hidden md:flex">
        <Sidebar step={session.step} cart={session.cart} sessionId={session.sessionId}
          version={VERSION} gitSha={GIT_SHA}
          collapsed={sidebarCollapsed}
          onToggleCollapse={() => setSidebarCollapsed(c => !c)}
          onViewCart={handleViewCart} />
      </div>
      <div className="flex flex-col flex-1 min-w-0">
        {/* header mobile */}
        <div className="md:hidden flex items-center justify-between px-4 py-3"
          style={{ borderBottom:'1px solid var(--border)', background:'var(--panel)' }}>
          <div className="flex items-center gap-2">
            <span className="text-xl">🛍️</span>
            <div>
              <span className="text-sm font-bold" style={{ color:'var(--text)' }}>Comprai</span>
              <span className="text-[10px] ml-2 font-mono" style={{ color:'var(--muted)' }}>v{VERSION}</span>
            </div>
          </div>
          <div className="flex items-center gap-2">
            {cartCount > 0 && (
              <button onClick={handleViewCart}
                className="text-xs px-2 py-0.5 rounded-full font-bold"
                style={{ background:'var(--accent)', color:'#fff' }}>
                🛒 {cartCount}
              </button>
            )}
            <button onClick={toggle}
              className="w-8 h-8 rounded-lg flex items-center justify-center text-sm"
              style={{ background:'var(--surface)', border:'1px solid var(--border)' }}>
              {theme === 'dark' ? '☀️' : '🌙'}
            </button>
          </div>
        </div>
        <UcpProgressBar step={session.step} cartCount={cartCount} />
        <ChatWindow messages={messages} isTyping={isTyping}
          onAddToCart={handleAddToCart} onCheckout={handleCheckout}
          onPayment={handlePayment} onPaymentConfirmed={handlePaymentConfirmed}
          onQuantityChange={handleQuantityChange} />
        <ChatInput onSend={sendMessage} disabled={isTyping}
          cartCount={cartCount} onViewCart={handleViewCart} />
      </div>
    </div>
  )
}
