import { type InputHTMLAttributes, forwardRef } from 'react'

interface Props extends InputHTMLAttributes<HTMLInputElement> {
  label?: string
  error?: string
}

const Input = forwardRef<HTMLInputElement, Props>(
  ({ label, error, className = '', ...props }, ref) => (
    <div className="flex flex-col gap-1">
      {label && (
        <label className="text-xs font-medium text-muted uppercase tracking-wide">
          {label}
        </label>
      )}
      <input
        ref={ref}
        {...props}
        className={[
          'w-full rounded-2xl border border-border bg-surface',
          'px-4 py-3 text-sm text-ink placeholder:text-muted',
          'focus:outline-none focus:ring-2 focus:ring-brand/30 focus:border-brand',
          'transition-colors',
          error ? 'border-red-400 focus:ring-red-200' : '',
          className,
        ].join(' ')}
      />
      {error && <p className="text-xs text-red-500">{error}</p>}
    </div>
  )
)
Input.displayName = 'Input'
export default Input
