'use client'

import Image from 'next/image'
import { signOut } from 'next-auth/react'
import type { Session } from 'next-auth'
import Button from '@/components/ui/Button'
import Link from 'next/link'
import { resetSessionId } from '@/lib/session'

interface Props {
  session: Session
}

export default function ProfileCard({ session }: Props) {
  const user = session.user
  const initials = (user?.name ?? 'U')
    .split(' ').map(w => w[0]).slice(0, 2).join('').toUpperCase()

  return (
    <div className="space-y-4">
      {/* avatar + name */}
      <div className="flex items-center gap-4 bg-white rounded-2xl shadow-card p-4">
        <div className="w-16 h-16 rounded-2xl bg-brand-light flex items-center justify-center overflow-hidden shrink-0">
          {user?.image ? (
            <Image src={user.image} alt={user.name ?? ''} width={64} height={64}
              className="w-full h-full object-cover" unoptimized />
          ) : (
            <span className="text-xl font-bold text-brand">{initials}</span>
          )}
        </div>
        <div className="flex-1 min-w-0">
          <p className="font-semibold text-ink truncate">{user?.name ?? 'User'}</p>
          <p className="text-sm text-muted truncate">{user?.email}</p>
        </div>
      </div>

      {/* email verification banner */}
      {!user?.isEmailVerified && (
        <div className="flex items-center gap-3 bg-amber-50 border border-amber-200 rounded-2xl p-4">
          <svg width="20" height="20" fill="none" stroke="#D97706" strokeWidth="2" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round"
              d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"/>
          </svg>
          <div className="flex-1 min-w-0">
            <p className="text-xs font-semibold text-amber-800">E-mail não verificado</p>
            <p className="text-xs text-amber-700 mt-0.5">Verifique para completar seu cadastro</p>
          </div>
          <Link href="/auth/verify"
            className="text-xs font-bold text-amber-800 bg-amber-100 rounded-lg px-3 py-1.5 shrink-0">
            Verificar
          </Link>
        </div>
      )}

      {/* menu items */}
      <div className="bg-white rounded-2xl shadow-card overflow-hidden divide-y divide-border">
        <Link href="/orders" className="flex items-center gap-3 px-4 py-3.5 active:bg-surface transition-colors">
          <svg width="20" height="20" fill="none" stroke="currentColor" strokeWidth="1.8" className="text-brand" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round"
              d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2"/>
          </svg>
          <span className="flex-1 text-sm font-medium text-ink">Meus Pedidos</span>
          <svg width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2" className="text-muted" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" d="M9 5l7 7-7 7"/>
          </svg>
        </Link>
        <Link href="/account/profile" className="flex items-center gap-3 px-4 py-3.5 active:bg-surface transition-colors">
          <svg width="20" height="20" fill="none" stroke="currentColor" strokeWidth="1.8" className="text-brand" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round"
              d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"/>
          </svg>
          <span className="flex-1 text-sm font-medium text-ink">Editar Perfil</span>
          <svg width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2" className="text-muted" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" d="M9 5l7 7-7 7"/>
          </svg>
        </Link>
      </div>

      {/* sign out */}
      <Button
        variant="ghost"
        fullWidth
        onClick={() => { resetSessionId(); signOut({ callbackUrl: '/auth/login' }) }}
        className="!text-red-500 hover:!bg-red-50"
      >
        <svg width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round"
            d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1"/>
        </svg>
        Sair
      </Button>
    </div>
  )
}
