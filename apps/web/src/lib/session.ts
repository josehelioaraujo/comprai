const SESSION_KEY = 'comprai_session_id'
const SESSION_TTL = 30 * 60 * 1000 // 30 minutos (mesmo TTL do Redis)

interface StoredSession {
  id: string
  createdAt: number
}

export function getSessionId(): string {
  if (typeof window === 'undefined') return generateUuid()

  try {
    const raw = localStorage.getItem(SESSION_KEY)
    if (raw) {
      const stored: StoredSession = JSON.parse(raw)
      const age = Date.now() - stored.createdAt
      if (age < SESSION_TTL) return stored.id
    }
  } catch {}

  return createSession()
}

export function createSession(): string {
  const id = generateUuid()
  try {
    const stored: StoredSession = { id, createdAt: Date.now() }
    localStorage.setItem(SESSION_KEY, JSON.stringify(stored))
  } catch {}
  return id
}

export function clearSession(): void {
  try {
    localStorage.removeItem(SESSION_KEY)
  } catch {}
}

export function generateUuid(): string {
  if (typeof crypto !== 'undefined' && crypto.randomUUID) {
    return crypto.randomUUID()
  }
  // fallback
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
    const r = (Math.random() * 16) | 0
    return (c === 'x' ? r : (r & 0x3) | 0x8).toString(16)
  })
}
