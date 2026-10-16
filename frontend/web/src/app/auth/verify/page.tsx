'use client'

import { useState } from 'react'
import { useSession } from 'next-auth/react'
import { useRouter } from 'next/navigation'

const BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5020'

export default function VerifyPage() {
  const { data: session } = useSession()
  const router = useRouter()
  const [code,    setCode]    = useState('')
  const [status,  setStatus]  = useState<'idle' | 'sending' | 'verifying' | 'done' | 'error'>('idle')
  const [message, setMessage] = useState('')

  const token = (session?.user as any)?.token as string | undefined

  async function sendCode() {
    setStatus('sending')
    setMessage('')
    try {
      const res = await fetch(`${BASE_URL}/api/auth/send-verification`, {
        method: 'POST',
        headers: { Authorization: `Bearer ${token}` },
      })
      const d = await res.json()
      setMessage(d.message ?? 'Código enviado!')
      setStatus('idle')
    } catch {
      setMessage('Erro ao enviar código.')
      setStatus('error')
    }
  }

  async function verify(e: React.FormEvent) {
    e.preventDefault()
    setStatus('verifying')
    setMessage('')
    try {
      const res = await fetch(`${BASE_URL}/api/auth/verify-email`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
        body: JSON.stringify({ code }),
      })
      const d = await res.json()
      if (res.ok) {
        setStatus('done')
        setMessage(d.message ?? 'Email verificado!')
        setTimeout(() => router.push('/chat'), 2000)
      } else {
        setStatus('error')
        setMessage(d.error ?? 'Código inválido.')
      }
    } catch {
      setStatus('error')
      setMessage('Erro de conexão.')
    }
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
            Confirme seu endereço de e-mail
          </p>
        </div>

        {/* Card */}
        <div style={{
          background: '#1a1a2e', borderRadius: 16, padding: '2rem',
          border: '1px solid #2d2d4a',
        }}>
          <h2 style={{ margin: '0 0 .5rem', fontSize: 20, fontWeight: 700, color: '#fff' }}>
            Verificação de e-mail
          </h2>
          <p style={{ margin: '0 0 1.5rem', fontSize: 14, color: '#9ca3af', lineHeight: 1.5 }}>
            Clique em <strong style={{ color: '#fff' }}>Enviar código</strong> e insira o código
            de 6 caracteres que chegará no seu e-mail.
          </p>

          {/* Send button */}
          <button
            onClick={sendCode}
            disabled={status === 'sending' || status === 'done'}
            style={{
              width: '100%', padding: '11px 0', borderRadius: 8, border: 'none',
              background: status === 'sending' ? '#374151' : '#3b3b6e',
              color: '#e5e7eb', fontWeight: 600, fontSize: 14,
              cursor: status === 'sending' ? 'not-allowed' : 'pointer',
              marginBottom: '1.25rem', transition: 'background .15s',
            }}
          >
            {status === 'sending' ? 'Enviando…' : 'Enviar código'}
          </button>

          {/* OTP input */}
          <form onSubmit={verify} style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
            <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
              <label style={{ fontSize: 13, fontWeight: 500, color: '#d1d5db' }}>
                Código de verificação
              </label>
              <input
                value={code}
                onChange={e => setCode(e.target.value.toUpperCase().replace(/[^A-Z0-9]/g, '').slice(0, 6))}
                maxLength={6}
                placeholder="XXXXXX"
                required
                style={{
                  padding: '12px 16px', borderRadius: 8,
                  border: '1px solid #3b3b6e', background: '#0f0f1a',
                  color: '#ffffff', fontSize: 22, fontWeight: 700,
                  letterSpacing: '0.5rem', textAlign: 'center',
                  outline: 'none', fontFamily: 'monospace',
                  boxSizing: 'border-box', width: '100%',
                }}
              />
            </div>

            {message && (
              <p style={{
                fontSize: 13, margin: 0, textAlign: 'center',
                color: status === 'done' ? '#4ade80' : status === 'error' ? '#f87171' : '#93c5fd',
              }}>
                {message}
              </p>
            )}

            <button
              type="submit"
              disabled={code.length < 6 || status === 'verifying' || status === 'done'}
              style={{
                padding: '13px 0', borderRadius: 8, border: 'none',
                background: code.length < 6 || status === 'done' ? '#374151' : '#ffffff',
                color: code.length < 6 || status === 'done' ? '#6b7280' : '#111827',
                fontWeight: 700, fontSize: 15,
                cursor: code.length < 6 ? 'not-allowed' : 'pointer',
                transition: 'all .15s',
              }}
            >
              {status === 'verifying' ? 'Verificando…' : status === 'done' ? '✓ Verificado!' : 'Confirmar'}
            </button>
          </form>
        </div>

        {/* Resend link */}
        <p style={{ textAlign: 'center', fontSize: 13, color: '#6b7280', margin: 0 }}>
          Não recebeu?{' '}
          <button
            onClick={sendCode}
            style={{ background: 'none', border: 'none', color: '#a5b4fc', cursor: 'pointer', fontSize: 13, textDecoration: 'underline', padding: 0 }}
          >
            Reenviar código
          </button>
        </p>
      </div>
    </div>
  )
}
