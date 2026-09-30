'use client'

import { useChat } from '@/hooks/useChat'
import Sidebar from '@/components/layout/Sidebar'
import UcpProgressBar from '@/components/layout/UcpProgressBar'
import ChatWindow from '@/components/chat/ChatWindow'
import ChatInput from '@/components/chat/ChatInput'

export default function ChatPage() {
  const { messages, session, isTyping, sendMessage, handleAddToCart,
    handleCheckout, handlePayment, handlePaymentConfirmed } = useChat()

  return (
    <div className="flex h-screen bg-zinc-950 text-zinc-100 overflow-hidden">
      <div className="hidden md:flex">
        <Sidebar step={session.step} cart={session.cart} sessionId={session.sessionId} />
      </div>
      <div className="flex flex-col flex-1 min-w-0">
        <div className="md:hidden flex items-center gap-2 px-4 py-3 border-b border-zinc-800 bg-zinc-900">
          <span className="text-xl">🛍️</span>
          <span className="text-sm font-bold text-zinc-100">Comprai</span>
        </div>
        <UcpProgressBar step={session.step} />
        <ChatWindow messages={messages} isTyping={isTyping}
          onAddToCart={handleAddToCart} onCheckout={handleCheckout}
          onPayment={handlePayment} onPaymentConfirmed={handlePaymentConfirmed} />
        <ChatInput onSend={sendMessage} disabled={isTyping} />
      </div>
    </div>
  )
}
