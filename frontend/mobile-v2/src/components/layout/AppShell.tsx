'use client'

import BottomNav from './BottomNav'

interface Props {
  children: React.ReactNode
  cartCount?: number
  hideNav?: boolean
}

export default function AppShell({ children, cartCount = 0, hideNav = false }: Props) {
  return (
    <div className="flex flex-col min-h-screen w-full max-w-[430px] mx-auto bg-white shadow-2xl shadow-black/20">
      <main className={['flex-1 overflow-y-auto', hideNav ? '' : 'pb-nav'].join(' ')}>
        {children}
      </main>
      {!hideNav && <BottomNav cartCount={cartCount} />}
    </div>
  )
}
