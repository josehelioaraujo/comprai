/**
 * fetchProxy — wrapper para chamadas ao /api/proxy/* do admin.
 *
 * Em caso de 401 (token expirado ou ausente) redireciona para /login
 * automaticamente, evitando que o usuário fique preso com tela de erro.
 */
export async function fetchProxy<T>(
  path: string,
  options?: RequestInit,
): Promise<T> {
  const res = await fetch(`/api/proxy${path}`, options)

  if (res.status === 401) {
    // Sessão expirada: redireciona para login (full page reload para limpar estado)
    if (typeof window !== 'undefined') {
      window.location.replace('/login')
    }
    throw new Error('Unauthorized')
  }

  if (!res.ok) {
    const data = await res.json().catch(() => ({})) as Record<string, unknown>
    throw new Error((data.error as string) ?? `HTTP ${res.status}`)
  }

  return res.json() as Promise<T>
}
