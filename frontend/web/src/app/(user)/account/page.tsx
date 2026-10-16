'use client'

import { useAuth } from '@/lib/useAuth'
import { useState, useEffect } from 'react'

const BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5020'

interface Address {
  id: string; label: string; zipCode: string; street: string
  number?: string; complement?: string; neighborhood?: string
  city: string; state: string; isDefault: boolean
}

interface CustomerFull {
  id: string; name: string; email: string; provider: string
  avatarUrl?: string; phone?: string; document?: string
  addresses?: Address[]
}

export default function AccountPage() {
  const { user } = useAuth()
  const [customer, setCustomer] = useState<CustomerFull | null>(null)
  const [saving,   setSaving]   = useState(false)
  const [savingAddr, setSavingAddr] = useState(false)
  const [msg,      setMsg]      = useState('')

  // form perfil
  const [name,     setName]     = useState('')
  const [phone,    setPhone]    = useState('')
  const [document, setDocument] = useState('')

  // form endereço
  const [zipCode,       setZipCode]       = useState('')
  const [street,        setStreet]        = useState('')
  const [number,        setNumber]        = useState('')
  const [complement,    setComplement]    = useState('')
  const [neighborhood,  setNeighborhood]  = useState('')
  const [city,          setCity]          = useState('')
  const [state,         setState]         = useState('')

  useEffect(() => {
    fetch(`${BASE_URL}/api/auth/me`, {
      headers: { Authorization: `Bearer ${(user as any)?.backendToken ?? ''}` }
    })
      .then(r => r.ok ? r.json() : null)
      .then((d: CustomerFull | null) => {
        if (!d) return
        setCustomer(d)
        setName(d.name ?? '')
        setPhone(d.phone ?? '')
        setDocument(d.document ?? '')
        const addr = d.addresses?.find(a => a.isDefault)
        if (addr) {
          setZipCode(addr.zipCode ?? ''); setStreet(addr.street ?? '')
          setNumber(addr.number ?? ''); setComplement(addr.complement ?? '')
          setNeighborhood(addr.neighborhood ?? ''); setCity(addr.city ?? '')
          setState(addr.state ?? '')
        }
      })
  }, [user])

  async function handleSaveProfile(e: React.FormEvent) {
    e.preventDefault(); setSaving(true); setMsg('')
    try {
      const r = await fetch(`${BASE_URL}/api/auth/me`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json',
          Authorization: `Bearer ${(user as any)?.backendToken ?? ''}` },
        body: JSON.stringify({ name, phone, document }),
      })
      setMsg(r.ok ? '✅ Perfil atualizado!' : '❌ Erro ao salvar.')
    } catch { setMsg('❌ Erro de conexão.') }
    setSaving(false)
  }

  async function handleSaveAddress(e: React.FormEvent) {
    e.preventDefault(); setSavingAddr(true); setMsg('')
    try {
      const r = await fetch(`${BASE_URL}/api/auth/me/address`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json',
          Authorization: `Bearer ${(user as any)?.backendToken ?? ''}` },
        body: JSON.stringify({ zipCode, street, number, complement, neighborhood, city, state, isDefault: true }),
      })
      setMsg(r.ok ? '✅ Endereço salvo!' : '❌ Erro ao salvar endereço.')
    } catch { setMsg('❌ Erro de conexão.') }
    setSavingAddr(false)
  }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '1.5rem' }}>
      <h1 style={{ fontSize: 22, fontWeight: 700, color: '#f4f4f5', margin: 0 }}>Minha Conta</h1>

      {msg && <p style={{ color: msg.startsWith('✅') ? '#22c55e' : '#f87171', fontSize: 13 }}>{msg}</p>}

      {/* Avatar */}
      <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
        {user?.image
          ? <img src={user.image} alt="" style={{ width: 52, height: 52, borderRadius: '50%' }} />
          : <div style={{ width: 52, height: 52, borderRadius: '50%', background: '#f97316', display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 20, fontWeight: 700, color: '#fff' }}>
              {(name || user?.email || '?')[0].toUpperCase()}
            </div>
        }
        <div>
          <div style={{ fontWeight: 600 }}>{name || user?.name}</div>
          <div style={{ fontSize: 12, color: '#a1a1aa' }}>{user?.email} · {customer?.provider ?? 'credentials'}</div>
        </div>
      </div>

      {/* Formulário perfil */}
      <form onSubmit={handleSaveProfile} style={{ background: '#18181b', border: '1px solid #3f3f46', borderRadius: 12, padding: '1.25rem', display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
        <p style={{ fontWeight: 600, fontSize: 14, margin: 0, color: '#f4f4f5' }}>Dados pessoais</p>
        <Input label="Nome" value={name}     onChange={setName}     required />
        <Input label="Telefone" value={phone} onChange={setPhone}   placeholder="(11) 99999-9999" />
        <Input label="CPF / Documento" value={document} onChange={setDocument} placeholder="000.000.000-00" />
        <button type="submit" disabled={saving} style={btnStyle}>
          {saving ? 'Salvando...' : 'Salvar perfil'}
        </button>
      </form>

      {/* Formulário endereço */}
      <form onSubmit={handleSaveAddress} style={{ background: '#18181b', border: '1px solid #3f3f46', borderRadius: 12, padding: '1.25rem', display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
        <p style={{ fontWeight: 600, fontSize: 14, margin: 0, color: '#f4f4f5' }}>Endereço de entrega</p>
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.75rem' }}>
          <Input label="CEP" value={zipCode} onChange={setZipCode} required placeholder="00000-000" />
          <Input label="Número" value={number} onChange={setNumber} placeholder="123" />
        </div>
        <Input label="Rua" value={street} onChange={setStreet} required />
        <Input label="Complemento" value={complement} onChange={setComplement} placeholder="Apto, Bloco..." />
        <Input label="Bairro" value={neighborhood} onChange={setNeighborhood} />
        <div style={{ display: 'grid', gridTemplateColumns: '1fr auto', gap: '0.75rem' }}>
          <Input label="Cidade" value={city} onChange={setCity} required />
          <Input label="UF" value={state} onChange={setState} required placeholder="SP" style={{ width: 60 }} />
        </div>
        <button type="submit" disabled={savingAddr} style={btnStyle}>
          {savingAddr ? 'Salvando...' : 'Salvar endereço'}
        </button>
      </form>
    </div>
  )
}

function Input({ label, value, onChange, required, placeholder, style: extraStyle }: {
  label: string; value: string; onChange: (v: string) => void
  required?: boolean; placeholder?: string; style?: React.CSSProperties
}) {
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
      <label style={{ fontSize: 12, color: '#a1a1aa' }}>{label}</label>
      <input
        value={value} onChange={e => onChange(e.target.value)}
        required={required} placeholder={placeholder}
        style={{ padding: '8px 12px', borderRadius: 8, border: '1px solid #3f3f46',
          background: '#27272a', color: '#f4f4f5', fontSize: 14, outline: 'none',
          ...extraStyle }}
      />
    </div>
  )
}

const btnStyle: React.CSSProperties = {
  padding: '9px 0', borderRadius: 8, border: 'none',
  background: '#f97316', color: '#fff', fontWeight: 700,
  fontSize: 14, cursor: 'pointer', marginTop: 4,
}
