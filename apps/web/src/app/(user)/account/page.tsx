'use client'

import { useAuth } from '@/lib/useAuth'

export default function AccountPage() {
  const { user } = useAuth()

  return (
    <div>
      <h1 style={{ fontSize: 22, fontWeight: 700, marginBottom: '1.5rem', color: '#f4f4f5' }}>Minha Conta</h1>

      <div style={{ background: '#18181b', border: '1px solid #3f3f46', borderRadius: 12, padding: '1.5rem', display: 'flex', flexDirection: 'column', gap: '1rem' }}>
        {/* Avatar + nome */}
        <div style={{ display: 'flex', alignItems: 'center', gap: '1rem', paddingBottom: '1rem', borderBottom: '1px solid #3f3f46' }}>
          {user?.image
            ? <img src={user.image} alt="" style={{ width: 56, height: 56, borderRadius: '50%', objectFit: 'cover' }} />
            : <div style={{ width: 56, height: 56, borderRadius: '50%', background: '#f97316', display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 22, fontWeight: 700, color: '#fff' }}>
                {(user?.name ?? user?.email ?? '?')[0].toUpperCase()}
              </div>
          }
          <div>
            <div style={{ fontWeight: 600, fontSize: 16 }}>{user?.name ?? '—'}</div>
            <div style={{ color: '#a1a1aa', fontSize: 13 }}>{user?.email}</div>
          </div>
        </div>

        {/* Campos */}
        <Field label="Nome" value={user?.name ?? '—'} />
        <Field label="Email" value={user?.email ?? '—'} />
        <Field label="Provedor" value={user?.provider ?? 'credentials'} />
        <Field label="ID" value={user?.id ?? '—'} mono />
      </div>

      <p style={{ color: '#52525b', fontSize: 12, marginTop: '1rem' }}>
        Para alterar seus dados, entre em contato com o suporte.
      </p>
    </div>
  )
}

function Field({ label, value, mono }: { label: string; value: string; mono?: boolean }) {
  return (
    <div style={{ display: 'flex', gap: '1rem', alignItems: 'baseline' }}>
      <span style={{ width: 90, color: '#71717a', fontSize: 13, flexShrink: 0 }}>{label}</span>
      <span style={{ fontSize: 14, fontFamily: mono ? 'monospace' : undefined, color: '#f4f4f5', wordBreak: 'break-all' }}>{value}</span>
    </div>
  )
}
