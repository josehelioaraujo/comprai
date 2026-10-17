import { auth } from '@/lib/auth'
import { redirect } from 'next/navigation'
import AppShell from '@/components/layout/AppShell'
import Link from 'next/link'

export default async function ProfilePage() {
  const session = await auth()
  if (!session) redirect('/auth/login')

  const user = session.user

  return (
    <AppShell hideNav>
      <div className="px-4 pt-12 pb-3 bg-white border-b border-border flex items-center gap-3">
        <Link href="/account" className="text-muted hover:text-ink transition-colors">
          <svg width="22" height="22" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" d="M15 19l-7-7 7-7"/>
          </svg>
        </Link>
        <h1 className="text-xl font-bold text-ink">Edit Profile</h1>
      </div>

      <div className="p-4 space-y-4">
        <div className="bg-white rounded-2xl shadow-card p-4 space-y-3">
          <div>
            <p className="text-xs font-medium text-muted uppercase tracking-wide mb-1">Name</p>
            <p className="text-sm text-ink">{user?.name ?? '—'}</p>
          </div>
          <div className="border-t border-border" />
          <div>
            <p className="text-xs font-medium text-muted uppercase tracking-wide mb-1">Email</p>
            <p className="text-sm text-ink">{user?.email ?? '—'}</p>
          </div>
        </div>

        <p className="text-xs text-center text-muted">
          Profile editing coming soon. Contact support to update your information.
        </p>
      </div>
    </AppShell>
  )
}
