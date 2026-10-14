import { NextRequest, NextResponse } from 'next/server'
import { cookies } from 'next/headers'

// Use private (non-NEXT_PUBLIC_) env var so it's read at runtime, not inlined at build time
const API_BASE_URL = process.env.API_BASE_URL ?? 'http://localhost:5020'

async function handler(req: NextRequest, { params }: { params: Promise<{ path: string[] }> }) {
  try {
    const { path } = await params
    const token = (await cookies()).get('admin_token')?.value

    if (!token) {
      return NextResponse.json({ error: 'Unauthorized' }, { status: 401 })
    }

    const url = new URL(req.url)
    const target = `${API_BASE_URL}/${path.join('/')}${url.search}`

    const res = await fetch(target, {
      method: req.method,
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
      body: req.method !== 'GET' && req.method !== 'HEAD' ? await req.text() : undefined,
    })

    // Se o backend rejeitar o token (expirado / revogado), limpar cookies
    // e retornar 401 para que o frontend redirecione para /login.
    if (res.status === 401) {
      const response = NextResponse.json(
        { error: 'Sessão expirada. Faça login novamente.' },
        { status: 401 },
      )
      const cookieOpts = { maxAge: 0, path: '/' } as const
      response.cookies.set('admin_token', '', cookieOpts)
      response.cookies.set('admin_user', '', cookieOpts)
      return response
    }

    const data = await res.text()
    return new NextResponse(data, {
      status: res.status,
      headers: { 'Content-Type': res.headers.get('Content-Type') ?? 'application/json' },
    })
  } catch (err) {
    console.error('[proxy] error:', err)
    return NextResponse.json({ error: 'Erro ao conectar ao servidor.' }, { status: 502 })
  }
}

export const GET    = handler
export const POST   = handler
export const PUT    = handler
export const DELETE = handler
export const PATCH  = handler
