'use client'

import { useState } from 'react'
import { signIn } from 'next-auth/react'
import { useRouter } from 'next/navigation'

const BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5020'

interface LoginPageProps {
  /** URL para redirecionar após login bem-sucedido */
  callbackUrl?: string
}

export default function LoginPage({ callbackUrl = '/chat' }: LoginPageProps) {
  const router = useRouter()
  const [tab,      setTab]      = useState<'login' | 'register'>('login')
  const [name,     setName]     = useState('')
  const [email,    setEmail]    = useState('')
  const [password, setPassword] = useState('')
  const [showPass, setShowPass] = useState(false)
  const [error,    setError]    = useState('')
  const [loading,  setLoading]  = useState(false)

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
      router.push(callbackUrl)
    } catch { setError('Erro de conexão.'); setLoading(false) }
  }

  async function handleSSO(provider: string) {
    setLoading(true)
    await signIn(provider, { callbackUrl })
  }

  return (
    <div style={{
      minHeight: '100vh',
      background: '#ffffff',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      padding: '1rem',
      fontFamily: 'Inter, -apple-system, BlinkMacSystemFont, sans-serif',
    }}>
      <div style={{
        width: '100%',
        maxWidth: 400,
        display: 'flex',
        flexDirection: 'column',
        gap: '1.25rem',
      }}>
        {/* Logo */}
        <div style={{ textAlign: 'center', marginBottom: '0.5rem' }}>
          <div style={{ fontSize: 28, fontWeight: 800, color: '#111827', letterSpacing: -0.5 }}>
            🛒 Comprai
          </div>
          <p style={{ margin: '6px 0 0', fontSize: 13, color: '#6b7280' }}>
            Você pede. A IA compra.
          </p>
        </div>

        {/* Tabs */}
        <div style={{ display: 'flex', borderBottom: '1px solid #e5e7eb' }}>
          {(['login', 'register'] as const).map(t => (
            <button key={t} onClick={() => { setTab(t); setError('') }} style={{
              flex: 1, padding: '10px 0', border: 'none', background: 'transparent',
              cursor: 'pointer', fontSize: 14, fontWeight: 600, transition: 'all .15s',
              color:        tab === t ? '#7c3aed' : '#9ca3af',
              borderBottom: tab === t ? '2px solid #7c3aed' : '2px solid transparent',
              marginBottom: -1,
            }}>
              {t === 'login' ? 'Entrar' : 'Criar conta'}
            </button>
          ))}
        </div>

        {/* SSO Buttons */}
        <div style={{ display: 'flex', gap: '0.5rem' }}>
          {[
            { provider: 'google',    icon: '𝗚', label: 'Google'    },
            { provider: 'facebook',  icon: '𝗙', label: 'Facebook'  },
            { provider: 'github',    icon: '⌥', label: 'GitHub'    },
          ].map(({ provider, icon, label }) => (
            <button key={provider} onClick={() => handleSSO(provider)} disabled={loading}
              title={`Continuar com ${label}`}
              style={{
                flex: 1, padding: '10px 0', borderRadius: 8,
                border: '1px solid #e5e7eb', background: '#fff',
                cursor: 'pointer', fontSize: 18, transition: 'border-color .15s',
              }}
              onMouseEnter={e => (e.currentTarget.style.borderColor = '#7c3aed')}
              onMouseLeave={e => (e.currentTarget.style.borderColor = '#e5e7eb')}
            >
              {icon}
            </button>
          ))}
        </div>

        {/* Divider */}
        <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
          <div style={{ flex: 1, height: 1, background: '#e5e7eb' }} />
          <span style={{ color: '#9ca3af', fontSize: 12 }}>or</span>
          <div style={{ flex: 1, height: 1, background: '#e5e7eb' }} />
        </div>

        {/* Form */}
        <form onSubmit={handleCredentials} style={{ display: 'flex', flexDirection: 'column', gap: '0.875rem' }}>
          {tab === 'register' && (
            <Field label="Nome completo">
              <input type="text" value={name} onChange={e => setName(e.target.value)}
                required placeholder="Seu nome" style={inputStyle} />
            </Field>
          )}

          <Field label="Email address">
            <input type="email" value={email} onChange={e => setEmail(e.target.value)}
              required placeholder="seu@email.com" style={inputStyle} />
          </Field>

          <Field label="Password">
            <div style={{ position: 'relative' }}>
              <input
                type={showPass ? 'text' : 'password'}
                value={password} onChange={e => setPassword(e.target.value)}
                required minLength={6} placeholder="••••••••"
                style={{ ...inputStyle, paddingRight: 40 }}
              />
              <button type="button" onClick={() => setShowPass(v => !v)} style={{
                position: 'absolute', right: 12, top: '50%', transform: 'translateY(-50%)',
                background: 'none', border: 'none', cursor: 'pointer', color: '#9ca3af', fontSize: 14,
              }}>
                {showPass ? '🙈' : '👁'}
              </button>
            </div>
          </Field>

          {error && (
            <p style={{ color: '#ef4444', fontSize: 12, margin: 0, textAlign: 'center' }}>{error}</p>
          )}

          {tab === 'login' && (
            <div style={{ textAlign: 'right', marginTop: -6 }}>
              <a href="#" style={{ fontSize: 12, color: '#7c3aed', textDecoration: 'none' }}>
                Forgot password?
              </a>
            </div>
          )}

          <button type="submit" disabled={loading} style={{
            padding: '12px 0', borderRadius: 8, border: 'none',
            background: loading ? '#a78bfa' : '#7c3aed',
            color: '#fff', fontWeight: 700, fontSize: 15,
            cursor: loading ? 'not-allowed' : 'pointer',
            transition: 'background .15s', marginTop: 2,
          }}>
            {loading ? '...' : tab === 'login' ? 'Log in' : 'Sign up'}
          </button>
        </form>

        {/* Footer links */}
        <div style={{ textAlign: 'center' }}>
          <a href="#" style={{ fontSize: 12, color: '#6b7280', textDecoration: 'none' }}>
            Can't Access Your Account?
          </a>
        </div>

        <p style={{ color: '#d1d5db', fontSize: 11, textAlign: 'center', margin: 0 }}>
          Ao entrar você concorda com nossos{' '}
          <a href="#" style={{ color: '#9ca3af' }}>termos de uso</a>.
        </p>
      </div>
    </div>
  )
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
      <label style={{ fontSize: 13, fontWeight: 500, color: '#374151' }}>{label}</label>
      {children}
    </div>
  )
}

const inputStyle: React.CSSProperties = {
  width: '100%', padding: '10px 12px', borderRadius: 8,
  border: '1px solid #d1d5db', background: '#fff',
  color: '#111827', fontSize: 14, outline: 'none',
  boxSizing: 'border-box', transition: 'border-color .15s',
}
