'use client'

import { useSession, signOut } from 'next-auth/react'
import { useState, useEffect } from 'react'

const BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5020'

export interface CustomerAddress {
  id: string; label: string; zipCode: string; street: string
  number?: string; complement?: string; neighborhood?: string
  city: string; state: string; isDefault: boolean
}

export interface CustomerProfile {
  id: string; name: string; email: string; provider: string
  avatarUrl?: string; phone?: string; document?: string
  addresses?: CustomerAddress[]
}

export function useAuth() {
  const { data: session, status } = useSession()
  const [profile, setProfile] = useState<CustomerProfile | null>(null)

  // Busca perfil completo (com endereços) quando autenticado
  useEffect(() => {
    if (status !== 'authenticated') { setProfile(null); return }
    const token = (session as any)?.backendToken
    if (!token) return
    fetch(`${BASE_URL}/api/auth/me`, {
      headers: { Authorization: `Bearer ${token}` }
    })
      .then(r => r.ok ? r.json() : null)
      .then(d => setProfile(d))
      .catch(() => {})
  }, [status, session])

  return {
    user:            session?.user ?? null,
    profile,
    isLoading:       status === 'loading',
    isAuthenticated: status === 'authenticated',
    backendToken:    (session as any)?.backendToken as string | null ?? null,
    signOut:         () => signOut({ callbackUrl: '/auth/login' }),
    defaultAddress:  profile?.addresses?.find(a => a.isDefault) ?? null,
  }
}
