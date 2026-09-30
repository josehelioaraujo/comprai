'use client'

import { useState } from 'react'
import type { Cart, Address } from '@/types/ucp'

interface Props {
  cart: Cart
  onCheckout: (address: Address, shipping: 'standard' | 'express') => void
}

const EMPTY_ADDRESS: Address = {
  name: '',
  street: '',
  number: '',
  complement: '',
  city: '',
  state: '',
  zipCode: '',
}

function formatPrice(v: number) {
  return v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
}

export default function CartCard({ cart, onCheckout }: Props) {
  const [showForm, setShowForm] = useState(false)
  const [address, setAddress] = useState<Address>(EMPTY_ADDRESS)
  const [shipping, setShipping] = useState<'standard' | 'express'>('standard')

  if (cart.items.length === 0) {
    return (
      <div className="mt-2 bg-zinc-800 border border-zinc-700 rounded-xl p-4 text-sm text-zinc-400">
        Seu carrinho está vazio.
      </div>
    )
  }

  function handleField(field: keyof Address, value: string) {
    setAddress((a) => ({ ...a, [field]: value }))
  }

  function isAddressValid() {
    return address.name && address.street && address.number && address.city && address.state && address.zipCode
  }

  return (
    <div className="mt-2 bg-zinc-800 border border-zinc-700 rounded-xl overflow-hidden">
      {/* header */}
      <div className="px-4 py-3 border-b border-zinc-700 flex items-center justify-between">
        <span className="text-sm font-semibold text-zinc-200">🛒 Seu carrinho</span>
        <span className="text-xs text-zinc-400">{cart.items.length} {cart.items.length === 1 ? 'item' : 'itens'}</span>
      </div>

      {/* itens */}
      <div className="divide-y divide-zinc-700/50">
        {cart.items.map((item) => (
          <div key={item.productId} className="px-4 py-3 flex items-center justify-between gap-2">
            <div className="flex-1 min-w-0">
              <p className="text-xs text-zinc-200 truncate">{item.title}</p>
              <p className="text-[10px] text-zinc-500">Qtd: {item.quantity}</p>
            </div>
            <span className="text-sm font-bold text-green-400 font-mono flex-shrink-0">
              {formatPrice(item.price * item.quantity)}
            </span>
          </div>
        ))}
      </div>

      {/* total */}
      <div className="px-4 py-3 border-t border-zinc-700 flex items-center justify-between">
        <span className="text-sm text-zinc-400">Total</span>
        <span className="text-base font-bold text-green-400 font-mono">{formatPrice(cart.total)}</span>
      </div>

      {/* form de endereço */}
      {showForm ? (
        <div className="px-4 pb-4 border-t border-zinc-700 pt-3 flex flex-col gap-2">
          <p className="text-xs font-semibold text-zinc-300 mb-1">📦 Endereço de entrega</p>

          <input
            className="input-field"
            placeholder="Nome completo"
            value={address.name}
            onChange={(e) => handleField('name', e.target.value)}
          />
          <div className="flex gap-2">
            <input
              className="input-field flex-1"
              placeholder="Rua / Avenida"
              value={address.street}
              onChange={(e) => handleField('street', e.target.value)}
            />
            <input
              className="input-field w-20"
              placeholder="Nº"
              value={address.number}
              onChange={(e) => handleField('number', e.target.value)}
            />
          </div>
          <input
            className="input-field"
            placeholder="Complemento (opcional)"
            value={address.complement}
            onChange={(e) => handleField('complement', e.target.value)}
          />
          <div className="flex gap-2">
            <input
              className="input-field flex-1"
              placeholder="Cidade"
              value={address.city}
              onChange={(e) => handleField('city', e.target.value)}
            />
            <input
              className="input-field w-16"
              placeholder="UF"
              maxLength={2}
              value={address.state}
              onChange={(e) => handleField('state', e.target.value.toUpperCase())}
            />
          </div>
          <input
            className="input-field"
            placeholder="CEP (00000-000)"
            value={address.zipCode}
            onChange={(e) => handleField('zipCode', e.target.value)}
          />

          {/* frete */}
          <div className="flex gap-2 mt-1">
            <button
              onClick={() => setShipping('standard')}
              className={`flex-1 py-2 rounded-lg text-xs font-medium border transition-colors ${
                shipping === 'standard'
                  ? 'border-blue-500 bg-blue-500/10 text-blue-400'
                  : 'border-zinc-600 text-zinc-400 hover:border-zinc-500'
              }`}
            >
              📦 Padrão (5–8 dias)
            </button>
            <button
              onClick={() => setShipping('express')}
              className={`flex-1 py-2 rounded-lg text-xs font-medium border transition-colors ${
                shipping === 'express'
                  ? 'border-blue-500 bg-blue-500/10 text-blue-400'
                  : 'border-zinc-600 text-zinc-400 hover:border-zinc-500'
              }`}
            >
              ⚡ Expresso (1–2 dias)
            </button>
          </div>

          <div className="flex gap-2 mt-1">
            <button
              onClick={() => setShowForm(false)}
              className="flex-1 py-2 rounded-lg text-xs text-zinc-400 border border-zinc-600 hover:border-zinc-500 transition-colors"
            >
              Voltar
            </button>
            <button
              onClick={() => onCheckout(address, shipping)}
              disabled={!isAddressValid()}
              className="flex-1 py-2 rounded-lg text-xs font-semibold bg-green-600 hover:bg-green-500
                disabled:bg-zinc-700 disabled:text-zinc-500 disabled:cursor-not-allowed
                text-white transition-colors"
            >
              Confirmar pedido →
            </button>
          </div>
        </div>
      ) : (
        <div className="px-4 pb-4 flex gap-2">
          <button
            onClick={() => setShowForm(true)}
            className="flex-1 py-2.5 rounded-lg text-sm font-semibold bg-green-600 hover:bg-green-500 text-white transition-colors"
          >
            Finalizar compra →
          </button>
        </div>
      )}
    </div>
  )
}
