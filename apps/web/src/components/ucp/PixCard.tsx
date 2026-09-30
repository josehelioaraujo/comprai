'use client'

import { useEffect, useState, useCallback } from 'react'
import Image from 'next/image'
import type { PaymentResult } from '@/types/ucp'

interface Props {
  payment: PaymentResult
  onConfirmed: () => void
}

const PIX_TTL_SECONDS = 15 * 60 // 15 minutos

function formatPrice(v: number) {
  return v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
}

function pad(n: number) {
  return String(n).padStart(2, '0')
}

export default function PixCard({ payment, onConfirmed }: Props) {
  const [seconds, setSeconds] = useState(PIX_TTL_SECONDS)
  const [copied, setCopied] = useState(false)
  const [confirmed, setConfirmed] = useState(payment.status === 'paid')

  // countdown
  useEffect(() => {
    if (confirmed || seconds <= 0) return
    const timer = setInterval(() => setSeconds((s) => s - 1), 1000)
    return () => clearInterval(timer)
  }, [confirmed, seconds])

  // auto-confirma quando chega status paid (polling real viria via SignalR)
  useEffect(() => {
    if (payment.status === 'paid' && !confirmed) {
      setConfirmed(true)
      onConfirmed()
    }
  }, [payment.status, confirmed, onConfirmed])

  const handleCopy = useCallback(async () => {
    if (!payment.pixCopyPaste) return
    await navigator.clipboard.writeText(payment.pixCopyPaste)
    setCopied(true)
    setTimeout(() => setCopied(false), 2000)
  }, [payment.pixCopyPaste])

  const expired = seconds <= 0

  const minutes = Math.floor(seconds / 60)
  const secs = seconds % 60

  // ── pago ──
  if (confirmed) {
    return (
      <div className="mt-2 bg-green-950/40 border border-green-700/50 rounded-xl p-4 text-center">
        <p className="text-3xl mb-2">✅</p>
        <p className="text-sm font-semibold text-green-400">Pagamento confirmado!</p>
        <p className="text-xs text-zinc-400 font-mono mt-1">#{payment.orderId}</p>
      </div>
    )
  }

  // ── expirado ──
  if (expired) {
    return (
      <div className="mt-2 bg-red-950/30 border border-red-700/40 rounded-xl p-4 text-center">
        <p className="text-3xl mb-2">⏰</p>
        <p className="text-sm font-semibold text-red-400">Pix expirado</p>
        <p className="text-xs text-zinc-400 mt-1">Digite "gerar novo pix" para tentar novamente.</p>
      </div>
    )
  }

  return (
    <div className="mt-2 bg-zinc-800 border border-zinc-700 rounded-xl overflow-hidden">
      {/* header */}
      <div className="px-4 py-3 border-b border-zinc-700 flex items-center justify-between">
        <div className="flex items-center gap-2">
          <span className="text-lg">🟢</span>
          <span className="text-sm font-semibold text-zinc-200">Pagar com Pix</span>
        </div>
        <span className="text-base font-bold text-green-400 font-mono">{formatPrice(payment.amount)}</span>
      </div>

      {/* QR Code */}
      <div className="p-4 flex flex-col items-center gap-3">
        {payment.pixQrCode ? (
          <div className="relative w-44 h-44 bg-white rounded-xl p-2">
            <Image
              src={`data:image/png;base64,${payment.pixQrCode}`}
              alt="QR Code Pix"
              fill
              className="object-contain p-1"
            />
          </div>
        ) : (
          // fallback visual quando não há QR (mock)
          <div className="w-44 h-44 bg-white rounded-xl flex items-center justify-center">
            <div className="text-center">
              <p className="text-4xl mb-1">🟩</p>
              <p className="text-xs text-zinc-600">QR Code</p>
              <p className="text-[10px] text-zinc-500">Mock — sem imagem</p>
            </div>
          </div>
        )}

        {/* countdown */}
        <div className="flex items-center gap-2">
          <div className={`w-2 h-2 rounded-full animate-pulse ${seconds < 60 ? 'bg-red-500' : 'bg-orange-400'}`} />
          <span className={`text-sm font-mono font-bold ${seconds < 60 ? 'text-red-400' : 'text-orange-400'}`}>
            {pad(minutes)}:{pad(secs)}
          </span>
          <span className="text-xs text-zinc-500">para expirar</span>
        </div>

        {/* copia e cola */}
        {payment.pixCopyPaste && (
          <div className="w-full">
            <p className="text-[10px] text-zinc-500 mb-1">Pix Copia e Cola</p>
            <div className="flex gap-2">
              <code className="flex-1 bg-zinc-900 border border-zinc-700 rounded-lg px-3 py-2 text-[10px] text-zinc-400 font-mono truncate">
                {payment.pixCopyPaste}
              </code>
              <button
                onClick={handleCopy}
                className="px-3 py-2 rounded-lg text-xs font-semibold bg-zinc-700 hover:bg-zinc-600 text-zinc-200 transition-colors whitespace-nowrap"
              >
                {copied ? '✓ Copiado' : 'Copiar'}
              </button>
            </div>
          </div>
        )}

        {/* botão confirmar manual (demo / mock) */}
        <button
          onClick={() => { setConfirmed(true); onConfirmed() }}
          className="w-full py-2.5 rounded-xl text-sm font-semibold
            bg-green-600 hover:bg-green-500 text-white transition-colors"
        >
          ✅ Simular pagamento confirmado
        </button>
        <p className="text-[10px] text-zinc-600">Em produção, confirmação é automática via webhook</p>
      </div>
    </div>
  )
}
