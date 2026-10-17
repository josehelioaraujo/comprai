'use client'

import { useTheme } from 'next-themes'
import { useEffect, useState } from 'react'
import BottomNav from './BottomNav'

interface Props {
  children: React.ReactNode
  cartCount?: number
  hideNav?: boolean
}

function ThemeToggle() {
  const { theme, setTheme } = useTheme()
  const [mounted, setMounted] = useState(false)
  useEffect(() => setMounted(true), [])
  if (!mounted) return null

  const isDark = theme === 'dark'
  return (
    <button
      onClick={() => setTheme(isDark ? 'light' : 'dark')}
      aria-label={isDark ? 'Modo claro' : 'Modo escuro'}
      className="absolute top-3 right-3 z-50 w-9 h-9 rounded-full flex items-center justify-center bg-surface border border-border shadow-sm transition-colors hover:bg-border"
    >
      {isDark ? (
        /* Sol */
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className="text-amber-400">
          <circle cx="12" cy="12" r="5"/>
          <line x1="12" y1="1" x2="12" y2="3"/>
          <line x1="12" y1="21" x2="12" y2="23"/>
          <line x1="4.22" y1="4.22" x2="5.64" y2="5.64"/>
          <line x1="18.36" y1="18.36" x2="19.78" y2="19.78"/>
          <line x1="1" y1="12" x2="3" y2="12"/>
          <line x1="21" y1="12" x2="23" y2="12"/>
          <line x1="4.22" y1="19.78" x2="5.64" y2="18.36"/>
          <line x1="18.36" y1="5.64" x2="19.78" y2="4.22"/>
        </svg>
      ) : (
        /* Lua */
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className="text-ink">
          <path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"/>
        </svg>
      )}
    </button>
  )
}

export default function AppShell({ children, cartCount = 0, hideNav = false }: Props) {
  return (
    <div className="relative flex flex-col min-h-screen w-full max-w-[430px] mx-auto bg-[var(--bg-card)] shadow-2xl shadow-black/20">
      <ThemeToggle />
      <main className={['flex-1 overflow-y-auto', hideNav ? '' : 'pb-nav'].join(' ')}>
        {children}
      </main>
      {!hideNav && <BottomNav cartCount={cartCount} />}
    </div>
  )
}
