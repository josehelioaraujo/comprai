export const dynamic = 'force-dynamic'

import { Suspense } from 'react'
import AppShell from '@/components/layout/AppShell'
import LoginForm from '@/components/auth/LoginForm'

export default function LoginPage() {
  return (
    <AppShell hideNav>
      <div className="min-h-screen flex flex-col justify-center px-6 py-12">
        {/* logo */}
        <div className="text-center mb-8">
          <div className="w-14 h-14 rounded-2xl bg-brand flex items-center justify-center mx-auto mb-3">
            <svg width="28" height="28" fill="white" viewBox="0 0 20 20">
              <path d="M3 1a1 1 0 000 2h1.22l.305 1.222a.997.997 0 00.01.042l1.358 5.43-.893.892C3.74 11.846 4.632 14 6.414 14H15a1 1 0 000-2H6.414l1-1H14a1 1 0 00.894-.553l3-6A1 1 0 0017 3H6.28l-.31-1.243A1 1 0 005 1H3zM16 16.5a1.5 1.5 0 11-3 0 1.5 1.5 0 013 0zM6.5 18a1.5 1.5 0 100-3 1.5 1.5 0 000 3z"/>
            </svg>
          </div>
          <h1 className="text-2xl font-bold text-ink">Welcome back</h1>
          <p className="text-sm text-muted mt-1">Sign in to continue shopping</p>
        </div>

        <Suspense>
          <LoginForm />
        </Suspense>
      </div>
    </AppShell>
  )
}
