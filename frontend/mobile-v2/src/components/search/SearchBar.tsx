'use client'

import { useRef, useEffect } from 'react'

interface Props {
  value:       string
  onChange:    (v: string) => void
  onSubmit:    () => void
  placeholder?: string
  autoFocus?:  boolean
}

export default function SearchBar({ value, onChange, onSubmit, placeholder = 'Search products…', autoFocus }: Props) {
  const ref = useRef<HTMLInputElement>(null)

  useEffect(() => {
    if (autoFocus) ref.current?.focus()
  }, [autoFocus])

  return (
    <div className="relative flex items-center">
      <span className="absolute left-4 text-muted pointer-events-none">
        <svg width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"/>
        </svg>
      </span>
      <input
        ref={ref}
        type="search"
        value={value}
        onChange={e => onChange(e.target.value)}
        onKeyDown={e => e.key === 'Enter' && onSubmit()}
        placeholder={placeholder}
        className="w-full rounded-2xl border border-border bg-surface pl-11 pr-12 py-3 text-sm
          text-ink placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-brand/30
          focus:border-brand transition-colors"
      />
      {value && (
        <button
          onClick={() => onChange('')}
          className="absolute right-4 text-muted hover:text-ink transition-colors"
          aria-label="Clear"
        >
          <svg width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2.5" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12"/>
          </svg>
        </button>
      )}
    </div>
  )
}
