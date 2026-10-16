'use client'

import { useState } from 'react'
import { useSession } from 'next-auth/react'
import { useRouter } from 'next/navigation'
import Image from 'next/image'

const BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5020'

export default function TotpSetupPage() {
  const { data: session } = useSession()
  const router = useRouter()

  const [step,       setStep]       = useState<'idle' | 'qr' | 'confirm' | 'done' | 'error'>('idle')
  const [secret,     setSecret]     = useState('')
  const [otpAuthUri, setOtpAuthUri] = useState('')
  const [code,       setCode]       = useState('')
  const [message,    setMessage]    = useState('')

  const token = (session?.user as any)?.token as string | undefined

  async function startSetup() {
    setStep('qr')
    setMessage('')
    try {
      const res = await fetch(`${BASE_URL}/api/auth/2fa/setup`, {
        method: 'POST',
        headers: { Authorization: `Bearer ${token}` },
      })
      const d = await res.json()
      if (!res.ok) throw new Error(d.error ?? 'Erro ao iniciar 2FA.')
      setSecret(d.secret)
      setOtpAuthUri(d.otpAuthUri)
    } catch (e: any) {
      setStep('error')
      setMessage(e.message)
    }
  }

  async function confirmCode(e: React.FormEvent) {
    e.preventDefault()
    setStep('confirm')
    setMessage('')
    try {
      const res = await fetch(`${BASE_URL}/api/auth/2fa/enable`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
        body: JSON.stringify({ code }),
      })
      const d = await res.json()
      if (res.ok) {
        setStep('done')
        setMessage(d.message ?? '2FA ativado!')
        setTimeout(() => router.push('/chat'), 2500)
      } else {
        setStep('qr')
        setMessage(d.error ?? 'Código inválido.')
      }
    } catch {
      setStep('qr')
      setMessage('Erro de conexão.')
    }
  }

  const qrUrl = otpAuthUri
    ? `https://api.qrserver.com/v1/create-qr-code/?size=200x200&data=${encodeURIComponent(otpAuthUri)}`
    : ''

  return (
    <div style={{
      minHeight: '100vh', background: '#0f0f1a',
      display: 'flex', alignItems: 'center', justifyContent: 'center',
      padding: '1rem', fontFamily: 'Inter, -apple-system, sans-serif',
    }}>
      <div style={{ width: '100%', maxWidth: 440, display: 'flex', flexDirection: 'column', gap: '1.5rem' }}>

        {/* Logo */}
        <div style={{ textAlign: 'center' }}>
          <div style={{ fontSize: 26, fontWeight: 800, color: '#ffffff', letterSpacing: -0.5 }}>
            🛒 Comprai
          </div>
          <p style={{ margin: '6px 0 0', fontSize: 13, color: '#6b7280' }}>
            Autenticação em dois fatores
          </p>
        </div>

        {/* Card */}
        <div style={{
          background: '#1a1a2e', borderRadius: 16, padding: '2rem',
          border: '1px solid #2d2d4a',
        }}>
          <h2 style={{ margin: '0 0 .5rem', fontSize: 20, fontWeight: 700, color: '#fff' }}>
            Configurar 2FA
          </h2>

          {step === 'idle' && (
            <>
              <p style={{ margin: '0 0 1.5rem', fontSize: 14, color: '#9ca3af', lineHeight: 1.5 }}>
                Adicione uma camada extra de segurança usando Google Authenticator, Authy ou qualquer
                aplicativo TOTP compatível.
              </p>
              <button
                onClick={startSetup}
                style={{
                  width: '100%', padding: '13px 0', borderRadius: 8, border: 'none',
                  background: '#ffffff', color: '#111827',
                  fontWeight: 700, fontSize: 15, cursor: 'pointer',
                }}
              >
                Começar configuração
              </button>
            </>
          )}

          {(step === 'qr' || step === 'confirm') && (
            <>
              <p style={{ margin: '0 0 1.25rem', fontSize: 14, color: '#9ca3af', lineHeight: 1.5 }}>
                1. Abra seu app autenticador e escaneie o QR Code abaixo.<br />
                2. Insira o código de 6 dígitos para confirmar.
              </p>

              {qrUrl && (
                <div style={{ display: 'flex', justifyContent: 'center', marginBottom: '1.25rem' }}>
                  {/* eslint-disable-next-line @next/next/no-img-element */}
                  <img
                    src={qrUrl}
                    alt="QR Code 2FA"
                    width={200} height={200}
                    style={{ borderRadius: 8, background: '#fff', padding: 8 }}
                  />
                </div>
              )}

              {secret && (
                <div style={{
                  background: '#0f0f1a', borderRadius: 8, padding: '10px 14px',
                  marginBottom: '1.25rem', textAlign: 'center',
                }}>
                  <p style={{ margin: '0 0 4px', fontSize: 11, color: '#6b7280', fontWeight: 600, textTransform: 'uppercase', letterSpacing: 1 }}>
                    Ou insira o código manualmente
                  </p>
                  <code style={{ fontSize: 14, color: '#a5b4fc', letterSpacing: '0.15rem', fontFamily: 'monospace' }}>
                    {secret}
                  </code>
                </div>
              )}

              <form onSubmit={confirmCode} style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
                <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
                  <label style={{ fontSize: 13, fontWeight: 500, color: '#d1d5db' }}>
                    Código de 6 dígitos
                  </label>
                  <input
                    value={code}
                    onChange={e => setCode(e.target.value.replace(/\D/g, '').slice(0, 6))}
                    maxLength={6}
                    placeholder="000000"
                    required
                    inputMode="numeric"
                    autoComplete="one-time-code"
                    style={{
                      padding: '12px 16px', borderRadius: 8,
                      border: '1px solid #3b3b6e', background: '#0f0f1a',
                      color: '#ffffff', fontSize: 28, fontWeight: 700,
                      letterSpacing: '0.6rem', textAlign: 'center',
                      outline: 'none', fontFamily: 'monospace',
                      boxSizing: 'border-box', width: '100%',
                    }}
                  />
                </div>

                {message && (
                  <p style={{
                    fontSize: 13, margin: 0, textAlign: 'center',
                    color: '#f87171',
                  }}>
                    {message}
                  </p>
                )}

                <button
                  type="submit"
                  disabled={code.length < 6 || step === 'confirm'}
                  style={{
                    padding: '13px 0', borderRadius: 8, border: 'none',
                    background: code.length < 6 || step === 'confirm' ? '#374151' : '#ffffff',
                    color: code.length < 6 || step === 'confirm' ? '#6b7280' : '#111827',
                    fontWeight: 700, fontSize: 15,
                    cursor: code.length < 6 ? 'not-allowed' : 'pointer',
                    transition: 'all .15s',
                  }}
                >
                  {step === 'confirm' ? 'Verificando…' : 'Ativar 2FA'}
                </button>
              </form>
            </>
          )}

          {step === 'done' && (
            <div style={{ textAlign: 'center', paddingTop: '1rem' }}>
              <div style={{ fontSize: 48, marginBottom: '1rem' }}>✅</div>
              <p style={{ fontSize: 16, color: '#4ade80', fontWeight: 700, margin: '0 0 .5rem' }}>
                2FA ativado com sucesso!
              </p>
              <p style={{ fontSize: 13, color: '#9ca3af', margin: 0 }}>
                Redirecionando…
              </p>
            </div>
          )}

          {step === 'error' && (
            <div style={{ textAlign: 'center', paddingTop: '1rem' }}>
              <p style={{ fontSize: 14, color: '#f87171', margin: '0 0 1rem' }}>{message}</p>
              <button
                onClick={() => setStep('idle')}
                style={{
                  padding: '11px 24px', borderRadius: 8, border: 'none',
                  background: '#3b3b6e', color: '#e5e7eb',
                  fontWeight: 600, fontSize: 14, cursor: 'pointer',
                }}
              >
                Tentar novamente
              </button>
            </div>
          )}
        </div>

        <p style={{ textAlign: 'center', fontSize: 13, color: '#6b7280', margin: 0 }}>
          <a href="/chat" style={{ color: '#a5b4fc', textDecoration: 'none' }}>
            ← Voltar ao início
          </a>
        </p>
      </div>
    </div>
  )
}
