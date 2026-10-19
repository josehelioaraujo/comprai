'use client'

import { useState, Suspense } from 'react'
import { useSearchParams, useRouter } from 'next/navigation'
import AppShell from '@/components/layout/AppShell'
import Button from '@/components/ui/Button'
import Link from 'next/link'

function ResetForm() {
  const params   = useSearchParams()
  const router   = useRouter()
  const token    = params.get('token') ?? ''

  const [password,  setPassword]  = useState('')
  const [confirm,   setConfirm]   = useState('')
  const [showPass,  setShowPass]  = useState(false)
  const [error,     setError]     = useState('')
  const [loading,   setLoading]   = useState(false)
  const [done,      setDone]      = useState(false)

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (password !== confirm) { setError('As senhas não coincidem.'); return }
    if (password.length < 8)  { setError('A senha deve ter pelo menos 8 caracteres.'); return }
    setError('')
    setLoading(true)
    try {
      const res = await fetch('/proxy/api/auth/reset-password', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ token, newPassword: password }),
      })
      if (res.ok) {
        setDone(true)
        setTimeout(() => router.push('/auth/login'), 2500)
      } else {
        const data = await res.json().catch(() => ({}))
        setError(data?.error ?? 'Link inválido ou expirado. Solicite um novo.')
      }
    } catch {
      setError('Erro de conexão. Tente novamente.')
    } finally {
      setLoading(false)
    }
  }

  const EyeIcon = () => showPass ? (
    <svg width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
      <path strokeLinecap="round" strokeLinejoin="round"
        d="M17.94 17.94A10.07 10.07 0 0112 20c-7 0-11-8-11-8a18.45 18.45 0 015.06-5.94M9.9 4.24A9.12 9.12 0 0112 4c7 0 11 8 11 8a18.5 18.5 0 01-2.16 3.19M6.53 6.53L17.47 17.47"/>
      <line x1="1" y1="1" x2="23" y2="23" strokeLinecap="round"/>
    </svg>
  ) : (
    <svg width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
      <path strokeLinecap="round" strokeLinejoin="round" d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/>
      <circle cx="12" cy="12" r="3" strokeLinecap="round" strokeLinejoin="round"/>
    </svg>
  )

  return done ? (
    <div className="space-y-4">
      <div className="bg-green-50 border border-green-200 rounded-2xl p-4 text-center">
        <p className="text-sm font-semibold text-green-800">Senha redefinida!</p>
        <p className="text-xs text-green-700 mt-1">Redirecionando para o login…</p>
      </div>
    </div>
  ) : (
    <form onSubmit={handleSubmit} className="space-y-4">
      {/* nova senha */}
      <div className="flex flex-col gap-1">
        <label className="text-xs font-medium text-muted uppercase tracking-wide">Nova senha</label>
        <div className="relative">
          <input
            type={showPass ? 'text' : 'password'}
            value={password}
            onChange={e => setPassword(e.target.value)}
            placeholder="Mínimo 8 caracteres"
            required
            className="w-full rounded-2xl border border-border bg-surface px-4 py-3 pr-11 text-sm text-ink placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-brand/30 focus:border-brand transition-colors"
          />
          <button type="button" onClick={() => setShowPass(v => !v)}
            className="absolute right-3 top-1/2 -translate-y-1/2 text-muted hover:text-ink p-1">
            <EyeIcon/>
          </button>
        </div>
      </div>

      {/* confirmar */}
      <div className="flex flex-col gap-1">
        <label className="text-xs font-medium text-muted uppercase tracking-wide">Confirmar senha</label>
        <input
          type={showPass ? 'text' : 'password'}
          value={confirm}
          onChange={e => setConfirm(e.target.value)}
          placeholder="Repita a nova senha"
          required
          className="w-full rounded-2xl border border-border bg-surface px-4 py-3 text-sm text-ink placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-brand/30 focus:border-brand transition-colors"
        />
      </div>

      {error && <p className="text-sm text-red-500 text-center">{error}</p>}

      <Button type="submit" fullWidth loading={loading}>
        Redefinir senha
      </Button>

      <p className="text-center text-xs text-muted">
        <Link href="/auth/login" className="text-brand font-semibold">Voltar ao login</Link>
      </p>
    </form>
  )
}

export default function ResetPasswordPage() {
  return (
    <AppShell hideNav>
      <div className="min-h-screen flex flex-col justify-center px-6 py-12">
        <div className="text-center mb-8">
          <div className="w-14 h-14 rounded-2xl bg-brand-light flex items-center justify-center mx-auto mb-3">
            <svg width="28" height="28" fill="none" stroke="#7C3AED" strokeWidth="1.8" viewBox="0 0 24 24">
              <rect x="3" y="11" width="18" height="11" rx="2" ry="2" strokeLinecap="round" strokeLinejoin="round"/>
              <path strokeLinecap="round" strokeLinejoin="round" d="M7 11V7a5 5 0 0110 0v4"/>
            </svg>
          </div>
          <h1 className="text-2xl font-bold text-ink">Redefinir senha</h1>
          <p className="text-sm text-muted mt-1">Crie uma nova senha para sua conta</p>
        </div>
        <Suspense>
          <ResetForm/>
        </Suspense>
      </div>
    </AppShell>
  )
}
