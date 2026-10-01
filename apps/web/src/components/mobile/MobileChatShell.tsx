'use client'

interface Props {
  children: React.ReactNode
}

export default function MobileChatShell({ children }: Props) {
  return (
    <div className="min-h-screen bg-black flex items-center justify-center p-0 sm:p-6">
      <div
        className="
          w-full max-w-[420px]
          bg-zinc-950
          sm:rounded-[36px] sm:border-[8px] sm:border-zinc-800
          sm:shadow-[0_40px_80px_rgba(0,0,0,0.8)]
          flex flex-col overflow-hidden
        "
        style={{ height: '100dvh' }}
      >
        {children}
      </div>
    </div>
  )
}
