'use client'

import { useRef } from 'react'
import { useChat } from '@/hooks/useChat'
import MobileChatShell      from '@/components/mobile/MobileChatShell'
import MobileUcpHeader      from '@/components/mobile/MobileUcpHeader'
import MobileUcpProgressBar from '@/components/mobile/MobileUcpProgressBar'
import MobileChatWindow     from '@/components/mobile/MobileChatWindow'
import MobileChatFooter     from '@/components/mobile/MobileChatFooter'

const VERSION = process.env.NEXT_PUBLIC_APP_VERSION ?? '1.0.0'

export default function MobilePage() {
  const {
    messages, session, cartCount, isTyping,
    sendMessage, handleAddToCart, handleViewCart, handleViewOrders,
    handleCheckout, handlePayment, handlePaymentConfirmed, handleQuantityChange,
  } = useChat()

  const chatRef = useRef<HTMLDivElement>(null)

  function openCart() { handleViewCart() }

  function scrollToBottom() {
    chatRef.current?.scrollTo({ top: chatRef.current.scrollHeight, behavior: 'smooth' })
  }

  return (
    <MobileChatShell>
      {/* Header fixo no topo — logo + versão */}
      <MobileUcpHeader version={VERSION} />

      {/* Área de mensagens — cresce e scrollável */}
      <MobileChatWindow
        ref={chatRef}
        messages={messages}
        isTyping={isTyping}
        onAddToCart={handleAddToCart}
        onCheckout={handleCheckout}
        onPayment={handlePayment}
        onPaymentConfirmed={handlePaymentConfirmed}
        onQuantityChange={handleQuantityChange}
      />

      {/* Stepper de progresso — fixo acima do footer */}
      <MobileUcpProgressBar
        step={session.step}
        cartCount={cartCount}
        onCartClick={openCart}
        onOrderClick={() => { handleViewOrders(); scrollToBottom() }}
      />

      {/* Footer com input e atalhos */}
      <MobileChatFooter
        onSend={sendMessage}
        disabled={isTyping}
        cartCount={cartCount}
        onViewCart={openCart}
        step={session.step}
        onViewOrders={() => { handleViewOrders(); scrollToBottom() }}
      />
    </MobileChatShell>
  )
}
