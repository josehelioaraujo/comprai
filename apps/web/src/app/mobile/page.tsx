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
    messages, session, cartCount, isTyping, hasPreviousOrder,
    sendMessage, handleAddToCart, handleViewCart, handleViewOrders,
    handleCheckout, handlePayment, handlePaymentConfirmed, handleQuantityChange,
    handleRestorePreviousOrder,
  } = useChat()

  const chatRef = useRef<HTMLDivElement>(null)

  function openCart() { handleViewCart() }

  function scrollToBottom() {
    setTimeout(() => {
      chatRef.current?.scrollTo({ top: chatRef.current.scrollHeight, behavior: 'smooth' })
    }, 80)
  }

  function handleViewOrdersAndScroll() {
    handleViewOrders()
    scrollToBottom()
  }

  return (
    <MobileChatShell>
      <MobileUcpHeader version={VERSION} />

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

      <MobileUcpProgressBar
        step={session.step}
        cartCount={cartCount}
        onCartClick={openCart}
        onOrderClick={handleViewOrdersAndScroll}
      />

      <MobileChatFooter
        onSend={sendMessage}
        disabled={isTyping}
        cartCount={cartCount}
        onViewCart={openCart}
        step={session.step}
        onViewOrders={handleViewOrdersAndScroll}
        hasPreviousOrder={hasPreviousOrder}
        onRestorePreviousOrder={handleRestorePreviousOrder}
      />
    </MobileChatShell>
  )
}
