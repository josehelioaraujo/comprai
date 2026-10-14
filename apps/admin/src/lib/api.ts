const API_URL = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5020'

export async function apiFetch<T>(
  path: string,
  token: string,
  options: RequestInit = {}
): Promise<T> {
  const res = await fetch(`${API_URL}${path}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`,
      ...options.headers,
    },
  })

  if (!res.ok) {
    const text = await res.text().catch(() => res.statusText)
    throw new Error(`API ${res.status}: ${text}`)
  }

  return res.json() as Promise<T>
}

export function formatCurrency(value: number): string {
  return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
}

export function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString('pt-BR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

export const STATUS_LABELS: Record<string, string> = {
  pending:    'Pendente',
  confirmed:  'Confirmado',
  processing: 'Processando',
  shipped:    'Enviado',
  delivered:  'Entregue',
  cancelled:  'Cancelado',
  returned:   'Devolvido',
}

export const STATUS_COLORS: Record<string, string> = {
  pending:    'bg-yellow-500/20 text-yellow-300',
  confirmed:  'bg-blue-500/20 text-blue-300',
  processing: 'bg-indigo-500/20 text-indigo-300',
  shipped:    'bg-purple-500/20 text-purple-300',
  delivered:  'bg-green-500/20 text-green-300',
  cancelled:  'bg-red-500/20 text-red-300',
  returned:   'bg-gray-500/20 text-gray-300',
}
