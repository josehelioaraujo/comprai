'use client'

interface Props {
  version?: string
}

export default function MobileUcpHeader({ version }: Props) {
  return (
    <header className="w-full bg-zinc-950/90 backdrop-blur-md border-b border-zinc-900 flex items-center justify-center gap-2 py-3 shrink-0">
      <div className="w-6 h-6 rounded-lg bg-gradient-to-br from-emerald-500 to-teal-600 flex items-center justify-center shadow-md shadow-emerald-900/40">
        <svg viewBox="0 0 16 16" fill="none" className="w-3.5 h-3.5">
          <path d="M3 4h10M3 8h7M3 12h5" stroke="white" strokeWidth="2" strokeLinecap="round" />
          <circle cx="13" cy="11" r="2.5" fill="white" opacity="0.9" />
          <path d="M12 11l.8.8 1.5-1.5" stroke="#10b981" strokeWidth="1.2" strokeLinecap="round" strokeLinejoin="round" />
        </svg>
      </div>
      <div className="flex items-baseline gap-1.5">
        <span className="text-sm font-bold tracking-tight text-zinc-100">Comprai</span>
        {version && <span className="text-[9px] font-mono text-zinc-600 leading-none">v{version}</span>}
      </div>
    </header>
  )
}
