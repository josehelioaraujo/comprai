import { NextRequest, NextResponse } from 'next/server'

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5020'

export async function POST(req: NextRequest) {
  try {
    const { email, password } = await req.json()

    const backendRes = await fetch(`${API_URL}/api/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password }),
    })

    if (!backendRes.ok) {
      return NextResponse.json({ error: 'Credenciais inválidas.' }, { status: 401 })
    }

    const data = await backendRes.json() as {
      token: string
      customer: { role: string; id: string; name: string; email: string }
    }

    // Verificar se o usuário tem role admin
    if (data.customer?.role !== 'admin') {
      return NextResponse.json({ error: 'Acesso negado. Somente administradores.' }, { status: 403 })
    }

    const response = NextResponse.json({ ok: true, name: data.customer.name })

    // Salvar token em cookie HttpOnly seguro
    response.cookies.set('admin_token', data.token, {
      httpOnly: true,
      secure: process.env.NODE_ENV === 'production',
      sameSite: 'lax',
      maxAge: 60 * 60, // 1 hora
      path: '/',
    })

    return response
  } catch (err) {
    console.error('[admin/login] error:', err)
    return NextResponse.json({ error: 'Erro interno.' }, { status: 500 })
  }
}
