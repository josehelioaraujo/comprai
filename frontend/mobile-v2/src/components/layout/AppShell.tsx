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
    /* Page background: bg-card on mobile (seamless), bg-phone on desktop (shows behind frame) */
    <div className="min-h-screen bg-[var(--bg-card)] sm:bg-[var(--bg-phone)] sm:flex sm:items-center sm:justify-center sm:py-10 sm:px-6 sm:min-h-screen">

      {/* ── Phone frame (sm+) ──────────────────────────────────────── */}
      <div className="relative w-full max-w-[430px] mx-auto
                      sm:rounded-[3.25rem]
                      sm:border-[10px] sm:border-gray-900
                      sm:shadow-[0_40px_100px_rgba(0,0,0,0.65),inset_0_0_0_1px_rgba(255,255,255,0.07)]">

        {/* Notch / Dynamic Island */}
        <div className="hidden sm:block absolute top-0 left-1/2 -translate-x-1/2
                        w-28 h-7 bg-gray-900 rounded-b-3xl z-50 pointer-events-none" />

        {/* Volume buttons (left) */}
        <div className="hidden sm:block absolute -left-[13px] top-28 w-[6px] h-8 bg-gray-800 rounded-l-full" />
        <div className="hidden sm:block absolute -left-[13px] top-40 w-[6px] h-8 bg-gray-800 rounded-l-full" />

        {/* Power button (right) */}
        <div className="hidden sm:block absolute -right-[13px] top-36 w-[6px] h-12 bg-gray-800 rounded-r-full" />

        {/* ── Screen ─────────────────────────────────────────────── */}
        <div className="relative flex flex-col min-h-screen sm:h-[812px] overflow-hidden
                        sm:rounded-[2.75rem] bg-[var(--bg-card)]">

          <ThemeToggle />

          {/* Status-bar spacer below notch (desktop only) */}
          <div className="hidden sm:block h-7 shrink-0" />

          <main className="flex-1 overflow-y-auto">
            {children}
          </main>

          {!hideNav && <BottomNav cartCount={cartCount} />}
        </div>
        {/* ── /Screen ────────────────────────────────────────────── */}

      </div>
      {/* ── /Phone frame ───────────────────────────────────────────── */}

    </div>
  )
}
