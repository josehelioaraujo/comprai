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
  const inputRefs   = useRef<(HTMLInputElement | null)[]>([])
  const autoSentRef = useRef(false)

  // Auto-envia o código assim que a sessão estiver disponível (uma vez por mount)
  useEffect(() => {
    if (!autoSentRef.current && session?.user?.apiToken) {
      autoSentRef.current = true
      handleSend()
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [session?.user?.apiToken])

  useEffect(() => {
    inputRefs.current[0]?.focus()
  }, [])

  async function handleSend() {
    if (!session?.user?.apiToken) return
    setSending(true)
    setError('')
    setInfo('')
    try {
      await apiSendVerification(session.user.apiToken, session.user.email ?? undefined)
      setInfo('Código de verificação enviado para o seu e-mail.')
    } catch {
      setError('Falha ao enviar o código. Tente novamente.')
    } finally {
      setSending(false)
    }
  }

  function handleDigitChange(index: number, value: string) {
    const clean = value.replace(/[^A-Z0-9]/gi, '').toUpperCase().slice(0, 1)
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
      setError('Por favor insira o código completo de 6 dígitos.')
      return
    }
    setError('')
    setVerifying(true)
    try {
      await apiVerifyEmail(code, session.user.apiToken)
      router.push('/account')
      router.refresh()
    } catch {
      setError('Código inválido ou expirado. Solicite um novo código.')
    } finally {
      setVerifying(false)
    }
  }

  return (
    <div className="space-y-6">
      <div className="text-center space-y-2">
        <p className="text-sm text-muted">
          Digite o código de 6 dígitos enviado para <strong className="text-ink">{session?.user?.email}</strong>
        </p>
      </div>

      {/* OTP inputs */}
      <div className="flex gap-2 justify-center">
        {digits.map((d, i) => (
          <input
            key={i}
            ref={el => { inputRefs.current[i] = el }}
            type="text"
            inputMode="text"
            maxLength={1}
            autoCapitalize="characters"
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
        Verificar E-mail
      </Button>

      <div className="text-center">
        <button
          onClick={handleSend}
          disabled={sending}
          className="text-xs text-brand font-semibold disabled:opacity-50"
        >
          {sending ? 'Enviando código…' : 'Não recebeu? Reenviar código'}
        </button>
      </div>
    </div>
  )
}
