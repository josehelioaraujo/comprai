'use client'

import { useSession, signOut } from 'next-auth/react'

export function useAuth() {
  const { data: session, status } = useSession()
  return {
    user:          session?.user ?? null,
    isLoading:     status === 'loading',
    isAuthenticated: status === 'authenticated',
    backendToken:  (session as any)?.backendToken as string | null ?? null,
    signOut:       () => signOut({ callbackUrl: '/auth/login' }),
  }
}
