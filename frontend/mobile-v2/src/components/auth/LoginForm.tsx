'use client'

import { useState } from 'react'
import { signIn } from 'next-auth/react'
import { useRouter } from 'next/navigation'
import Input  from '@/components/ui/Input'
import Button from '@/components/ui/Button'
import Link   from 'next/link'
import { resetSessionId } from '@/lib/session'

interface Props {
  callbackUrl?: string
}

export default function LoginForm({ callbackUrl = '/' }: Props) {
  const router = useRouter()
  const [email,       setEmail]       = useState('')
  const [password,    setPassword]    = useState('')
  const [showPass,    setShowPass]    = useState(false)
  const [error,       setError]       = useState('')
  const [loading,     setLoading]     = useState(false)

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    setLoading(true)
    try {
      const res = await signIn('credentials', {
        email, password, redirect: false,
      })
      if (res?.error) {
        setError('E-mail ou senha inválidos.')
      } else {
        resetSessionId()          // novo carrinho para este login
        router.push(callbackUrl)
        router.refresh()
      }
    } catch {
      setError('Algo deu errado. Tente novamente.')
    } finally {
      setLoading(false)
    }
  }

  return (
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

      {/* senha com toggle de visibilidade */}
      <div className="flex flex-col gap-1">
        <div className="flex items-center justify-between">
          <label className="text-xs font-medium text-muted uppercase tracking-wide">Senha</label>
          <Link href="/auth/forgot-password" className="text-xs text-brand font-medium hover:underline">
            Esqueceu a senha?
          </Link>
        </div>
        <div className="relative">
          <input
            type={showPass ? 'text' : 'password'}
            value={password}
            onChange={e => setPassword(e.target.value)}
            placeholder="••••••••"
            autoComplete="current-password"
            required
            className="w-full rounded-2xl border border-border bg-surface px-4 py-3 pr-11 text-sm text-ink placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-brand/30 focus:border-brand transition-colors"
          />
          <button
            type="button"
            onClick={() => setShowPass(v => !v)}
            className="absolute right-3 top-1/2 -translate-y-1/2 text-muted hover:text-ink transition-colors p-1"
            aria-label={showPass ? 'Ocultar senha' : 'Mostrar senha'}
          >
            {showPass ? (
              /* olho fechado */
              <svg width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round"
                  d="M17.94 17.94A10.07 10.07 0 0112 20c-7 0-11-8-11-8a18.45 18.45 0 015.06-5.94M9.9 4.24A9.12 9.12 0 0112 4c7 0 11 8 11 8a18.5 18.5 0 01-2.16 3.19M6.53 6.53L17.47 17.47"/>
                <line x1="1" y1="1" x2="23" y2="23" strokeLinecap="round"/>
              </svg>
            ) : (
              /* olho aberto */
              <svg width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/>
                <circle cx="12" cy="12" r="3" strokeLinecap="round" strokeLinejoin="round"/>
              </svg>
            )}
          </button>
        </div>
      </div>

      {error && (
        <p className="text-sm text-red-500 text-center">{error}</p>
      )}

      <Button type="submit" fullWidth loading={loading}>
        Entrar
      </Button>

      <div className="relative my-4">
        <div className="absolute inset-0 flex items-center">
          <div className="w-full border-t border-border" />
        </div>
        <div className="relative flex justify-center text-xs text-muted bg-white px-2">ou</div>
      </div>

      <Button
        type="button"
        variant="secondary"
        fullWidth
        onClick={() => signIn('google', { callbackUrl })}
      >
        <svg width="18" height="18" viewBox="0 0 24 24">
          <path fill="#4285F4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"/>
          <path fill="#34A853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"/>
          <path fill="#FBBC05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"/>
          <path fill="#EA4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"/>
        </svg>
        Continuar com Google
      </Button>

      <p className="text-center text-xs text-muted">
        Não tem uma conta?{' '}
        <Link href="/auth/register" className="text-brand font-semibold">Cadastre-se</Link>
      </p>
    </form>
  )
}
