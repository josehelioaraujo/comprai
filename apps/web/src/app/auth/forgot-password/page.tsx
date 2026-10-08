'use client'

import { useState } from 'react'
import Link from 'next/link'

const BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5020'

export default function ForgotPasswordPage() {
  const [email,   setEmail]   = useState('')
  const [sent,    setSent]    = useState(false)
  const [loading, setLoading] = useState(false)
  const [error,   setError]   = useState('')

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    setLoading(true)
    try {
      await fetch(`${BASE_URL}/api/auth/forgot-password`, {
        method:  'POST',
        headers: { 'Content-Type': 'application/json' },
        body:    JSON.stringify({ email }),
      })
      // Sempre exibe mensagem de sucesso (não revela se e-mail existe)
      setSent(true)
    } catch {
      setError('Erro ao conectar. Tente novamente.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div style={{
      minHeight: '100vh',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      background: '#fff',
      fontFamily: 'Inter, sans-serif',
      padding: '1rem',
    }}>
      <div style={{ width: '100%', maxWidth: 400 }}>
        {/* Logo */}
        <div style={{ textAlign: 'center', marginBottom: '2rem' }}>
          <span style={{ fontSize: 32, fontWeight: 800, color: '#7c3aed', letterSpacing: '-1px' }}>
            Comprai
          </span>
          <div style={{ fontSize: 13, color: '#6b7280', marginTop: 4 }}>
            Recuperação de senha
          </div>
        </div>

        {sent ? (
          <div style={{
            background: '#f0fdf4',
            border: '1px solid #bbf7d0',
            borderRadius: 12,
            padding: '1.5rem',
            textAlign: 'center',
          }}>
            <div style={{ fontSize: 28, marginBottom: 12 }}>📧</div>
            <div style={{ fontWeight: 700, color: '#15803d', marginBottom: 8 }}>
              E-mail enviado!
            </div>
            <div style={{ fontSize: 14, color: '#166534' }}>
              Se este e-mail estiver cadastrado, você receberá as instruções em breve.
              Verifique também a caixa de spam.
            </div>
            <Link href="/auth/login" style={{
              display: 'inline-block',
              marginTop: '1.25rem',
              color: '#7c3aed',
              fontWeight: 600,
              fontSize: 14,
              textDecoration: 'none',
            }}>
              ← Voltar para o login
            </Link>
          </div>
        ) : (
          <form onSubmit={handleSubmit}>
            <div style={{ marginBottom: '1rem' }}>
              <label style={{ display: 'block', fontSize: 13, fontWeight: 600, color: '#374151', marginBottom: 6 }}>
                E-mail
              </label>
              <input
                type="email"
                required
                value={email}
                onChange={e => setEmail(e.target.value)}
                placeholder="seu@email.com"
                style={{
                  width: '100%',
                  padding: '10px 14px',
                  border: '1px solid #d1d5db',
                  borderRadius: 8,
                  fontSize: 14,
                  outline: 'none',
                  boxSizing: 'border-box',
                  color: '#111827',
                }}
              />
            </div>

            {error && (
              <div style={{ color: '#dc2626', fontSize: 13, marginBottom: '1rem' }}>{error}</div>
            )}

            <button
              type="submit"
              disabled={loading}
              style={{
                width: '100%',
                padding: '11px',
                background: loading ? '#a78bfa' : '#7c3aed',
                color: '#fff',
                border: 'none',
                borderRadius: 8,
                fontSize: 15,
                fontWeight: 700,
                cursor: loading ? 'not-allowed' : 'pointer',
                transition: 'background .15s',
              }}
            >
              {loading ? 'Enviando...' : 'Enviar link de recuperação'}
            </button>

            <div style={{ textAlign: 'center', marginTop: '1.25rem' }}>
              <Link href="/auth/login" style={{
                color: '#7c3aed',
                fontWeight: 600,
                fontSize: 14,
                textDecoration: 'none',
              }}>
                ← Voltar para o login
              </Link>
            </div>
          </form>
        )}
      </div>
    </div>
  )
}
