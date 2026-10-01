'use client'

import type { ChatMessage } from '@/types/ucp'

interface Props {
  message: ChatMessage
  children?: React.ReactNode  // slot para cards UCP (carousel, cart, etc.)
}

function renderText(text: string) {
  const parts = text.split(/(\*\*[^*]+\*\*)/)
  return parts.map((part, i) =>
    part.startsWith('**') && part.endsWith('**')
      ? <strong key={i} className="font-semibold text-zinc-100">{part.slice(2, -2)}</strong>
      : part
  )
}

function isError(text: string) {
  const lower = text.toLowerCase()
  return lower.startsWith('ops!') || lower.startsWith('erro') || lower.startsWith('não consegui')
}

function formatTime(date: Date) {
  return date.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })
}

export default function MobileChatBubble({ message, children }: Props) {
  const isBot  = message.role === 'bot'
  const isUser = message.role === 'user'
  const error  = isBot && isError(message.text)

  if (isUser) {
    return (
      <div className="flex justify-end">
        <div className="
          max-w-[80%] px-4 py-2.5
          bg-emerald-600 text-white
          rounded-2xl rounded-tr-none
          text-sm leading-relaxed shadow-sm
        ">
          {message.text}
          <div className="text-[10px] text-emerald-200 mt-1 text-right opacity-70">
            {formatTime(message.timestamp)}
          </div>
        </div>
      </div>
    )
  }

  return (
    <div className="flex flex-col gap-2">
      {/* Balão principal */}
      <div className="flex items-start gap-3 max-w-[88%]">
        {/* Avatar IA */}
        <div className="
          w-8 h-8 rounded-full shrink-0
          bg-gradient-to-tr from-emerald-500 to-teal-600
          flex items-center justify-center
          text-white text-xs font-bold shadow-md
        ">
          C
        </div>

        {/* Texto */}
        <div className={`
          rounded-2xl rounded-tl-none px-3.5 py-2.5 shadow-sm text-sm leading-relaxed
          ${error
            ? 'bg-red-950/60 border border-red-900/50 text-red-300'
            : 'bg-zinc-900 border border-zinc-800 text-zinc-200'
          }
        `}>
          {renderText(message.text)}
          <div className="text-[10px] text-zinc-500 mt-1 text-right">
            {formatTime(message.timestamp)}
          </div>
        </div>
      </div>

      {/* Slot para cards UCP — alinhado com recuo do balão */}
      {children && (
        <div className="pl-11">
          {children}
        </div>
      )}
    </div>
  )
}
