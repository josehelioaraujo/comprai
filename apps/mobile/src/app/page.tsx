'use client'

import { useChat } from '@/hooks/useChat'
import MobileChatShell      from '@/components/mobile/MobileChatShell'
import MobileUcpProgressBar from '@/components/mobile/MobileUcpProgressBar'
import MobileChatWindow     from '@/components/mobile/MobileChatWindow'
import MobileChatFooter     from '@/components/mobile/MobileChatFooter'

export default function MobilePage() {
  const {
    messages, session, cartCount, isTyping,
    sendMessage, handleAddToCart, handleViewCart,
    handleCheckout, handlePayment, handlePaymentConfirmed, handleQuantityChange,
  } = useChat()

  function openCart() { handleViewCart() }

  return (
    <MobileChatShell>
      <MobileUcpProgressBar
        step={session.step}
        cartCount={cartCount}
        onCartClick={openCart}
      />
      <MobileChatWindow
        messages={messages}
        isTyping={isTyping}
        onAddToCart={handleAddToCart}
        onCheckout={handleCheckout}
        onPayment={handlePayment}
        onPaymentConfirmed={handlePaymentConfirmed}
        onQuantityChange={handleQuantityChange}
      />
      <MobileChatFooter
        onSend={sendMessage}
        disabled={isTyping}
        cartCount={cartCount}
        onViewCart={openCart}
      />
    </MobileChatShell>
  )
}
