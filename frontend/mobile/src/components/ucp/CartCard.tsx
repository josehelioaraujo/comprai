'use client'

import { useState } from 'react'
import type { Cart } from '@/types/ucp'

interface Props {
  cart: Cart
  onCheckout: (customer: CustomerDto, shipping: 'standard' | 'express', total: number) => void
  onQuantityChange?: (productId: string, qty: number) => void
}

export interface CustomerDto {
  name: string; email: string; phone: string; document: string
  cep: string; street: string; number: string; complement?: string
  neighborhood: string; city: string; state: string
}

const UF_LIST = ['AC','AL','AP','AM','BA','CE','DF','ES','GO','MA','MT','MS','MG','PA','PB','PR','PE','PI','RJ','RN','RS','RO','RR','SC','SP','SE','TO']
const EMPTY: CustomerDto = {
  name: '', email: '', phone: '', document: '',
  cep: '', street: '', number: '', complement: '',
  neighborhood: '', city: '', state: '',
}

const FREE_SHIPPING_THRESHOLD = 1000
const STANDARD_COST           = 15.90
const EXPRESS_COST            = 29.90

function calcShipping(subtotal: number, method: 'standard' | 'express'): number {
  if (subtotal >= FREE_SHIPPING_THRESHOLD) return 0
  return method === 'express' ? EXPRESS_COST : STANDARD_COST
}

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
  // Aplica máscara XXXXX-XXX
  if (d.length <= 5) return d
  return `${d.slice(0, 5)}-${d.slice(5)}`
}

export default function CartCard({ cart, onCheckout, onQuantityChange }: Props) {
  const [showForm, setShowForm]     = useState(false)
  const [customer, setCustomer]     = useState<CustomerDto>(EMPTY)
  const [shipping, setShipping]     = useState<'standard' | 'express'>('standard')
  const [loadingCep, setLoadingCep] = useState(false)
  const [quantities, setQuantities] = useState<Record<string, number>>({})

  if (!cart || !cart.items) return null
  if (cart.items.length === 0) return (
    <div className="mt-2 rounded-xl p-3 text-sm" style={{ background: 'var(--panel)', border: '1px solid var(--border)', color: 'var(--muted)' }}>
      Seu carrinho está vazio.
    </div>
  )

  const getQty  = (id: string, def: number) => quantities[id] ?? def
  function setQty(id: string, qty: number) {
    if (qty < 1) return
    setQuantities(q => ({ ...q, [id]: qty }))
    onQuantityChange?.(id, qty)
  }

  const subtotal     = cart.items.reduce((s, i) => s + i.price * getQty(i.productId, i.quantity), 0)
  const shippingCost = calcShipping(subtotal, shipping)
  const isFree       = shippingCost === 0
  const total        = subtotal + shippingCost
  const totalItems   = cart.items.reduce((s, i) => s + getQty(i.productId, i.quantity), 0)

  function setField(f: keyof CustomerDto, v: string) { setCustomer(c => ({ ...c, [f]: v })) }

  async function handleCepBlur() {
    const d = customer.cep.replace(/\D/g, '')
    if (d.length !== 8) return
    setLoadingCep(true)
    try {
      const r    = await fetch(`https://viacep.com.br/ws/${d}/json/`)
      const data = await r.json()
      if (!data.erro) {
        setCustomer(c => ({
          ...c,
          street:       data.logradouro ?? '',
          neighborhood: data.bairro     ?? '',
          city:         data.localidade ?? '',
          state:        data.uf         ?? '',
        }))
      }
    } catch { }
    finally { setLoadingCep(false) }
  }

  const isValid = customer.name && customer.email && customer.phone &&
                  customer.document && customer.cep && customer.street &&
                  customer.number && customer.city && customer.state

  const FREIGHT_OPTIONS = [
    { value: 'standard', label: 'Padrão (5–8 dias)', cost: isFree ? 0 : STANDARD_COST },
    { value: 'express',  label: 'Expresso (1–2 dias)', cost: EXPRESS_COST },
  ]

  return (
    <div className="mt-2 rounded-xl overflow-hidden" style={{ background: 'var(--panel)', border: '1px solid var(--border)' }}>

      {/* Header */}
      <div className="px-3 py-2 flex items-center justify-between" style={{ borderBottom: '1px solid var(--border)' }}>
        <span className="text-xs font-semibold" style={{ color: 'var(--text)' }}>🛒 Carrinho</span>
        <span className="text-[10px]" style={{ color: 'var(--muted)' }}>
          {totalItems} {totalItems === 1 ? 'item' : 'itens'}
        </span>
      </div>

      {/* Itens */}
      <div style={{ borderBottom: '1px solid var(--border)' }}>
        {cart.items.map(item => {
          const qty = getQty(item.productId, item.quantity)
          return (
            <div key={item.productId} className="px-3 py-2 flex items-center gap-2">
              <div className="flex-1 min-w-0">
                <p className="text-[11px] truncate" style={{ color: 'var(--text)' }}>{item.title}</p>
                <p className="text-[9px]" style={{ color: 'var(--muted)' }}>{fmt(item.price)}/un</p>
              </div>
              <div className="flex items-center gap-1 flex-shrink-0">
                <button onClick={() => setQty(item.productId, qty - 1)} disabled={qty <= 1}
                  className="w-5 h-5 rounded flex items-center justify-center text-xs font-bold"
                  style={{ background: 'var(--surface)', color: qty <= 1 ? 'var(--border)' : 'var(--text)', cursor: qty <= 1 ? 'not-allowed' : 'pointer' }}>−</button>
                <span className="text-xs w-4 text-center font-mono" style={{ color: 'var(--text)' }}>{qty}</span>
                <button onClick={() => setQty(item.productId, qty + 1)}
                  className="w-5 h-5 rounded flex items-center justify-center text-xs font-bold"
                  style={{ background: 'var(--surface)', color: 'var(--text)', cursor: 'pointer' }}>+</button>
              </div>
              <span className="text-xs font-bold font-mono w-16 text-right flex-shrink-0" style={{ color: 'var(--accent)' }}>
                {fmt(item.price * qty)}
              </span>
              <button onClick={() => setQty(item.productId, 0)} title="Remover"
                className="w-5 h-5 rounded flex items-center justify-center flex-shrink-0 opacity-40 hover:opacity-100 transition-opacity"
                style={{ color: '#f87171' }}>
                <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" className="w-3 h-3">
                  <path d="M3 3l10 10M13 3L3 13" />
                </svg>
              </button>
            </div>
          )
        })}
      </div>

      {/* Frete */}
      <div className="px-3 py-2 flex flex-col gap-1.5" style={{ borderBottom: '1px solid var(--border)' }}>
        {isFree && (
          <span className="text-[10px] font-bold px-2 py-0.5 rounded-full self-start"
            style={{ background: 'rgba(34,197,94,0.15)', color: 'var(--accent)', border: '1px solid rgba(34,197,94,0.3)' }}>
            🎉 FRETE GRÁTIS — pedido acima de {fmt(FREE_SHIPPING_THRESHOLD)}
          </span>
        )}
        {FREIGHT_OPTIONS.map(opt => (
          <label key={opt.value} className="flex items-center gap-2 cursor-pointer">
            <div onClick={() => setShipping(opt.value as 'standard' | 'express')}
              className="w-3.5 h-3.5 rounded-full border-2 flex items-center justify-center flex-shrink-0"
              style={{ borderColor: 'var(--accent)', background: shipping === opt.value ? 'var(--accent)' : 'transparent' }}>
              {shipping === opt.value && <div className="w-1 h-1 rounded-full bg-white" />}
            </div>
            <span className="text-[11px] flex-1" style={{ color: 'var(--text)' }}>{opt.label}</span>
            <span className="text-[11px] font-mono font-semibold"
              style={{ color: isFree && opt.value === 'standard' ? 'var(--accent)' : 'var(--muted)' }}>
              {isFree && opt.value === 'standard' ? 'GRÁTIS' : fmt(opt.cost)}
            </span>
          </label>
        ))}
      </div>

      {/* Totais */}
      <div className="px-3 py-2 flex items-center justify-between" style={{ borderBottom: '1px solid var(--border)' }}>
        <div className="flex flex-col gap-0.5">
          <span className="text-[10px]" style={{ color: 'var(--muted)' }}>Subtotal: {fmt(subtotal)}</span>
          <span className="text-[10px]" style={{ color: 'var(--muted)' }}>
            Frete: {isFree ? <span style={{ color: 'var(--accent)', fontWeight: 600 }}>GRÁTIS</span> : fmt(shippingCost)}
          </span>
        </div>
        <span className="text-sm font-bold font-mono" style={{ color: 'var(--accent)' }}>{fmt(total)}</span>
      </div>

      {/* Form / Botão */}
      {showForm ? (
        <div className="px-3 pb-3 pt-2 flex flex-col gap-1.5">
          <p className="text-[11px] font-semibold mb-0.5" style={{ color: 'var(--text)' }}>📦 Dados de entrega</p>

          {/* Identificação */}
          <input className="input-field text-xs py-1.5" placeholder="Nome completo *" autoComplete="name"
            value={customer.name} onChange={e => setField('name', e.target.value)} />
          <input className="input-field text-xs py-1.5" placeholder="E-mail *" type="email" autoComplete="email"
            value={customer.email} onChange={e => setField('email', e.target.value)} />
          <div className="flex gap-2">
            <input className="input-field text-xs py-1.5 flex-1" placeholder="(11) 99999-9999 *" autoComplete="tel"
              value={customer.phone} onChange={e => setField('phone', fPhone(e.target.value))} />
            <input className="input-field text-xs py-1.5 flex-1" placeholder="CPF *" autoComplete="off"
              value={customer.document} onChange={e => setField('document', fCPF(e.target.value))} />
          </div>

          {/* CEP — linha própria com label */}
          <div className="flex flex-col gap-0.5">
            <label className="text-[9px] uppercase tracking-wider" style={{ color: 'var(--muted)' }}>CEP *</label>
            <div className="flex gap-2 items-center">
              <div className="flex-[2] relative">
                <input
                  className="input-field text-xs py-1.5 w-full"
                  placeholder="00000-000"
                  autoComplete="postal-code"
                  value={customer.cep}
                  onChange={e => setField('cep', fCEP(e.target.value))}
                  onBlur={handleCepBlur}
                  maxLength={9}
                />
                {loadingCep && (
                  <span className="absolute right-2 top-1/2 -translate-y-1/2 text-[9px] animate-pulse" style={{ color: 'var(--muted)' }}>
                    buscando...
                  </span>
                )}
              </div>
              <select className="input-field text-xs py-1.5 w-16" autoComplete="address-level1"
                value={customer.state} onChange={e => setField('state', e.target.value)}>
                <option value="">UF *</option>
                {UF_LIST.map(u => <option key={u} value={u}>{u}</option>)}
              </select>
            </div>
          </div>

          {/* Logradouro + Número */}
          <input className="input-field text-xs py-1.5" placeholder="Logradouro *" autoComplete="street-address"
            value={customer.street} onChange={e => setField('street', e.target.value)} />

          <input className="input-field text-xs py-1.5 w-28" placeholder="Número *" autoComplete="off"
            value={customer.number} onChange={e => setField('number', e.target.value)} />

          {/* Complemento — linha própria com hint */}
          <div className="flex flex-col gap-0.5">
            <input
              className="input-field text-xs py-1.5"
              placeholder="Complemento (Apto, sala, bloco...)"
              autoComplete="off"
              value={customer.complement ?? ''}
              onChange={e => setField('complement', e.target.value)}
            />
            <span className="text-[9px]" style={{ color: 'var(--muted)' }}>Opcional — ex: Apto 42, Bloco B</span>
          </div>

          {/* Bairro + Cidade */}
          <div className="flex gap-2">
            <input className="input-field text-xs py-1.5 flex-1" placeholder="Bairro *" autoComplete="off"
              value={customer.neighborhood} onChange={e => setField('neighborhood', e.target.value)} />
            <input className="input-field text-xs py-1.5 flex-1" placeholder="Cidade *" autoComplete="address-level2"
              value={customer.city} onChange={e => setField('city', e.target.value)} />
          </div>

          {/* Total final */}
          <div className="flex items-center justify-between mt-1 px-1">
            <span className="text-[10px]" style={{ color: 'var(--muted)' }}>Total a pagar:</span>
            <span className="text-sm font-bold font-mono" style={{ color: 'var(--accent)' }}>{fmt(total)}</span>
          </div>

          <button onClick={() => onCheckout(customer, shipping, total)} disabled={!isValid}
            className="w-full py-2 rounded-lg text-xs font-semibold mt-1"
            style={{ background: isValid ? 'var(--accent)' : 'var(--surface)', color: isValid ? '#fff' : 'var(--muted)', cursor: isValid ? 'pointer' : 'not-allowed' }}>
            Confirmar pedido →
          </button>
        </div>
      ) : (
        <div className="px-3 pb-3 pt-2">
          <button onClick={() => setShowForm(true)} className="w-full py-2 rounded-lg text-xs font-semibold"
            style={{ background: 'var(--accent)', color: '#fff' }}>
            Finalizar compra →
          </button>
        </div>
      )}
    </div>
  )
}
