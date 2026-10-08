'use client'

import { useState, useEffect, Suspense } from 'react'
import { useRouter, useSearchParams } from 'next/navigation'
import { signIn } from 'next-auth/react'

const BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5020'

function TotpVerifyContent() {
  const router       = useRouter()
  const searchParams = useSearchParams()

  const [tempToken, setTempToken] = useState('')
  const [code,      setCode]      = useState('')
  const [status,    setStatus]    = useState<'idle' | 'verifying' | 'done' | 'error'>('idle')
  const [message,   setMessage]   = useState('')

  // tempToken pode vir por query string (?t=...) ou sessionStorage
  useEffect(() => {
    const fromQuery   = searchParams.get('t') ?? ''
    const fromStorage = typeof sessionStorage !== 'undefined'
      ? sessionStorage.getItem('2fa_temp_token') ?? ''
      : ''
    setTempToken(fromQuery || fromStorage)
  }, [searchParams])

  async function verify(e: React.FormEvent) {
    e.preventDefault()
    setStatus('verifying')
    setMessage('')

    try {
      const res = await fetch(`${BASE_URL}/api/auth/2fa/verify`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ tempToken, code }),
      })
      const d = await res.json()

      if (res.ok) {
        // Salva o token no next-auth via credentials callback
        const result = await signIn('credentials', {
          redirect: false,
          token:    d.token,
          customer: JSON.stringify(d.customer),
        })
        if (result?.ok) {
          sessionStorage.removeItem('2fa_temp_token')
          setStatus('done')
          setTimeout(() => router.push('/chat'), 1200)
        } else {
          setStatus('error')
          setMessage('Erro ao iniciar sessão. Tente fazer login novamente.')
        }
      } else {
        setStatus('error')
        setMessage(d.error ?? 'Código inválido ou expirado.')
      }
    } catch {
      setStatus('error')
      setMessage('Erro de conexão. Tente novamente.')
    }
  }

  if (!tempToken) {
    return (
      <div style={{ textAlign: 'center', paddingTop: '2rem' }}>
        <p style={{ color: '#f87171', fontSize: 14 }}>
          Sessão de 2FA inválida ou expirada.{' '}
          <a href="/auth/login" style={{ color: '#a5b4fc' }}>Fazer login</a>
        </p>
      </div>
    )
  }

  return (
    <div style={{
      minHeight: '100vh', background: '#0f0f1a',
      display: 'flex', alignItems: 'center', justifyContent: 'center',
      padding: '1rem', fontFamily: 'Inter, -apple-system, sans-serif',
    }}>
      <div style={{ width: '100%', maxWidth: 420, display: 'flex', flexDirection: 'column', gap: '1.5rem' }}>

        {/* Logo */}
        <div style={{ textAlign: 'center' }}>
          <div style={{ fontSize: 26, fontWeight: 800, color: '#ffffff', letterSpacing: -0.5 }}>
            🛒 Comprai
          </div>
          <p style={{ margin: '6px 0 0', fontSize: 13, color: '#6b7280' }}>
            Verificação em dois fatores
          </p>
        </div>

        {/* Card */}
        <div style={{
          background: '#1a1a2e', borderRadius: 16, padding: '2rem',
          border: '1px solid #2d2d4a',
        }}>
          <h2 style={{ margin: '0 0 .5rem', fontSize: 20, fontWeight: 700, color: '#fff' }}>
            Código de autenticação
          </h2>

          {status !== 'done' && (
            <>
              <p style={{ margin: '0 0 1.5rem', fontSize: 14, color: '#9ca3af', lineHeight: 1.5 }}>
                Abra seu app autenticador (Google Authenticator, Authy…) e insira o código
                de 6 dígitos exibido.
              </p>

              <form onSubmit={verify} style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
                <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
                  <label style={{ fontSize: 13, fontWeight: 500, color: '#d1d5db' }}>
                    Código TOTP
                  </label>
                  <input
                    value={code}
                    onChange={e => setCode(e.target.value.replace(/\D/g, '').slice(0, 6))}
                    maxLength={6}
                    placeholder="000000"
                    required
                    inputMode="numeric"
                    autoComplete="one-time-code"
                    autoFocus
                    style={{
                      padding: '12px 16px', borderRadius: 8,
                      border: '1px solid #3b3b6e', background: '#0f0f1a',
                      color: '#ffffff', fontSize: 28, fontWeight: 700,
                      letterSpacing: '0.6rem', textAlign: 'center',
                      outline: 'none', fontFamily: 'monospace',
                      boxSizing: 'border-box', width: '100%',
                    }}
                  />
                </div>

                {message && (
                  <p style={{
                    fontSize: 13, margin: 0, textAlign: 'center',
                    color: status === 'error' ? '#f87171' : '#93c5fd',
                  }}>
                    {message}
                  </p>
                )}

                <button
                  type="submit"
                  disabled={code.length < 6 || status === 'verifying'}
                  style={{
                    padding: '13px 0', borderRadius: 8, border: 'none',
                    background: code.length < 6 || status === 'verifying' ? '#374151' : '#ffffff',
                    color: code.length < 6 || status === 'verifying' ? '#6b7280' : '#111827',
                    fontWeight: 700, fontSize: 15,
                    cursor: code.length < 6 ? 'not-allowed' : 'pointer',
                    transition: 'all .15s',
                  }}
                >
                  {status === 'verifying' ? 'Verificando…' : 'Confirmar'}
                </button>
              </form>
            </>
          )}

          {status === 'done' && (
            <div style={{ textAlign: 'center', paddingTop: '1rem' }}>
              <div style={{ fontSize: 48, marginBottom: '1rem' }}>✅</div>
              <p style={{ fontSize: 16, color: '#4ade80', fontWeight: 700, margin: 0 }}>
                Autenticado! Redirecionando…
              </p>
            </div>
          )}
        </div>

        <p style={{ textAlign: 'center', fontSize: 13, color: '#6b7280', margin: 0 }}>
          <a href="/auth/login" style={{ color: '#a5b4fc', textDecoration: 'none' }}>
            ← Voltar ao login
          </a>
        </p>
      </div>
    </div>
  )
}

export default function TotpVerifyPage() {
  return (
    <Suspense fallback={
      <div style={{ minHeight: '100vh', background: '#0f0f1a', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
        <p style={{ color: '#6b7280' }}>Carregando…</p>
      </div>
    }>
      <TotpVerifyContent />
    </Suspense>
  )
}
