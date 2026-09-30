'use client'

import { useState } from 'react'
import type { Cart } from '@/types/ucp'

interface Props {
  cart: Cart
  onCheckout: (customer: CustomerDto, shipping: 'standard' | 'express') => void
  onQuantityChange?: (productId: string, qty: number) => void
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
  const d = v.replace(/\D/g,'').slice(0,11)
  if (d.length<=2) return d
  if (d.length<=7) return `(${d.slice(0,2)}) ${d.slice(2)}`
  return `(${d.slice(0,2)}) ${d.slice(2,7)}-${d.slice(7)}`
}

function formatCPF(v: string) {
  const d = v.replace(/\D/g,'').slice(0,11)
  if (d.length<=3) return d
  if (d.length<=6) return `${d.slice(0,3)}.${d.slice(3)}`
  if (d.length<=9) return `${d.slice(0,3)}.${d.slice(3,6)}.${d.slice(6)}`
  return `${d.slice(0,3)}.${d.slice(3,6)}.${d.slice(6,9)}-${d.slice(9)}`
}

function formatCEP(v: string) {
  const d = v.replace(/\D/g,'').slice(0,8)
  if (d.length<=5) return d
  return `${d.slice(0,5)}-${d.slice(5)}`
}

export default function CartCard({ cart, onCheckout, onQuantityChange }: Props) {
  const [showForm, setShowForm] = useState(false)
  const [customer, setCustomer] = useState<CustomerDto>(EMPTY)
  const [shipping, setShipping] = useState<'standard' | 'express'>('standard')
  const [cep, setCep] = useState('')
  const [uf, setUf] = useState('')
  const [loadingCep, setLoadingCep] = useState(false)
  const [quantities, setQuantities] = useState<Record<string, number>>({})

  if (cart.items.length === 0) {
    return (
      <div className="mt-2 rounded-xl p-4 text-sm" style={{ background:'var(--panel)', border:'1px solid var(--border)', color:'var(--muted)' }}>
        Seu carrinho está vazio.
      </div>
    )
  }

  function getQty(productId: string, defaultQty: number) {
    return quantities[productId] ?? defaultQty
  }

  function setQty(productId: string, qty: number) {
    if (qty < 1) return
    setQuantities(q => ({ ...q, [productId]: qty }))
    onQuantityChange?.(productId, qty)
  }

  const total = cart.items.reduce((sum, item) => {
    return sum + item.price * getQty(item.productId, item.quantity)
  }, 0)

  const shippingCost = shipping === 'express' ? 29.90 : 15.90

  function setField(field: keyof CustomerDto, value: string) {
    setCustomer(c => ({ ...c, [field]: value }))
  }

  async function handleCepBlur() {
    const digits = cep.replace(/\D/g,'')
    if (digits.length !== 8) return
    setLoadingCep(true)
    try {
      const res = await fetch(`https://viacep.com.br/ws/${digits}/json/`)
      const data = await res.json()
      if (!data.erro) setUf(data.uf ?? '')
    } catch {}
    finally { setLoadingCep(false) }
  }

  const isValid = customer.name && customer.email && customer.phone && customer.document

  return (
    <div className="mt-2 rounded-xl overflow-hidden" style={{ background:'var(--panel)', border:'1px solid var(--border)' }}>
      {/* header */}
      <div className="px-4 py-3 flex items-center justify-between" style={{ borderBottom:'1px solid var(--border)' }}>
        <span className="text-sm font-semibold" style={{ color:'var(--text)' }}>🛒 Seu carrinho</span>
        <span className="text-xs" style={{ color:'var(--muted)' }}>{cart.items.length} {cart.items.length===1?'item':'itens'}</span>
      </div>

      {/* itens com +/- */}
      <div style={{ borderBottom:'1px solid var(--border)' }}>
        {cart.items.map(item => {
          const qty = getQty(item.productId, item.quantity)
          return (
            <div key={item.productId} className="px-4 py-3 flex items-center gap-2">
              <div className="flex-1 min-w-0">
                <p className="text-xs truncate" style={{ color:'var(--text)' }}>{item.title}</p>
                <p className="text-[10px]" style={{ color:'var(--muted)' }}>{formatPrice(item.price)} / un</p>
              </div>
              <div className="flex items-center gap-1 flex-shrink-0">
                <button onClick={() => setQty(item.productId, qty - 1)} disabled={qty <= 1}
                  className="w-6 h-6 rounded flex items-center justify-center text-sm font-bold"
                  style={{ background:'var(--surface)', color: qty<=1 ? 'var(--border)' : 'var(--text)',
                    cursor: qty<=1 ? 'not-allowed' : 'pointer' }}>
                  −
                </button>
                <span className="text-xs w-5 text-center font-mono" style={{ color:'var(--text)' }}>{qty}</span>
                <button onClick={() => setQty(item.productId, qty + 1)}
                  className="w-6 h-6 rounded flex items-center justify-center text-sm font-bold"
                  style={{ background:'var(--surface)', color:'var(--text)', cursor:'pointer' }}>
                  +
                </button>
              </div>
              <span className="text-sm font-bold font-mono flex-shrink-0 w-20 text-right" style={{ color:'var(--accent)' }}>
                {formatPrice(item.price * qty)}
              </span>
            </div>
          )
        })}
      </div>

      {/* frete */}
      <div className="px-4 py-3" style={{ borderBottom:'1px solid var(--border)' }}>
        <p className="text-[10px] mb-2 uppercase tracking-wide" style={{ color:'var(--muted)' }}>Frete</p>
        <div className="flex flex-col gap-1.5">
          {[
            { value:'standard', label:'Padrão (5–8 dias)', price: 15.90 },
            { value:'express',  label:'Expresso (1–2 dias)', price: 29.90 },
          ].map(opt => (
            <label key={opt.value} className="flex items-center gap-2 cursor-pointer">
              <div onClick={() => setShipping(opt.value as any)}
                className="w-4 h-4 rounded-full border-2 flex items-center justify-center flex-shrink-0"
                style={{ borderColor:'var(--accent)', background: shipping===opt.value ? 'var(--accent)' : 'transparent' }}>
                {shipping===opt.value && <div className="w-1.5 h-1.5 rounded-full bg-white" />}
              </div>
              <span className="text-xs flex-1" style={{ color:'var(--text)' }}>{opt.label}</span>
              <span className="text-xs font-mono" style={{ color:'var(--muted)' }}>{formatPrice(opt.price)}</span>
            </label>
          ))}
        </div>
      </div>

      {/* total */}
      <div className="px-4 py-3 flex flex-col gap-1" style={{ borderBottom:'1px solid var(--border)' }}>
        <div className="flex items-center justify-between">
          <span className="text-xs" style={{ color:'var(--muted)' }}>Subtotal</span>
          <span className="text-xs font-mono" style={{ color:'var(--muted)' }}>{formatPrice(total)}</span>
        </div>
        <div className="flex items-center justify-between">
          <span className="text-xs" style={{ color:'var(--muted)' }}>Frete</span>
          <span className="text-xs font-mono" style={{ color:'var(--muted)' }}>{formatPrice(shippingCost)}</span>
        </div>
        <div className="flex items-center justify-between mt-1">
          <span className="text-sm font-semibold" style={{ color:'var(--text)' }}>Total</span>
          <span className="text-base font-bold font-mono" style={{ color:'var(--accent)' }}>
            {formatPrice(total + shippingCost)}
          </span>
        </div>
      </div>

      {/* form de dados / botão finalizar */}
      {showForm ? (
        <div className="px-4 pb-4 pt-3 flex flex-col gap-2">
          <p className="text-xs font-semibold mb-1" style={{ color:'var(--text)' }}>👤 Seus dados</p>
          <input className="input-field" placeholder="Nome completo"
            value={customer.name} onChange={e => setField('name', e.target.value)} />
          <input className="input-field" placeholder="E-mail" type="email"
            value={customer.email} onChange={e => setField('email', e.target.value)} />
          <input className="input-field" placeholder="Telefone (11) 99999-9999"
            value={customer.phone} onChange={e => setField('phone', formatPhone(e.target.value))} />
          <input className="input-field" placeholder="CPF 000.000.000-00"
            value={customer.document} onChange={e => setField('document', formatCPF(e.target.value))} />
          <p className="text-xs font-semibold mt-2 mb-1" style={{ color:'var(--text)' }}>📦 Entrega</p>
          <div className="flex gap-2 items-center">
            <input className="input-field flex-1" placeholder="CEP 00000-000"
              value={cep} onChange={e => setCep(formatCEP(e.target.value))} onBlur={handleCepBlur} />
            {loadingCep && <span className="text-[10px] animate-pulse" style={{ color:'var(--muted)' }}>buscando...</span>}
          </div>
          <select className="input-field" value={uf} onChange={e => setUf(e.target.value)}>
            <option value="">Estado (UF)</option>
            {UF_LIST.map(u => <option key={u} value={u}>{u}</option>)}
          </select>
          <button onClick={() => onCheckout(customer, shipping)} disabled={!isValid}
            className="mt-2 w-full py-2.5 rounded-lg text-sm font-semibold transition-colors"
            style={{ background: isValid ? 'var(--accent)' : 'var(--surface)',
              color: isValid ? '#fff' : 'var(--muted)', cursor: isValid ? 'pointer' : 'not-allowed' }}>
            Confirmar pedido →
          </button>
        </div>
      ) : (
        <div className="px-4 pb-4 pt-3">
          <button onClick={() => setShowForm(true)}
            className="w-full py-2.5 rounded-lg text-sm font-semibold"
            style={{ background:'var(--accent)', color:'#fff' }}>
            Finalizar compra →
          </button>
        </div>
      )}
    </div>
  )
}
