'use client'

import { useState, useEffect, Suspense } from 'react'
import { useSearchParams } from 'next/navigation'
import Link from 'next/link'

const BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5020'

function ResetPasswordForm() {
  const searchParams  = useSearchParams()
  const token         = searchParams.get('token') ?? ''

  const [newPassword,     setNewPassword]     = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [done,            setDone]            = useState(false)
  const [loading,         setLoading]         = useState(false)
  const [error,           setError]           = useState('')

  useEffect(() => {
    if (!token) setError('Link inválido ou expirado. Solicite um novo.')
  }, [token])

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError('')

    if (newPassword.length < 8) {
      setError('A senha deve ter pelo menos 8 caracteres.')
      return
    }
    if (newPassword !== confirmPassword) {
      setError('As senhas não coincidem.')
      return
    }

    setLoading(true)
    try {
      const res = await fetch(`${BASE_URL}/api/auth/reset-password`, {
        method:  'POST',
        headers: { 'Content-Type': 'application/json' },
        body:    JSON.stringify({ token, newPassword }),
      })
      if (res.ok) {
        setDone(true)
      } else {
        const data = await res.json().catch(() => ({}))
        setError(data?.error ?? 'Token inválido ou expirado. Solicite um novo link.')
      }
    } catch {
      setError('Erro ao conectar. Tente novamente.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div style={{
      minHeight:       '100vh',
      display:         'flex',
      alignItems:      'center',
      justifyContent:  'center',
      background:      '#fff',
      fontFamily:      'Inter, sans-serif',
      padding:         '1rem',
    }}>
      <div style={{ width: '100%', maxWidth: 400 }}>
        {/* Logo */}
        <div style={{ textAlign: 'center', marginBottom: '2rem' }}>
          <span style={{ fontSize: 32, fontWeight: 800, color: '#7c3aed', letterSpacing: '-1px' }}>
            Comprai
          </span>
          <div style={{ fontSize: 13, color: '#6b7280', marginTop: 4 }}>
            Redefinição de senha
          </div>
        </div>

        {done ? (
          <div style={{
            background:   '#f0fdf4',
            border:       '1px solid #bbf7d0',
            borderRadius: 12,
            padding:      '1.5rem',
            textAlign:    'center',
          }}>
            <div style={{ fontSize: 28, marginBottom: 12 }}>✅</div>
            <div style={{ fontWeight: 700, color: '#15803d', marginBottom: 8 }}>
              Senha redefinida!
            </div>
            <div style={{ fontSize: 14, color: '#166534' }}>
              Sua senha foi alterada com sucesso. Você já pode fazer login.
            </div>
            <Link href="/auth/login" style={{
              display:        'inline-block',
              marginTop:      '1.25rem',
              color:          '#7c3aed',
              fontWeight:     600,
              fontSize:       14,
              textDecoration: 'none',
            }}>
              Ir para o login →
            </Link>
          </div>
        ) : (
          <form onSubmit={handleSubmit}>
            <div style={{ marginBottom: '1rem' }}>
              <label style={{ display: 'block', fontSize: 13, fontWeight: 600, color: '#374151', marginBottom: 6 }}>
                Nova senha
              </label>
              <input
                type="password"
                required
                minLength={8}
                value={newPassword}
                onChange={e => setNewPassword(e.target.value)}
                placeholder="Mínimo 8 caracteres"
                disabled={!token}
                style={{
                  width:      '100%',
                  padding:    '10px 14px',
                  border:     '1px solid #d1d5db',
                  borderRadius: 8,
                  fontSize:   14,
                  outline:    'none',
                  boxSizing:  'border-box',
                  color:      '#111827',
                  background: token ? '#fff' : '#f9fafb',
                }}
              />
            </div>

            <div style={{ marginBottom: '1rem' }}>
              <label style={{ display: 'block', fontSize: 13, fontWeight: 600, color: '#374151', marginBottom: 6 }}>
                Confirmar nova senha
              </label>
              <input
                type="password"
                required
                value={confirmPassword}
                onChange={e => setConfirmPassword(e.target.value)}
                placeholder="Repita a nova senha"
                disabled={!token}
                style={{
                  width:      '100%',
                  padding:    '10px 14px',
                  border:     '1px solid #d1d5db',
                  borderRadius: 8,
                  fontSize:   14,
                  outline:    'none',
                  boxSizing:  'border-box',
                  color:      '#111827',
                  background: token ? '#fff' : '#f9fafb',
                }}
              />
            </div>

            {error && (
              <div style={{ color: '#dc2626', fontSize: 13, marginBottom: '1rem' }}>{error}</div>
            )}

            <button
              type="submit"
              disabled={loading || !token}
              style={{
                width:      '100%',
                padding:    '11px',
                background: (loading || !token) ? '#a78bfa' : '#7c3aed',
                color:      '#fff',
                border:     'none',
                borderRadius: 8,
                fontSize:   15,
                fontWeight: 700,
                cursor:     (loading || !token) ? 'not-allowed' : 'pointer',
                transition: 'background .15s',
              }}
            >
              {loading ? 'Salvando...' : 'Redefinir senha'}
            </button>

            <div style={{ textAlign: 'center', marginTop: '1.25rem' }}>
              <Link href="/auth/login" style={{
                color:          '#7c3aed',
                fontWeight:     600,
                fontSize:       14,
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

export default function ResetPasswordPage() {
  return (
    <Suspense>
      <ResetPasswordForm />
    </Suspense>
  )
}
