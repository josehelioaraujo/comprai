'use client'

import { useAuth } from '@/lib/useAuth'
import { useRouter, usePathname } from 'next/navigation'
import { useEffect } from 'react'
import Link from 'next/link'

export default function UserLayout({ children }: { children: React.ReactNode }) {
  const { user, isLoading, isAuthenticated, signOut } = useAuth()
  const router   = useRouter()
  const pathname = usePathname()

  useEffect(() => {
    if (!isLoading && !isAuthenticated) router.push('/auth/login')
  }, [isLoading, isAuthenticated, router])

  if (isLoading) return (
    <div style={{ minHeight: '100vh', background: '#09090b', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
      <div style={{ color: '#a1a1aa', fontSize: 14 }}>Carregando...</div>
    </div>
  )

  if (!isAuthenticated) return null

  return (
    <div style={{ minHeight: '100vh', background: '#09090b', color: '#f4f4f5', fontFamily: 'var(--font-inter, Inter, sans-serif)' }}>
      {/* Header */}
      <header style={{ background: '#18181b', borderBottom: '1px solid #3f3f46', padding: '0 1.5rem', display: 'flex', alignItems: 'center', justifyContent: 'space-between', height: 56 }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '1.5rem' }}>
          <Link href="/chat" style={{ color: '#f97316', fontWeight: 800, fontSize: 18, textDecoration: 'none' }}>🛒 Comprai</Link>
          <nav style={{ display: 'flex', gap: '0.25rem' }}>
            {[
              { href: '/account', label: 'Conta' },
              { href: '/profile', label: 'Pedidos' },
            ].map(({ href, label }) => (
              <Link key={href} href={href} style={{
                padding: '4px 12px', borderRadius: 6, fontSize: 13, fontWeight: 500,
                textDecoration: 'none', transition: 'all .15s',
                background: pathname === href ? '#f97316' : 'transparent',
                color:      pathname === href ? '#fff'     : '#a1a1aa',
              }}>{label}</Link>
            ))}
          </nav>
        </div>
        <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
          <span style={{ fontSize: 13, color: '#a1a1aa' }}>{user?.name ?? user?.email}</span>
          {user?.image && <img src={user.image} alt="" style={{ width: 28, height: 28, borderRadius: '50%', objectFit: 'cover' }} />}
          <button onClick={signOut} style={{
            padding: '4px 12px', borderRadius: 6, border: '1px solid #3f3f46',
            background: 'transparent', color: '#a1a1aa', fontSize: 12, cursor: 'pointer',
          }}>Sair</button>
        </div>
      </header>
      <main style={{ maxWidth: 900, margin: '0 auto', padding: '2rem 1.5rem' }}>
        {children}
      </main>
    </div>
  )
}
