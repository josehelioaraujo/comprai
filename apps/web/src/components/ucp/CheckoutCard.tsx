'use client'

import type { CheckoutData, PaymentMethod } from '@/types/ucp'

interface Props {
  checkout: CheckoutData
  onPayment: (method: PaymentMethod) => void
}

function formatPrice(v: number) {
  return v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
}

export default function CheckoutCard({ checkout, onPayment }: Props) {
  const shipping = checkout.shippingCost ?? 0
  const total = checkout.total + shipping

  return (
    <div className="mt-2 bg-zinc-800 border border-zinc-700 rounded-xl overflow-hidden">
      {/* header */}
      <div className="px-4 py-3 border-b border-zinc-700">
        <p className="text-sm font-semibold text-zinc-200">📋 Resumo do pedido</p>
        <p className="text-[10px] text-zinc-500 font-mono mt-0.5">#{checkout.orderId}</p>
      </div>

      {/* itens */}
      <div className="px-4 py-3 flex flex-col gap-1 border-b border-zinc-700">
        {checkout.items.map((item) => (
          <div key={item.productId} className="flex justify-between text-xs">
            <span className="text-zinc-300 truncate flex-1 mr-2">{item.title} × {item.quantity}</span>
            <span className="text-zinc-400 font-mono flex-shrink-0">{formatPrice(item.price * item.quantity)}</span>
          </div>
        ))}
      </div>

      {/* endereço */}
      {checkout.address && (
        <div className="px-4 py-3 border-b border-zinc-700">
          <p className="text-[10px] text-zinc-500 uppercase tracking-wide mb-1">Entrega</p>
          <p className="text-xs text-zinc-300">
            {checkout.address.street}, {checkout.address.number}
            {checkout.address.complement ? ` — ${checkout.address.complement}` : ''}
          </p>
          <p className="text-xs text-zinc-400">
            {checkout.address.city}/{checkout.address.state} · CEP {checkout.address.zipCode}
          </p>
        </div>
      )}

      {/* totais */}
      <div className="px-4 py-3 border-b border-zinc-700 flex flex-col gap-1">
        <div className="flex justify-between text-xs text-zinc-400">
          <span>Subtotal</span>
          <span className="font-mono">{formatPrice(checkout.total)}</span>
        </div>
        <div className="flex justify-between text-xs text-zinc-400">
          <span>Frete {checkout.shippingMethod === 'express' ? '(Expresso)' : '(Padrão)'}</span>
          <span className="font-mono">{shipping === 0 ? 'Grátis' : formatPrice(shipping)}</span>
        </div>
        <div className="flex justify-between text-sm font-bold mt-1">
          <span className="text-zinc-200">Total</span>
          <span className="text-green-400 font-mono">{formatPrice(total)}</span>
        </div>
      </div>

      {/* seleção de pagamento */}
      <div className="px-4 py-4 flex flex-col gap-2">
        <p className="text-xs text-zinc-400 mb-1">Como você quer pagar?</p>
        <button
          onClick={() => onPayment('pix')}
          className="w-full py-3 rounded-xl text-sm font-semibold
            bg-green-600/20 border border-green-600/40 text-green-400
            hover:bg-green-600/30 transition-colors flex items-center justify-center gap-2"
        >
          <span className="text-lg">🟢</span> Pix — pagamento instantâneo
        </button>
        <button
          onClick={() => onPayment('credit_card')}
          className="w-full py-3 rounded-xl text-sm font-semibold
            bg-blue-600/20 border border-blue-600/40 text-blue-400
            hover:bg-blue-600/30 transition-colors flex items-center justify-center gap-2"
        >
          <span className="text-lg">💳</span> Cartão de crédito
        </button>
      </div>
    </div>
  )
}
