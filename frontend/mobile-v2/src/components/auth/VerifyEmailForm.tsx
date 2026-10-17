'use client'

import { useState, useRef, useEffect } from 'react'
import { useSession } from 'next-auth/react'
import { useRouter } from 'next/navigation'
import Button from '@/components/ui/Button'
import { apiSendVerification, apiVerifyEmail } from '@/lib/api'

const CODE_LENGTH = 6

export default function VerifyEmailForm() {
  const { data: session } = useSession()
  const router = useRouter()

  const [digits, setDigits]     = useState<string[]>(Array(CODE_LENGTH).fill(''))
  const [error,  setError]      = useState('')
  const [info,   setInfo]       = useState('')
  const [sending,  setSending]  = useState(false)
  const [verifying, setVerifying] = useState(false)
  const inputRefs = useRef<(HTMLInputElement | null)[]>([])

  useEffect(() => {
    inputRefs.current[0]?.focus()
  }, [])

  async function handleSend() {
    if (!session?.user?.apiToken) return
    setSending(true)
    setError('')
    setInfo('')
    try {
      await apiSendVerification(session.user.apiToken)
      setInfo('Verification code sent to your email.')
    } catch {
      setError('Failed to send code. Please try again.')
    } finally {
      setSending(false)
    }
  }

  function handleDigitChange(index: number, value: string) {
    const clean = value.replace(/\D/g, '').slice(0, 1)
    const next  = [...digits]
    next[index] = clean
    setDigits(next)
    if (clean && index < CODE_LENGTH - 1) {
      inputRefs.current[index + 1]?.focus()
    }
  }

  function handleKeyDown(index: number, e: React.KeyboardEvent) {
    if (e.key === 'Backspace' && !digits[index] && index > 0) {
      inputRefs.current[index - 1]?.focus()
    }
  }

  async function handleVerify() {
    if (!session?.user?.apiToken) return
    const code = digits.join('')
    if (code.length < CODE_LENGTH) {
      setError('Please enter the full 6-digit code.')
      return
    }
    setError('')
    setVerifying(true)
    try {
      await apiVerifyEmail(code, session.user.apiToken)
      router.push('/account')
      router.refresh()
    } catch {
      setError('Invalid or expired code. Please request a new one.')
    } finally {
      setVerifying(false)
    }
  }

  return (
    <div className="space-y-6">
      <div className="text-center space-y-2">
        <p className="text-sm text-muted">
          Enter the 6-digit code we sent to <strong className="text-ink">{session?.user?.email}</strong>
        </p>
      </div>

      {/* OTP inputs */}
      <div className="flex gap-2 justify-center">
        {digits.map((d, i) => (
          <input
            key={i}
            ref={el => { inputRefs.current[i] = el }}
            type="text"
            inputMode="numeric"
            maxLength={1}
            value={d}
            onChange={e => handleDigitChange(i, e.target.value)}
            onKeyDown={e => handleKeyDown(i, e)}
            className={[
              'w-11 h-14 rounded-xl border text-center text-xl font-bold text-ink',
              'focus:outline-none focus:ring-2 focus:ring-brand/30 focus:border-brand',
              'transition-colors bg-surface',
              d ? 'border-brand' : 'border-border',
            ].join(' ')}
          />
        ))}
      </div>

      {error && <p className="text-sm text-red-500 text-center">{error}</p>}
      {info  && <p className="text-sm text-green-600 text-center">{info}</p>}

      <Button fullWidth onClick={handleVerify} loading={verifying}
        disabled={digits.join('').length < CODE_LENGTH}>
        Verify Email
      </Button>

      <div className="text-center">
        <button
          onClick={handleSend}
          disabled={sending}
          className="text-xs text-brand font-semibold disabled:opacity-50"
        >
          {sending ? 'Sending…' : "Didn't receive it? Resend code"}
        </button>
      </div>
    </div>
  )
}
