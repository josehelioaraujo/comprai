import type { FulfillmentEvent } from '@/types'

interface SeedEvent {
  description: string
  location?: string
  occurredAt: string
}

interface Props {
  events: FulfillmentEvent[]
  /** Evento-semente exibido no final da timeline quando não há outros eventos
   *  (ex: "Pedido recebido" com a data de criação do pedido). */
  seedEvent?: SeedEvent
}

export default function TrackingTimeline({ events, seedEvent }: Props) {
  // Monta a lista combinada: eventos reais + seed no final (se não há nenhum real ainda)
  const hasReal = events.length > 0
  const list: (FulfillmentEvent | (SeedEvent & { trackingCode?: string }))[] = [
    ...events,
    ...(seedEvent && !hasReal ? [seedEvent] : []),
  ]

  if (!list.length) {
    return (
      <div className="text-center py-8 text-sm text-muted">
        Nenhum evento de rastreio ainda.
      </div>
    )
  }

  return (
    <div className="relative">
      {/* vertical line */}
      <div className="absolute left-4 top-2 bottom-2 w-0.5 bg-border" />

      <div className="space-y-4">
        {list.map((ev, i) => {
          const isFirst = i === 0
          return (
            <div key={i} className="relative flex gap-4 pl-4">
              {/* dot */}
              <div className={[
                'absolute left-0 top-2 w-4 h-4 rounded-full border-2 -translate-x-[7px] z-10',
                isFirst
                  ? 'bg-brand border-brand'
                  : 'bg-white border-border',
              ].join(' ')} />

              {/* content */}
              <div className="flex-1 pl-4 pb-1">
                <p className={['text-sm font-medium', isFirst ? 'text-ink' : 'text-muted'].join(' ')}>
                  {ev.description}
                </p>
                {ev.location && (
                  <p className="text-xs text-muted mt-0.5">{ev.location}</p>
                )}
                {ev.trackingCode && (
                  <p className="text-xs text-brand font-mono mt-0.5">{ev.trackingCode}</p>
                )}
                <p className="text-xs text-muted mt-1">
                  {new Date(ev.occurredAt).toLocaleString('pt-BR', {
                    month: 'short', day: 'numeric',
                    hour: '2-digit', minute: '2-digit',
                  })}
                </p>
              </div>
            </div>
          )
        })}
      </div>
    </div>
  )
}
