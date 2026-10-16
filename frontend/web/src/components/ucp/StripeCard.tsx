'use client'

import { useState } from 'react'
import type { PaymentResult } from '@/types/ucp'

interface Props {
  payment: PaymentResult
  onConfirmed: (detail?: string) => void
}

function formatPrice(v: number) {
  return v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
}

export default function StripeCard({ payment, onConfirmed }: Props) {
  const [number, setNumber] = useState('')
  const [expiry, setExpiry] = useState('')
  const [cvc, setCvc] = useState('')
  const [name, setName] = useState('')
  const [loading, setLoading] = useState(false)
  const [paid, setPaid] = useState(payment.status === 'paid')

  function formatCardNumber(val: string) {
    return val.replace(/\D/g, '').slice(0, 16).replace(/(.{4})/g, '$1 ').trim()
  }

  function formatExpiry(val: string) {
    const digits = val.replace(/\D/g, '').slice(0, 4)
    if (digits.length >= 3) return `${digits.slice(0, 2)}/${digits.slice(2)}`
    return digits
  }

  const isTestCard = number.replace(/\s/g, '') === '4242424242424242'
  const isValid = name && number.replace(/\s/g, '').length === 16 && expiry.length === 5 && cvc.length >= 3

  async function handlePay() {
    if (!isValid) return
    setLoading(true)
    // Em produção: confirmar com Stripe via clientSecret
    // Para demo: simula delay e confirma
    await new Promise((r) => setTimeout(r, 1500))
    setLoading(false)
    setPaid(true)
    onConfirmed(number.replace(/\s/g, '').slice(-4))
  }

  if (paid) {
    return (
      <div className="mt-2 bg-green-950/40 border border-green-700/50 rounded-xl p-4 text-center">
        <p className="text-3xl mb-2">✅</p>
        <p className="text-sm font-semibold text-green-400">Pagamento confirmado!</p>
        <p className="text-xs text-zinc-400 font-mono mt-1">#{payment.orderId}</p>
      </div>
    )
  }

  return (
    <div className="mt-2 bg-zinc-800 border border-zinc-700 rounded-xl overflow-hidden">
      {/* header */}
      <div className="px-4 py-3 border-b border-zinc-700 flex items-center justify-between">
        <div className="flex items-center gap-2">
          <span className="text-lg">💳</span>
          <span className="text-sm font-semibold text-zinc-200">Cartão de crédito</span>
        </div>
        <span className="text-base font-bold text-blue-400 font-mono">{formatPrice(payment.amount)}</span>
      </div>

      <div className="p-4 flex flex-col gap-3">
        {/* dica de cartão de teste */}
        <div className="bg-blue-950/40 border border-blue-800/40 rounded-lg px-3 py-2">
          <p className="text-[10px] text-blue-400">
            🧪 Cartão de teste: <span className="font-mono">4242 4242 4242 4242</span> · qualquer data/CVV
          </p>
        </div>

        {/* nome */}
        <div className="flex flex-col gap-1">
          <label className="text-[10px] text-zinc-500">Nome no cartão</label>
          <input
            className="input-field"
            placeholder="João Silva"
            value={name}
            onChange={(e) => setName(e.target.value)}
          />
        </div>

        {/* número */}
        <div className="flex flex-col gap-1">
          <label className="text-[10px] text-zinc-500">Número do cartão</label>
          <div className="relative">
            <input
              className={`input-field pr-16 font-mono ${isTestCard ? 'border-green-600/50' : ''}`}
              placeholder="0000 0000 0000 0000"
              value={number}
              onChange={(e) => setNumber(formatCardNumber(e.target.value))}
            />
            {isTestCard && (
              <span className="absolute right-3 top-1/2 -translate-y-1/2 text-[10px] text-green-400">✓ Teste</span>
            )}
          </div>
        </div>

        {/* validade + cvv */}
        <div className="flex gap-2">
          <div className="flex flex-col gap-1 flex-1">
            <label className="text-[10px] text-zinc-500">Validade</label>
            <input
              className="input-field font-mono"
              placeholder="MM/AA"
              value={expiry}
              onChange={(e) => setExpiry(formatExpiry(e.target.value))}
            />
          </div>
          <div className="flex flex-col gap-1 w-24">
            <label className="text-[10px] text-zinc-500">CVV</label>
            <input
              className="input-field font-mono"
              placeholder="123"
              maxLength={4}
              value={cvc}
              onChange={(e) => setCvc(e.target.value.replace(/\D/g, '').slice(0, 4))}
            />
          </div>
        </div>

        {/* botão pagar */}
        <button
          onClick={handlePay}
          disabled={!isValid || loading}
          className="w-full py-3 rounded-xl text-sm font-semibold
            bg-blue-600 hover:bg-blue-500 active:bg-blue-700
            disabled:bg-zinc-700 disabled:text-zinc-500 disabled:cursor-not-allowed
            text-white transition-colors flex items-center justify-center gap-2"
        >
          {loading ? (
            <>
              <span className="animate-spin">⏳</span> Processando...
            </>
          ) : (
            <>💳 Pagar {formatPrice(payment.amount)}</>
          )}
        </button>

        <p className="text-[10px] text-zinc-600 text-center">
          🔒 Pagamento seguro via Stripe
        </p>
      </div>
    </div>
  )
}
