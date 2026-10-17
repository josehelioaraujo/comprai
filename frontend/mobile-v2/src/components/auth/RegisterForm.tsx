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
      setError('Passwords do not match.')
      return
    }
    if (password.length < 6) {
      setError('Password must be at least 6 characters.')
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
        setError('Account created but sign-in failed. Please log in manually.')
        router.push('/auth/login')
        return
      }

      // 3. Go to account page where user can verify email
      router.push('/account')
      router.refresh()
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Registration failed'
      setError(msg)
    } finally {
      setLoading(false)
    }
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <Input
        label="Full Name"
        type="text"
        value={name}
        onChange={e => setName(e.target.value)}
        placeholder="Jane Doe"
        autoComplete="name"
        required
      />
      <Input
        label="Email"
        type="email"
        value={email}
        onChange={e => setEmail(e.target.value)}
        placeholder="you@example.com"
        autoComplete="email"
        required
      />
      <Input
        label="Password"
        type="password"
        value={password}
        onChange={e => setPassword(e.target.value)}
        placeholder="At least 6 characters"
        autoComplete="new-password"
        required
      />
      <Input
        label="Confirm Password"
        type="password"
        value={confirm}
        onChange={e => setConfirm(e.target.value)}
        placeholder="Repeat your password"
        autoComplete="new-password"
        required
      />

      {error && <p className="text-sm text-red-500 text-center">{error}</p>}

      <Button type="submit" fullWidth loading={loading}>
        Create Account
      </Button>

      <p className="text-center text-xs text-muted">
        Already have an account?{' '}
        <Link href="/auth/login" className="text-brand font-semibold">Sign in</Link>
      </p>

      <p className="text-center text-[11px] text-muted leading-relaxed">
        By registering you agree to our Terms of Service and Privacy Policy.
      </p>
    </form>
  )
}
