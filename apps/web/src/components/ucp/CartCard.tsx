'use client'

import { useState } from 'react'
import type { Cart } from '@/types/ucp'

interface Props {
  cart: Cart
  onCheckout: (customer: CustomerDto, shipping: 'standard' | 'express') => void
}

export interface CustomerDto {
  name: string
  email: string
  phone: string
  document: string
}

const UF_LIST = [
  'AC','AL','AP','AM','BA','CE','DF','ES','GO','MA','MT','MS','MG',
  'PA','PB','PR','PE','PI','RJ','RN','RS','RO','RR','SC','SP','SE','TO'
]

const EMPTY: CustomerDto = { name:'', email:'', phone:'', document:'' }

function formatPrice(v: number) {
  return v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
}

function formatPhone(v: string) {
  const d = v.replace(/\D/g, '').slice(0,11)
  if (d.length <= 2) return d
  if (d.length <= 7) return `(${d.slice(0,2)}) ${d.slice(2)}`
  return `(${d.slice(0,2)}) ${d.slice(2,7)}-${d.slice(7)}`
}

function formatCPF(v: string) {
  const d = v.replace(/\D/g, '').slice(0,11)
  if (d.length <= 3) return d
  if (d.length <= 6) return `${d.slice(0,3)}.${d.slice(3)}`
  if (d.length <= 9) return `${d.slice(0,3)}.${d.slice(3,6)}.${d.slice(6)}`
  return `${d.slice(0,3)}.${d.slice(3,6)}.${d.slice(6,9)}-${d.slice(9)}`
}

function formatCEP(v: string) {
  const d = v.replace(/\D/g, '').slice(0,8)
  if (d.length <= 5) return d
  return `${d.slice(0,5)}-${d.slice(5)}`
}

export default function CartCard({ cart, onCheckout }: Props) {
  const [showForm, setShowForm] = useState(false)
  const [customer, setCustomer] = useState<CustomerDto>(EMPTY)
  const [shipping, setShipping] = useState<'standard' | 'express'>('standard')
  const [cep, setCep] = useState('')
  const [uf, setUf] = useState('')
  const [loadingCep, setLoadingCep] = useState(false)

  if (cart.items.length === 0) {
    return (
      <div className="mt-2 bg-zinc-800 border border-zinc-700 rounded-xl p-4 text-sm text-zinc-400">
        Seu carrinho está vazio.
      </div>
    )
  }

  function setField(field: keyof CustomerDto, value: string) {
    setCustomer(c => ({ ...c, [field]: value }))
  }

  // preparado para API de CEP (ViaCEP)
  async function handleCepBlur() {
    const digits = cep.replace(/\D/g, '')
    if (digits.length !== 8) return
    setLoadingCep(true)
    try {
      const res = await fetch(`https://viacep.com.br/ws/${digits}/json/`)
      const data = await res.json()
      if (!data.erro) {
        setUf(data.uf ?? '')
        // cidade disponível: data.localidade — pode preencher campo cidade quando adicionarmos
      }
    } catch {}
    finally { setLoadingCep(false) }
  }

  const isValid = customer.name && customer.email && customer.phone && customer.document

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

      {showForm ? (
        <div className="px-4 pb-4 border-t border-zinc-700 pt-3 flex flex-col gap-2">
          <p className="text-xs font-semibold text-zinc-300 mb-1">👤 Seus dados</p>

          <input className="input-field" placeholder="Nome completo"
            value={customer.name} onChange={e => setField('name', e.target.value)} />
          <input className="input-field" placeholder="E-mail" type="email"
            value={customer.email} onChange={e => setField('email', e.target.value)} />
          <input className="input-field" placeholder="Telefone (11) 99999-9999"
            value={customer.phone}
            onChange={e => setField('phone', formatPhone(e.target.value))} />
          <input className="input-field" placeholder="CPF 000.000.000-00"
            value={customer.document}
            onChange={e => setField('document', formatCPF(e.target.value))} />

          <p className="text-xs font-semibold text-zinc-300 mt-2 mb-1">📦 Endereço de entrega</p>

          {/* CEP com busca automática (ViaCEP) */}
          <div className="flex gap-2 items-center">
            <input className="input-field flex-1" placeholder="CEP 00000-000"
              value={cep}
              onChange={e => setCep(formatCEP(e.target.value))}
              onBlur={handleCepBlur} />
            {loadingCep && <span className="text-[10px] text-zinc-500 animate-pulse">buscando...</span>}
          </div>
          <p className="text-[10px] text-zinc-600">Digite o CEP para preencher UF automaticamente</p>

          {/* UF dropdown */}
          <select
            className="input-field"
            value={uf}
            onChange={e => setUf(e.target.value)}
          >
            <option value="">Selecione o estado (UF)</option>
            {UF_LIST.map(u => <option key={u} value={u}>{u}</option>)}
          </select>

          {/* frete */}
          <div className="flex gap-2 mt-1">
            <button onClick={() => setShipping('standard')}
              className={`flex-1 py-2 rounded-lg text-xs font-medium border transition-colors ${
                shipping === 'standard'
                  ? 'border-blue-500 bg-blue-500/10 text-blue-400'
                  : 'border-zinc-600 text-zinc-400 hover:border-zinc-500'}`}>
              📦 Padrão (5–8 dias)
            </button>
            <button onClick={() => setShipping('express')}
              className={`flex-1 py-2 rounded-lg text-xs font-medium border transition-colors ${
                shipping === 'express'
                  ? 'border-blue-500 bg-blue-500/10 text-blue-400'
                  : 'border-zinc-600 text-zinc-400 hover:border-zinc-500'}`}>
              ⚡ Expresso (1–2 dias)
            </button>
          </div>

          <div className="flex gap-2 mt-2">
            <button onClick={() => setShowForm(false)}
              className="flex-1 py-2 rounded-lg text-xs text-zinc-400 border border-zinc-600
                hover:border-zinc-400 hover:text-zinc-200 transition-colors">
              ← Voltar
            </button>
            <button onClick={() => onCheckout(customer, shipping)}
              disabled={!isValid}
              className="flex-1 py-2 rounded-lg text-xs font-semibold bg-green-600 hover:bg-green-500
                disabled:bg-zinc-700 disabled:text-zinc-500 disabled:cursor-not-allowed
                text-white transition-colors">
              Confirmar pedido →
            </button>
          </div>
        </div>
      ) : (
        <div className="px-4 pb-4">
          <button onClick={() => setShowForm(true)}
            className="w-full py-2.5 rounded-lg text-sm font-semibold bg-green-600 hover:bg-green-500 text-white transition-colors">
            Finalizar compra →
          </button>
        </div>
      )}
    </div>
  )
}
