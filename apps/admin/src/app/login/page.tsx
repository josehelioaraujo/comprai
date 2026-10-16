'use client'

import { useState, FormEvent } from 'react'
import { useRouter } from 'next/navigation'

/* ─── tipos de modal ─────────────────────────────────────────────────────── */
type Modal = 'none' | 'forgot' | 'register'

/* ─── ícone de olho ──────────────────────────────────────────────────────── */
function EyeIcon({ open }: { open: boolean }) {
  return open ? (
    <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2}
        d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l3.59 3.59m0 0A9.953 9.953 0 0112 5c4.478 0 8.268 2.943 9.543 7a10.025 10.025 0 01-4.132 5.411m0 0L21 21" />
    </svg>
  ) : (
    <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2}
        d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
    </svg>
  )
}

/* ─── backdrop do modal ──────────────────────────────────────────────────── */
function Backdrop({ children, onClose }: { children: React.ReactNode; onClose: () => void }) {
  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 backdrop-blur-sm p-4"
      onClick={e => { if (e.target === e.currentTarget) onClose() }}
    >
      {children}
    </div>
  )
}

/* ─── modal "Esqueci a senha" ────────────────────────────────────────────── */
function ForgotPasswordModal({ onClose }: { onClose: () => void }) {
  const [email, setEmail] = useState('')
  const [loading, setLoading] = useState(false)
  const [done, setDone] = useState(false)
  const [error, setError] = useState('')

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setError('')
    setLoading(true)
    try {
      await fetch('/api/auth/forgot-password', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email }),
      })
      // Anti-enumeração: sempre mostra sucesso
      setDone(true)
    } catch {
      setError('Erro de conexão. Tente novamente.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <Backdrop onClose={onClose}>
      <div className="w-full max-w-sm bg-gray-900 rounded-2xl border border-gray-800 shadow-2xl p-7">
        <div className="flex items-center justify-between mb-5">
          <h2 className="text-lg font-bold text-white">Esqueci a senha</h2>
          <button onClick={onClose} className="text-gray-500 hover:text-gray-300 transition">
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        {done ? (
          <div className="text-center py-4">
            <div className="inline-flex items-center justify-center w-14 h-14 rounded-full bg-purple-500/20 mb-4">
              <svg className="w-7 h-7 text-purple-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
              </svg>
            </div>
            <p className="text-white font-semibold mb-1">Verifique seu e-mail</p>
            <p className="text-gray-400 text-sm">
              Se houver uma conta com este endereço, você receberá um link para redefinir sua senha em breve.
            </p>
            <button
              onClick={onClose}
              className="mt-5 w-full py-2.5 rounded-xl bg-purple-700 hover:bg-purple-600 text-white font-semibold transition text-sm"
            >
              Fechar
            </button>
          </div>
        ) : (
          <form onSubmit={handleSubmit}>
            <p className="text-gray-400 text-sm mb-4">
              Informe seu e-mail de administrador e enviaremos um link para redefinir sua senha.
            </p>

            {error && (
              <div className="mb-4 p-3 rounded-lg bg-red-500/10 border border-red-500/30 text-red-400 text-sm">
                {error}
              </div>
            )}

            <div className="mb-5">
              <label className="block text-sm font-medium text-gray-300 mb-1.5">E-mail</label>
              <input
                type="email"
                value={email}
                onChange={e => setEmail(e.target.value)}
                required
                autoFocus
                className="w-full px-4 py-3 rounded-xl bg-gray-800 border border-gray-700 text-white
                           placeholder-gray-500 focus:outline-none focus:border-purple-500 focus:ring-1
                           focus:ring-purple-500 transition"
                placeholder="admin@comprai.com.br"
              />
            </div>

            <button
              type="submit"
              disabled={loading}
              className="w-full py-3 rounded-xl bg-purple-700 hover:bg-purple-600 text-white font-semibold
                         transition disabled:opacity-60 disabled:cursor-not-allowed"
            >
              {loading ? 'Enviando…' : 'Enviar link'}
            </button>
          </form>
        )}
      </div>
    </Backdrop>
  )
}

/* ─── modal "Cadastrar Usuário" ──────────────────────────────────────────── */
function RegisterModal({ onClose }: { onClose: () => void }) {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [loading, setLoading] = useState(false)
  const [done, setDone] = useState(false)
  const [error, setError] = useState('')

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setError('')
    if (password.length < 8) {
      setError('A senha deve ter pelo menos 8 caracteres.')
      return
    }
    setLoading(true)
    try {
      const res = await fetch('/api/auth/register-admin', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password }),
      })
      if (res.status === 204 || res.ok) {
        setDone(true)
      } else {
        const data = await res.json().catch(() => ({}))
        setError(data.error ?? 'Não foi possível cadastrar o usuário.')
      }
    } catch {
      setError('Erro de conexão. Tente novamente.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <Backdrop onClose={onClose}>
      <div className="w-full max-w-sm bg-gray-900 rounded-2xl border border-gray-800 shadow-2xl p-7">
        <div className="flex items-center justify-between mb-5">
          <h2 className="text-lg font-bold text-white">Cadastrar Usuário</h2>
          <button onClick={onClose} className="text-gray-500 hover:text-gray-300 transition">
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        {done ? (
          <div className="text-center py-4">
            <div className="inline-flex items-center justify-center w-14 h-14 rounded-full bg-green-500/20 mb-4">
              <svg className="w-7 h-7 text-green-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
              </svg>
            </div>
            <p className="text-white font-semibold mb-1">Cadastro enviado!</p>
            <p className="text-gray-400 text-sm">
              Um e-mail de confirmação foi enviado para <span className="text-purple-400">{email}</span>.
              O usuário deve clicar no link do e-mail para ativar o acesso.
            </p>
            <button
              onClick={onClose}
              className="mt-5 w-full py-2.5 rounded-xl bg-purple-700 hover:bg-purple-600 text-white font-semibold transition text-sm"
            >
              Fechar
            </button>
          </div>
        ) : (
          <form onSubmit={handleSubmit}>
            <p className="text-gray-400 text-sm mb-4">
              Cadastre um novo administrador. Um e-mail de confirmação será enviado para ativar o acesso.
            </p>

            {error && (
              <div className="mb-4 p-3 rounded-lg bg-red-500/10 border border-red-500/30 text-red-400 text-sm">
                {error}
              </div>
            )}

            <div className="mb-4">
              <label className="block text-sm font-medium text-gray-300 mb-1.5">E-mail</label>
              <input
                type="email"
                value={email}
                onChange={e => setEmail(e.target.value)}
                required
                autoFocus
                className="w-full px-4 py-3 rounded-xl bg-gray-800 border border-gray-700 text-white
                           placeholder-gray-500 focus:outline-none focus:border-purple-500 focus:ring-1
                           focus:ring-purple-500 transition"
                placeholder="novo@comprai.com.br"
              />
            </div>

            <div className="mb-5">
              <label className="block text-sm font-medium text-gray-300 mb-1.5">Senha inicial</label>
              <div className="relative">
                <input
                  type={showPassword ? 'text' : 'password'}
                  value={password}
                  onChange={e => setPassword(e.target.value)}
                  required
                  minLength={8}
                  className="w-full px-4 py-3 pr-11 rounded-xl bg-gray-800 border border-gray-700 text-white
                             placeholder-gray-500 focus:outline-none focus:border-purple-500 focus:ring-1
                             focus:ring-purple-500 transition"
                  placeholder="Mínimo 8 caracteres"
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(v => !v)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-200 transition"
                  aria-label={showPassword ? 'Ocultar senha' : 'Exibir senha'}
                >
                  <EyeIcon open={showPassword} />
                </button>
              </div>
            </div>

            <button
              type="submit"
              disabled={loading}
              className="w-full py-3 rounded-xl bg-purple-700 hover:bg-purple-600 text-white font-semibold
                         transition disabled:opacity-60 disabled:cursor-not-allowed"
            >
              {loading ? 'Cadastrando…' : 'Cadastrar e enviar e-mail'}
            </button>
          </form>
        )}
      </div>
    </Backdrop>
  )
}

/* ─── página principal ───────────────────────────────────────────────────── */
export default function LoginPage() {
  const router = useRouter()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const [redirecting, setRedirecting] = useState(false)
  const [showPassword, setShowPassword] = useState(false)
  const [modal, setModal] = useState<Modal>('none')

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setError('')
    setLoading(true)
    try {
      const res = await fetch('/api/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password }),
      })
      const data = await res.json()
      if (!res.ok) {
        setError(data.error ?? 'Credenciais inválidas.')
        return
      }
      setRedirecting(true)
      router.push('/dashboard')
    } catch {
      setError('Erro de conexão. Tente novamente.')
    } finally {
      setLoading(false)
    }
  }

  if (redirecting) {
    return (
      <div className="min-h-screen flex flex-col items-center justify-center bg-gray-950 gap-4">
        <svg className="w-10 h-10 text-purple-500 animate-spin" fill="none" viewBox="0 0 24 24">
          <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
          <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8z" />
        </svg>
        <p className="text-gray-300 text-sm">Redirecionando para o dashboard…</p>
      </div>
    )
  }

  return (
    <>
      {modal === 'forgot'    && <ForgotPasswordModal onClose={() => setModal('none')} />}
      {modal === 'register'  && <RegisterModal       onClose={() => setModal('none')} />}

      <div className="min-h-screen flex items-center justify-center bg-gray-950">
        <div className="w-full max-w-md">
          {/* Logo / título */}
          <div className="text-center mb-8">
            <div className="inline-flex items-center justify-center w-16 h-16 rounded-2xl bg-purple-700 mb-4">
              <svg className="w-8 h-8 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2}
                  d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
              </svg>
            </div>
            <h1 className="text-2xl font-bold text-white">Comprai Admin</h1>
            <p className="text-gray-400 text-sm mt-1">Acesso restrito a administradores</p>
          </div>

          <form onSubmit={handleSubmit} className="bg-gray-900 rounded-2xl p-8 shadow-xl border border-gray-800">
            {error && (
              <div className="mb-4 p-3 rounded-lg bg-red-500/10 border border-red-500/30 text-red-400 text-sm">
                {error}
              </div>
            )}

            <div className="mb-5">
              <label className="block text-sm font-medium text-gray-300 mb-1.5">E-mail</label>
              <input
                type="email"
                value={email}
                onChange={e => setEmail(e.target.value)}
                required
                className="w-full px-4 py-3 rounded-xl bg-gray-800 border border-gray-700 text-white
                           placeholder-gray-500 focus:outline-none focus:border-purple-500 focus:ring-1
                           focus:ring-purple-500 transition"
                placeholder="admin@comprai.com.br"
              />
            </div>

            <div className="mb-2">
              <label className="block text-sm font-medium text-gray-300 mb-1.5">Senha</label>
              <div className="relative">
                <input
                  type={showPassword ? 'text' : 'password'}
                  value={password}
                  onChange={e => setPassword(e.target.value)}
                  required
                  className="w-full px-4 py-3 pr-11 rounded-xl bg-gray-800 border border-gray-700 text-white
                             placeholder-gray-500 focus:outline-none focus:border-purple-500 focus:ring-1
                             focus:ring-purple-500 transition"
                  placeholder="••••••••"
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(v => !v)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-200 transition"
                  aria-label={showPassword ? 'Ocultar senha' : 'Exibir senha'}
                >
                  <EyeIcon open={showPassword} />
                </button>
              </div>
            </div>

            {/* Esqueci a senha */}
            <div className="mb-6 text-right">
              <button
                type="button"
                onClick={() => setModal('forgot')}
                className="text-sm text-purple-400 hover:text-purple-300 transition"
              >
                Esqueci a senha
              </button>
            </div>

            <button
              type="submit"
              disabled={loading}
              className="w-full py-3 px-6 rounded-xl bg-purple-700 hover:bg-purple-600 text-white font-semibold
                         transition disabled:opacity-60 disabled:cursor-not-allowed"
            >
              {loading ? 'Verificando…' : 'Entrar'}
            </button>

            <div className="mt-4 pt-4 border-t border-gray-800">
              <button
                type="button"
                onClick={() => setModal('register')}
                className="w-full py-2.5 rounded-xl border border-gray-700 hover:border-purple-600
                           text-gray-300 hover:text-white text-sm font-medium transition"
              >
                + Cadastrar Usuário
              </button>
            </div>
          </form>
        </div>
      </div>
    </>
  )
}
