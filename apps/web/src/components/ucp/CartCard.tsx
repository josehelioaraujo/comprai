'use client'

import { useState } from 'react'
import type { Cart } from '@/types/ucp'

interface Props {
  cart: Cart
  onCheckout: (customer: CustomerDto, shipping: 'standard' | 'express') => void
  onQuantityChange?: (productId: string, qty: number) => void
}

export interface CustomerDto {
  name: string; email: string; phone: string; document: string
}

const UF_LIST = ['AC','AL','AP','AM','BA','CE','DF','ES','GO','MA','MT','MS','MG','PA','PB','PR','PE','PI','RJ','RN','RS','RO','RR','SC','SP','SE','TO']
const EMPTY: CustomerDto = { name: '', email: '', phone: '', document: '' }

function fmt(v: number) { return v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' }) }
function fPhone(v: string) {
  const d = v.replace(/\D/g, '').slice(0, 11)
  if (d.length <= 2) return d
  if (d.length <= 7) return `(${d.slice(0, 2)}) ${d.slice(2)}`
  return `(${d.slice(0, 2)}) ${d.slice(2, 7)}-${d.slice(7)}`
}
function fCPF(v: string) {
  const d = v.replace(/\D/g, '').slice(0, 11)
  if (d.length <= 3) return d
  if (d.length <= 6) return `${d.slice(0, 3)}.${d.slice(3)}`
  if (d.length <= 9) return `${d.slice(0, 3)}.${d.slice(3, 6)}.${d.slice(6)}`
  return `${d.slice(0, 3)}.${d.slice(3, 6)}.${d.slice(6, 9)}-${d.slice(9)}`
}
function fCEP(v: string) {
  const d = v.replace(/\D/g, '').slice(0, 8)
  return d.length <= 5 ? d : `${d.slice(0, 5)}-${d.slice(5)}`
}

export default function CartCard({ cart, onCheckout, onQuantityChange }: Props) {
  const [showForm, setShowForm]     = useState(false)
  const [customer, setCustomer]     = useState<CustomerDto>(EMPTY)
  const [shipping, setShipping]     = useState<'standard' | 'express'>('standard')
  const [cep, setCep]               = useState('')
  const [uf, setUf]                 = useState('')
  const [loadingCep, setLoadingCep] = useState(false)
  const [quantities, setQuantities] = useState<Record<string, number>>({})

  // ── Guard total contra null/undefined ──────────────────────────────────────
  if (!cart || !cart.items) return null

  if (cart.items.length === 0) return (
    <div className="mt-2 rounded-xl p-3 text-sm" style={{ background: 'var(--panel)', border: '1px solid var(--border)', color: 'var(--muted)' }}>
      Seu carrinho está vazio.
    </div>
  )

  const getQty = (id: string, def: number) => quantities[id] ?? def
  function setQty(id: string, qty: number) {
    if (qty < 1) return
    setQuantities(q => ({ ...q, [id]: qty }))
    onQuantityChange?.(id, qty)
  }

  const subtotal    = cart.items.reduce((s, i) => s + i.price * getQty(i.productId, i.quantity), 0)
  const shippingCost = shipping === 'express' ? 29.90 : 15.90
  const total       = subtotal + shippingCost
  const totalItems  = cart.items.reduce((s, i) => s + getQty(i.productId, i.quantity), 0)

  function setField(f: keyof CustomerDto, v: string) { setCustomer(c => ({ ...c, [f]: v })) }

  async function handleCepBlur() {
    const d = cep.replace(/\D/g, '')
    if (d.length !== 8) return
    setLoadingCep(true)
    try {
      const r    = await fetch(`https://viacep.com.br/ws/${d}/json/`)
      const data = await r.json()
      if (!data.erro) setUf(data.uf ?? '')
    } catch { }
    finally { setLoadingCep(false) }
  }

  const isValid = customer.name && customer.email && customer.phone && customer.document

  const FREIGHT_OPTIONS = [
    { value: 'standard', label: 'Padrão (5–8d)', price: 15.90 },
    { value: 'express',  label: 'Expresso (1–2d)', price: 29.90 },
  ]

  return (
    <div className="mt-2 rounded-xl overflow-hidden" style={{ background: 'var(--panel)', border: '1px solid var(--border)' }}>

      {/* header — totalItems sincronizado com qtdes locais */}
      <div className="px-3 py-2 flex items-center justify-between" style={{ borderBottom: '1px solid var(--border)' }}>
        <span className="text-xs font-semibold" style={{ color: 'var(--text)' }}>🛒 Carrinho</span>
        <span className="text-[10px]" style={{ color: 'var(--muted)' }}>
          {totalItems} {totalItems === 1 ? 'item' : 'itens'}
        </span>
      </div>

      {/* itens com botão remover */}
      <div style={{ borderBottom: '1px solid var(--border)' }}>
        {cart.items.map(item => {
          const qty = getQty(item.productId, item.quantity)
          return (
            <div key={item.productId} className="px-3 py-2 flex items-center gap-2">
              <div className="flex-1 min-w-0">
                <p className="text-[11px] truncate" style={{ color: 'var(--text)' }}>{item.title}</p>
                <p className="text-[9px]" style={{ color: 'var(--muted)' }}>{fmt(item.price)}/un</p>
              </div>

              {/* qtde */}
              <div className="flex items-center gap-1 flex-shrink-0">
                <button
                  onClick={() => setQty(item.productId, qty - 1)}
                  disabled={qty <= 1}
                  className="w-5 h-5 rounded flex items-center justify-center text-xs font-bold"
                  style={{ background: 'var(--surface)', color: qty <= 1 ? 'var(--border)' : 'var(--text)', cursor: qty <= 1 ? 'not-allowed' : 'pointer' }}
                >−</button>
                <span className="text-xs w-4 text-center font-mono" style={{ color: 'var(--text)' }}>{qty}</span>
                <button
                  onClick={() => setQty(item.productId, qty + 1)}
                  className="w-5 h-5 rounded flex items-center justify-center text-xs font-bold"
                  style={{ background: 'var(--surface)', color: 'var(--text)', cursor: 'pointer' }}
                >+</button>
              </div>

              <span className="text-xs font-bold font-mono w-16 text-right flex-shrink-0" style={{ color: 'var(--accent)' }}>
                {fmt(item.price * qty)}
              </span>

              {/* botão remover discreto */}
              <button
                onClick={() => setQty(item.productId, 0)}
                title="Remover item"
                className="w-5 h-5 rounded flex items-center justify-center flex-shrink-0 opacity-40 hover:opacity-100 transition-opacity"
                style={{ color: '#f87171' }}
              >
                <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" className="w-3 h-3">
                  <path d="M3 3l10 10M13 3L3 13" />
                </svg>
              </button>
            </div>
          )
        })}
      </div>

      {/* frete — cada opção em linha própria */}
      <div className="px-3 py-2 flex flex-col gap-1.5" style={{ borderBottom: '1px solid var(--border)' }}>
        {FREIGHT_OPTIONS.map(opt => (
          <label key={opt.value} className="flex items-center gap-2 cursor-pointer">
            <div
              onClick={() => setShipping(opt.value as 'standard' | 'express')}
              className="w-3.5 h-3.5 rounded-full border-2 flex items-center justify-center flex-shrink-0"
              style={{ borderColor: 'var(--accent)', background: shipping === opt.value ? 'var(--accent)' : 'transparent' }}
            >
              {shipping === opt.value && <div className="w-1 h-1 rounded-full bg-white" />}
            </div>
            <span className="text-[11px] flex-1" style={{ color: 'var(--text)' }}>{opt.label}</span>
            <span className="text-[11px] font-mono" style={{ color: 'var(--muted)' }}>{fmt(opt.price)}</span>
          </label>
        ))}
      </div>

      {/* totais */}
      <div className="px-3 py-2 flex items-center justify-between" style={{ borderBottom: '1px solid var(--border)' }}>
        <div className="flex flex-col gap-0.5">
          <span className="text-[10px]" style={{ color: 'var(--muted)' }}>Sub: {fmt(subtotal)}</span>
          <span className="text-[10px]" style={{ color: 'var(--muted)' }}>Frete: {fmt(shippingCost)}</span>
        </div>
        <span className="text-sm font-bold font-mono" style={{ color: 'var(--accent)' }}>{fmt(total)}</span>
      </div>

      {/* form / botão */}
      {showForm ? (
        <div className="px-3 pb-3 pt-2 flex flex-col gap-1.5">
          <p className="text-[10px] font-semibold" style={{ color: 'var(--text)' }}>👤 Dados</p>
          <input className="input-field text-xs py-1.5" placeholder="Nome completo" value={customer.name} onChange={e => setField('name', e.target.value)} />
          <input className="input-field text-xs py-1.5" placeholder="E-mail" type="email" value={customer.email} onChange={e => setField('email', e.target.value)} />
          <input className="input-field text-xs py-1.5" placeholder="(11) 99999-9999" value={customer.phone} onChange={e => setField('phone', fPhone(e.target.value))} />
          <input className="input-field text-xs py-1.5" placeholder="000.000.000-00" value={customer.document} onChange={e => setField('document', fCPF(e.target.value))} />
          <div className="flex gap-2 items-center mt-1">
            <input className="input-field text-xs py-1.5 flex-1" placeholder="CEP" value={cep} onChange={e => setCep(fCEP(e.target.value))} onBlur={handleCepBlur} />
            {loadingCep && <span className="text-[9px] animate-pulse" style={{ color: 'var(--muted)' }}>...</span>}
            <select className="input-field text-xs py-1.5 w-20" value={uf} onChange={e => setUf(e.target.value)}>
              <option value="">UF</option>
              {UF_LIST.map(u => <option key={u} value={u}>{u}</option>)}
            </select>
          </div>
          <div className="flex gap-2 mt-1">
            <button
              onClick={() => setShowForm(false)}
              className="flex-1 py-2 rounded-lg text-xs font-medium"
              style={{ background: 'var(--surface)', color: 'var(--muted)' }}
            >← Voltar</button>
            <button
              onClick={() => onCheckout(customer, shipping)}
              disabled={!isValid}
              className="flex-1 py-2 rounded-lg text-xs font-semibold"
              style={{ background: isValid ? 'var(--accent)' : 'var(--surface)', color: isValid ? '#fff' : 'var(--muted)', cursor: isValid ? 'pointer' : 'not-allowed' }}
            >Confirmar pedido →</button>
          </div>
        </div>
      ) : (
        <div className="px-3 pb-3 pt-2">
          <button
            onClick={() => setShowForm(true)}
            className="w-full py-2 rounded-lg text-xs font-semibold"
            style={{ background: 'var(--accent)', color: '#fff' }}
          >Finalizar compra →</button>
        </div>
      )}
    </div>
  )
}
