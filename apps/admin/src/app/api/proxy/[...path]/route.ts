import { NextRequest, NextResponse } from 'next/server'
import { cookies } from 'next/headers'

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5020'

async function handler(req: NextRequest, { params }: { params: Promise<{ path: string[] }> }) {
  const { path } = await params
  const token = (await cookies()).get('admin_token')?.value

  if (!token) {
    return NextResponse.json({ error: 'Unauthorized' }, { status: 401 })
  }

  const url = new URL(req.url)
  const target = `${API_URL}/${path.join('/')}${url.search}`

  const res = await fetch(target, {
    method: req.method,
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`,
    },
    body: req.method !== 'GET' && req.method !== 'HEAD' ? await req.text() : undefined,
  })

  const data = await res.text()
  return new NextResponse(data, {
    status: res.status,
    headers: { 'Content-Type': res.headers.get('Content-Type') ?? 'application/json' },
  })
}

export const GET    = handler
export const POST   = handler
export const PUT    = handler
export const DELETE = handler
export const PATCH  = handler
