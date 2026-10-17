import { type ButtonHTMLAttributes } from 'react'
import Spinner from './Spinner'

type Variant = 'primary' | 'secondary' | 'ghost' | 'danger'

interface Props extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: Variant
  loading?: boolean
  fullWidth?: boolean
}

const styles: Record<Variant, string> = {
  primary:   'bg-brand text-white hover:bg-brand-dark active:scale-95',
  secondary: 'bg-brand-light text-brand hover:bg-purple-100 active:scale-95',
  ghost:     'bg-transparent text-ink hover:bg-surface active:scale-95',
  danger:    'bg-red-50 text-red-600 hover:bg-red-100 active:scale-95',
}

export default function Button({
  variant = 'primary',
  loading = false,
  fullWidth = false,
  children,
  disabled,
  className = '',
  ...props
}: Props) {
  return (
    <button
      {...props}
      disabled={disabled || loading}
      className={[
        'inline-flex items-center justify-center gap-2',
        'rounded-2xl px-5 py-3 text-sm font-semibold',
        'transition-all duration-150 select-none',
        'disabled:opacity-50 disabled:cursor-not-allowed',
        styles[variant],
        fullWidth ? 'w-full' : '',
        className,
      ].join(' ')}
    >
      {loading && <Spinner size={16} />}
      {children}
    </button>
  )
}
