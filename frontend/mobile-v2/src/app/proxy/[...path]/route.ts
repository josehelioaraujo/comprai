import { NextRequest, NextResponse } from 'next/server'

// Read at REQUEST TIME (runtime) — not at build time like next.config.ts rewrites
const API_URL = process.env.API_URL ?? 'http://localhost:5020'

type Context = { params: Promise<{ path: string[] }> }

async function handler(req: NextRequest, context: Context) {
  const { path } = await context.params
  const pathStr   = path.join('/')
  const { search } = new URL(req.url)
  const target    = `${API_URL}/${pathStr}${search}`

  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
  }
  const auth = req.headers.get('authorization')
  if (auth) headers['Authorization'] = auth

  const init: RequestInit = {
    method: req.method,
    headers,
    cache:  'no-store',
  }
  if (req.method !== 'GET' && req.method !== 'HEAD') {
    init.body = await req.text()
  }

  try {
    const res  = await fetch(target, init)
    const body = await res.text()
    return new NextResponse(body, {
      status:  res.status,
      headers: { 'Content-Type': res.headers.get('Content-Type') ?? 'application/json' },
    })
  } catch (err) {
    console.error(`[proxy] Failed to proxy ${target}`, err)
    return new NextResponse(JSON.stringify({ error: 'Proxy error' }), {
      status:  502,
      headers: { 'Content-Type': 'application/json' },
    })
  }
}

export const GET    = handler
export const POST   = handler
export const PUT    = handler
export const PATCH  = handler
export const DELETE = handler
