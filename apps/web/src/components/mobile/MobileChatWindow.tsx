'use client'

import { useEffect, useRef, forwardRef } from 'react'
import type { ChatMessage, Product, PaymentMethod, PaymentProvider } from '@/types/ucp'
import type { CustomerDto } from '@/components/ucp/CartCard'
import MobileChatBubble from './MobileChatBubble'
import MobileIntentRenderer from './MobileIntentRenderer'

interface Props {
  messages: ChatMessage[]
  isTyping: boolean
  onAddToCart: (product: Product) => void
  onCheckout: (customer: CustomerDto, shipping: 'standard' | 'express', total: number) => void
  onPayment: (orderId: string, provider: PaymentProvider, method: PaymentMethod) => void
  onPaymentConfirmed: (orderId: string, detail?: string) => void
  onQuantityChange?: (productId: string, qty: number) => void
  onDismiss?: (id: string) => void
}

const MobileChatWindow = forwardRef<HTMLElement, Props>(function MobileChatWindow(
  { messages, isTyping, onAddToCart, onCheckout, onPayment, onPaymentConfirmed, onQuantityChange, onDismiss },
  ref
) {
  const bottomRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages, isTyping])

  return (
    <main
      ref={ref as React.RefObject<HTMLElement>}
      className="flex-1 min-h-0 overflow-y-auto px-4 pt-4 pb-2 space-y-4 scrollbar-none"
    >
      {messages.map(msg => (
        <MobileChatBubble key={msg.id} message={msg}>
          {msg.role === 'bot' && msg.intent && (
            <MobileIntentRenderer
              message={msg}
              onAddToCart={onAddToCart}
              onCheckout={onCheckout}
              onPayment={onPayment}
              onPaymentConfirmed={onPaymentConfirmed}
              onQuantityChange={onQuantityChange}
              onClose={onDismiss ? () => onDismiss(msg.id) : undefined}
            />
          )}
        </MobileChatBubble>
      ))}

      {isTyping && (
        <div className="flex items-start gap-3 max-w-[88%]">
          <div className="w-8 h-8 rounded-full shrink-0 bg-gradient-to-tr from-emerald-500 to-teal-600 flex items-center justify-center text-white text-xs font-bold shadow-md">
            C
          </div>
          <div className="bg-zinc-900 border border-zinc-800 rounded-2xl rounded-tl-none px-4 py-3">
            <div className="flex gap-1.5 items-center">
              <span className="w-1.5 h-1.5 rounded-full bg-emerald-500 animate-bounce [animation-delay:0ms]" />
              <span className="w-1.5 h-1.5 rounded-full bg-emerald-500 animate-bounce [animation-delay:150ms]" />
              <span className="w-1.5 h-1.5 rounded-full bg-emerald-500 animate-bounce [animation-delay:300ms]" />
            </div>
          </div>
        </div>
      )}

      <div ref={bottomRef} />
    </main>
  )
})

export default MobileChatWindow
