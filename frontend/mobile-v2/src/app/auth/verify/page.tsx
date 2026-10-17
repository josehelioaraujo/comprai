import AppShell from '@/components/layout/AppShell'
import VerifyEmailForm from '@/components/auth/VerifyEmailForm'

export default function VerifyPage() {
  return (
    <AppShell hideNav>
      <div className="min-h-screen flex flex-col justify-center px-6 py-12">
        <div className="text-center mb-8">
          <div className="w-14 h-14 rounded-2xl bg-brand-light flex items-center justify-center mx-auto mb-3">
            <svg width="28" height="28" fill="none" stroke="#7C3AED" strokeWidth="1.8" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round"
                d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"/>
            </svg>
          </div>
          <h1 className="text-2xl font-bold text-ink">Verify your email</h1>
          <p className="text-sm text-muted mt-1">Check your inbox for the 6-digit code</p>
        </div>

        <VerifyEmailForm />
      </div>
    </AppShell>
  )
}
