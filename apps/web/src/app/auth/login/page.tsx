'use client'

import { useState } from 'react'
import { signIn } from 'next-auth/react'
import { useRouter } from 'next/navigation'

export default function LoginPage() {
  const router = useRouter()
  const [tab,      setTab]      = useState<'login' | 'register'>('login')
  const [name,     setName]     = useState('')
  const [email,    setEmail]    = useState('')
  const [password, setPassword] = useState('')
  const [error,    setError]    = useState('')
  const [loading,  setLoading]  = useState(false)

  const BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5020'

  async function handleCredentials(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    setLoading(true)
    try {
      if (tab === 'register') {
        const res = await fetch(`${BASE_URL}/api/auth/register`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ name, email, password }),
        })
        if (!res.ok) {
          const d = await res.json()
          setError(d.error ?? 'Erro ao cadastrar.')
          setLoading(false)
          return
        }
      }
      const result = await signIn('credentials', { email, password, redirect: false })
      if (result?.error) { setError('Email ou senha inválidos.'); setLoading(false); return }
      router.push('/chat')
    } catch { setError('Erro de conexão.'); setLoading(false) }
  }

  async function handleSSO(provider: string) {
    setLoading(true)
    await signIn(provider, { callbackUrl: '/chat' })
  }

  return (
    <div style={{
      minHeight: '100vh', background: '#09090b',
      display: 'flex', alignItems: 'center', justifyContent: 'center',
      padding: '1rem', fontFamily: 'var(--font-inter, Inter, sans-serif)',
    }}>
      <div style={{
        width: '100%', maxWidth: 400,
        background: '#18181b', borderRadius: 16,
        border: '1px solid #3f3f46', padding: '2rem',
        boxShadow: '0 24px 48px rgba(0,0,0,0.5)',
      }}>
        {/* Logo */}
        <div style={{ textAlign: 'center', marginBottom: '1.5rem' }}>
          <div style={{ fontSize: 32, fontWeight: 800, color: '#f97316', letterSpacing: -1 }}>
            🛒 Comprai
          </div>
          <p style={{ color: '#a1a1aa', fontSize: 13, marginTop: 4 }}>
            Você pede. A IA compra.
          </p>
        </div>

        {/* Tabs */}
        <div style={{
          display: 'flex', background: '#27272a', borderRadius: 8,
          padding: 3, marginBottom: '1.5rem',
        }}>
          {(['login', 'register'] as const).map(t => (
            <button key={t} onClick={() => { setTab(t); setError('') }} style={{
              flex: 1, padding: '6px 0', borderRadius: 6, border: 'none', cursor: 'pointer',
              fontWeight: 600, fontSize: 13, transition: 'all .15s',
              background: tab === t ? '#f97316' : 'transparent',
              color:      tab === t ? '#fff'     : '#a1a1aa',
            }}>
              {t === 'login' ? 'Entrar' : 'Cadastrar'}
            </button>
          ))}
        </div>

        {/* Form */}
        <form onSubmit={handleCredentials} style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
          {tab === 'register' && (
            <input
              type="text" placeholder="Nome completo" value={name}
              onChange={e => setName(e.target.value)} required
              style={inputStyle}
            />
          )}
          <input
            type="email" placeholder="Email" value={email}
            onChange={e => setEmail(e.target.value)} required
            style={inputStyle}
          />
          <input
            type="password" placeholder="Senha" value={password}
            onChange={e => setPassword(e.target.value)} required minLength={6}
            style={inputStyle}
          />
          {error && (
            <p style={{ color: '#f87171', fontSize: 12, margin: 0, textAlign: 'center' }}>{error}</p>
          )}
          <button type="submit" disabled={loading} style={primaryBtnStyle}>
            {loading ? '...' : tab === 'login' ? 'Entrar' : 'Criar conta'}
          </button>
        </form>

        {/* Divider */}
        <div style={{ display: 'flex', alignItems: 'center', gap: 8, margin: '1.25rem 0' }}>
          <div style={{ flex: 1, height: 1, background: '#3f3f46' }} />
          <span style={{ color: '#71717a', fontSize: 12 }}>ou continue com</span>
          <div style={{ flex: 1, height: 1, background: '#3f3f46' }} />
        </div>

        {/* SSO Buttons */}
        <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
          <SsoButton label="Continuar com Google"    icon="G" provider="google"    onClick={handleSSO} disabled={loading} />
          <SsoButton label="Continuar com GitHub"    icon="⌥" provider="github"    onClick={handleSSO} disabled={loading} />
          <SsoButton label="Continuar com Microsoft" icon="⊞" provider="microsoft-entra-id" onClick={handleSSO} disabled={loading} />
        </div>

        <p style={{ color: '#52525b', fontSize: 11, textAlign: 'center', marginTop: '1.5rem' }}>
          Ao entrar você concorda com nossos termos de uso.
        </p>
      </div>
    </div>
  )
}

function SsoButton({ label, icon, provider, onClick, disabled }: {
  label: string; icon: string; provider: string
  onClick: (p: string) => void; disabled: boolean
}) {
  return (
    <button onClick={() => onClick(provider)} disabled={disabled} style={{
      display: 'flex', alignItems: 'center', gap: 10,
      padding: '10px 16px', borderRadius: 8, border: '1px solid #3f3f46',
      background: '#27272a', color: '#f4f4f5', cursor: 'pointer',
      fontSize: 14, fontWeight: 500, width: '100%', transition: 'border-color .15s',
    }}
    onMouseEnter={e => (e.currentTarget.style.borderColor = '#f97316')}
    onMouseLeave={e => (e.currentTarget.style.borderColor = '#3f3f46')}
    >
      <span style={{ width: 20, textAlign: 'center', fontSize: 16 }}>{icon}</span>
      {label}
    </button>
  )
}

const inputStyle: React.CSSProperties = {
  padding: '10px 14px', borderRadius: 8, border: '1px solid #3f3f46',
  background: '#27272a', color: '#f4f4f5', fontSize: 14, outline: 'none',
  width: '100%', boxSizing: 'border-box',
}

const primaryBtnStyle: React.CSSProperties = {
  padding: '11px 0', borderRadius: 8, border: 'none',
  background: '#f97316', color: '#fff', fontWeight: 700,
  fontSize: 15, cursor: 'pointer', width: '100%',
  marginTop: 4, transition: 'opacity .15s',
}
