'use client'

import { useState } from 'react'
import AppShell from '@/components/layout/AppShell'
import Button from '@/components/ui/Button'
import Input from '@/components/ui/Input'
import Link from 'next/link'

export default function ForgotPasswordPage() {
  const [email,   setEmail]   = useState('')
  const [sent,    setSent]    = useState(false)
  const [error,   setError]   = useState('')
  const [loading, setLoading] = useState(false)

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    setLoading(true)
    try {
      const res = await fetch('/proxy/api/auth/forgot-password', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email }),
      })
      // O backend sempre retorna 200 (não revela se o e-mail existe)
      if (res.ok || res.status === 404) {
        setSent(true)
      } else {
        const data = await res.json().catch(() => ({}))
        setError(data?.error ?? 'Algo deu errado. Tente novamente.')
      }
    } catch {
      setError('Erro de conexão. Tente novamente.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <AppShell hideNav>
      <div className="min-h-screen flex flex-col justify-center px-6 py-12">
        {/* voltar */}
        <Link href="/auth/login"
          className="absolute top-5 left-4 text-muted hover:text-ink transition-colors">
          <svg width="22" height="22" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" d="M15 19l-7-7 7-7"/>
          </svg>
        </Link>

        <div className="text-center mb-8">
          <div className="w-14 h-14 rounded-2xl bg-brand-light flex items-center justify-center mx-auto mb-3">
            <svg width="28" height="28" fill="none" stroke="#7C3AED" strokeWidth="1.8" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round"
                d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"/>
            </svg>
          </div>
          <h1 className="text-2xl font-bold text-ink">Esqueceu a senha?</h1>
          <p className="text-sm text-muted mt-1">
            {sent
              ? 'Verifique seu e-mail'
              : 'Informe seu e-mail para receber o link de redefinição'}
          </p>
        </div>

        {sent ? (
          <div className="space-y-4">
            <div className="bg-green-50 border border-green-200 rounded-2xl p-4 text-center">
              <p className="text-sm font-semibold text-green-800">Link enviado!</p>
              <p className="text-xs text-green-700 mt-1">
                Se esse e-mail estiver cadastrado, você receberá um link em instantes.
                Verifique também a caixa de spam.
              </p>
            </div>
            <Link href="/auth/login">
              <Button fullWidth variant="secondary">Voltar ao login</Button>
            </Link>
          </div>
        ) : (
          <form onSubmit={handleSubmit} className="space-y-4">
            <Input
              label="E-mail"
              type="email"
              value={email}
              onChange={e => setEmail(e.target.value)}
              placeholder="voce@exemplo.com"
              autoComplete="email"
              required
            />

            {error && (
              <p className="text-sm text-red-500 text-center">{error}</p>
            )}

            <Button type="submit" fullWidth loading={loading}>
              Enviar link de redefinição
            </Button>

            <p className="text-center text-xs text-muted">
              Lembrou a senha?{' '}
              <Link href="/auth/login" className="text-brand font-semibold">Entrar</Link>
            </p>
          </form>
        )}
      </div>
    </AppShell>
  )
}
