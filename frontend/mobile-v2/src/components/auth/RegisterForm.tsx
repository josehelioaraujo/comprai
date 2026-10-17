'use client'

import { useState } from 'react'
import { signIn } from 'next-auth/react'
import { useRouter } from 'next/navigation'
import Input  from '@/components/ui/Input'
import Button from '@/components/ui/Button'
import Link   from 'next/link'
import { apiRegister } from '@/lib/api'

export default function RegisterForm() {
  const router = useRouter()
  const [name,     setName]     = useState('')
  const [email,    setEmail]    = useState('')
  const [password, setPassword] = useState('')
  const [confirm,  setConfirm]  = useState('')
  const [error,    setError]    = useState('')
  const [loading,  setLoading]  = useState(false)

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError('')

    if (password !== confirm) {
      setError('As senhas não coincidem.')
      return
    }
    if (password.length < 6) {
      setError('A senha deve ter pelo menos 6 caracteres.')
      return
    }

    setLoading(true)
    try {
      // 1. Register → backend returns token + customer
      const { token, customer } = await apiRegister(name, email, password)

      // 2. Sign in via pre-auth bypass path (no extra API call)
      const res = await signIn('credentials', {
        token,
        customer: JSON.stringify(customer),
        redirect: false,
      })

      if (res?.error) {
        setError('Conta criada, mas o login falhou. Por favor, entre manualmente.')
        router.push('/auth/login')
        return
      }

      // 3. Go to account page where user can verify email
      router.push('/account')
      router.refresh()
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Falha no cadastro'
      setError(msg)
    } finally {
      setLoading(false)
    }
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <Input
        label="Nome completo"
        type="text"
        value={name}
        onChange={e => setName(e.target.value)}
        placeholder="João Silva"
        autoComplete="name"
        required
      />
      <Input
        label="E-mail"
        type="email"
        value={email}
        onChange={e => setEmail(e.target.value)}
        placeholder="voce@exemplo.com"
        autoComplete="email"
        required
      />
      <Input
        label="Senha"
        type="password"
        value={password}
        onChange={e => setPassword(e.target.value)}
        placeholder="Mínimo 6 caracteres"
        autoComplete="new-password"
        required
      />
      <Input
        label="Confirmar Senha"
        type="password"
        value={confirm}
        onChange={e => setConfirm(e.target.value)}
        placeholder="Repita sua senha"
        autoComplete="new-password"
        required
      />

      {error && <p className="text-sm text-red-500 text-center">{error}</p>}

      <Button type="submit" fullWidth loading={loading}>
        Criar Conta
      </Button>

      <p className="text-center text-xs text-muted">
        Já tem uma conta?{' '}
        <Link href="/auth/login" className="text-brand font-semibold">Entrar</Link>
      </p>

      <p className="text-center text-[11px] text-muted leading-relaxed">
        Ao se cadastrar você concorda com nossos Termos de Serviço e Política de Privacidade.
      </p>
    </form>
  )
}
