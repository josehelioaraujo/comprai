'use client'

import { useState } from 'react'
import { useChat } from '@/hooks/useChat'
import MobileChatShell from '@/components/mobile/MobileChatShell'
import MobileUcpProgressBar from '@/components/mobile/MobileUcpProgressBar'
import MobileChatWindow from '@/components/mobile/MobileChatWindow'
import MobileChatFooter from '@/components/mobile/MobileChatFooter'

export default function MobilePage() {
  const {
    messages, session, cartCount, isTyping,
    sendMessage, handleAddToCart, handleViewCart,
    handleCheckout, handlePayment, handlePaymentConfirmed, handleQuantityChange
  } = useChat()

  const [cartPopupOpen, setCartPopupOpen] = useState(false)

  function openCart() {
    setCartPopupOpen(true)
    handleViewCart?.()
  }

  return (
    <MobileChatShell>
      {/* Stepper */}
      <MobileUcpProgressBar
        step={session.step}
        cartCount={cartCount}
        onCartClick={openCart}
      />

      {/* Mensagens */}
      <MobileChatWindow
        messages={messages}
        isTyping={isTyping}
        onAddToCart={handleAddToCart}
        onCheckout={handleCheckout}
        onPayment={handlePayment}
        onPaymentConfirmed={handlePaymentConfirmed}
        onQuantityChange={handleQuantityChange}
      />

      {/* Input fixo */}
      <MobileChatFooter
        onSend={sendMessage}
        disabled={isTyping}
        cartCount={cartCount}
        onViewCart={openCart}
      />
    </MobileChatShell>
  )
}
