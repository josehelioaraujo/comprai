import { auth } from '@/lib/auth'
import { redirect } from 'next/navigation'
import AppShell from '@/components/layout/AppShell'
import ProfileCard from '@/components/account/ProfileCard'
import Link from 'next/link'
import Button from '@/components/ui/Button'

export default async function AccountPage() {
  const session = await auth()

  if (!session) {
    return (
      <AppShell>
        <div className="flex flex-col items-center justify-center min-h-[70vh] px-6 text-center">
          <div className="w-16 h-16 rounded-2xl bg-brand-light flex items-center justify-center mb-4">
            <svg width="32" height="32" fill="none" stroke="#7C3AED" strokeWidth="1.8" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round"
                d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"/>
            </svg>
          </div>
          <h2 className="text-xl font-bold text-ink">Entre na sua conta</h2>
          <p className="text-sm text-muted mt-2 mb-6">Acompanhe pedidos, gerencie seu perfil e muito mais.</p>
          <div className="w-full space-y-3">
            <Link href="/auth/login">
              <Button fullWidth>Entrar</Button>
            </Link>
            <Link href="/auth/register">
              <Button variant="secondary" fullWidth>Criar Conta</Button>
            </Link>
          </div>
        </div>
      </AppShell>
    )
  }

  return (
    <AppShell>
      <div className="px-4 pt-12 pb-3 bg-white border-b border-border">
        <h1 className="text-xl font-bold text-ink">Conta</h1>
      </div>
      <div className="p-4">
        <ProfileCard session={session} />
      </div>
    </AppShell>
  )
}
