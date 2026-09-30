'use client'

import { useChat } from '@/hooks/useChat'
import Sidebar from '@/components/layout/Sidebar'
import UcpProgressBar from '@/components/layout/UcpProgressBar'
import ChatWindow from '@/components/chat/ChatWindow'
import ChatInput from '@/components/chat/ChatInput'

export default function ChatPage() {
  const {
    messages,
    session,
    isTyping,
    sendMessage,
    handleAddToCart,
    handleCheckout,
    handlePayment,
    handlePaymentConfirmed,
  } = useChat()

  return (
    <div className="flex h-screen bg-zinc-950 text-zinc-100 overflow-hidden">
      {/* sidebar — oculta em mobile */}
      <div className="hidden md:flex">
        <Sidebar
          step={session.step}
          cart={session.cart}
          sessionId={session.sessionId}
        />
      </div>

      {/* área principal do chat */}
      <div className="flex flex-col flex-1 min-w-0">
        {/* header mobile */}
        <div className="md:hidden flex items-center gap-2 px-4 py-3 border-b border-zinc-800 bg-zinc-900">
          <span className="text-xl">🛍️</span>
          <span className="text-sm font-bold text-zinc-100">Comprai</span>
        </div>

        {/* barra de progresso UCP */}
        <UcpProgressBar step={session.step} />

        {/* mensagens */}
        <ChatWindow
          messages={messages}
          isTyping={isTyping}
          onAddToCart={handleAddToCart}
          onCheckout={handleCheckout}
          onPayment={handlePayment}
          onPaymentConfirmed={handlePaymentConfirmed}
        />

        {/* input */}
        <ChatInput onSend={sendMessage} disabled={isTyping} />
      </div>
    </div>
  )
}
