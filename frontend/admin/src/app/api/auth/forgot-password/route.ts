import { NextRequest, NextResponse } from 'next/server'

const API_BASE_URL = process.env.API_BASE_URL ?? 'http://localhost:5020'

export async function POST(req: NextRequest) {
  try {
    const body = await req.json()
    const backendRes = await fetch(`${API_BASE_URL}/api/auth/admin/forgot-password`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    })
    // Sempre retorna 204 (anti-enumeração)
    return backendRes.status === 204
      ? new NextResponse(null, { status: 204 })
      : NextResponse.json({ error: 'Erro interno.' }, { status: 500 })
  } catch {
    return NextResponse.json({ error: 'Erro de conexão.' }, { status: 500 })
  }
}
