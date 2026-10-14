import { NextRequest, NextResponse } from 'next/server'

// Use private env var (runtime, not build-time)
const API_BASE_URL = process.env.API_BASE_URL ?? 'http://localhost:5020'

export async function POST(req: NextRequest) {
  try {
    const { email, password } = await req.json()

    const backendRes = await fetch(`${API_BASE_URL}/api/auth/login`, {
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

    const secure = process.env.COOKIE_SECURE === 'true'
    const maxAge = 60 * 60 * 8 // 8 horas

    // Token HttpOnly — inacessível pelo JS do browser
    response.cookies.set('admin_token', data.token, {
      httpOnly: true,
      secure,
      sameSite: 'lax',
      maxAge,
      path: '/',
    })

    // Info do usuário — legível pelo JS (para exibir nome no topbar)
    response.cookies.set('admin_user', JSON.stringify({
      name: data.customer.name,
      email: data.customer.email,
    }), {
      httpOnly: false,
      secure,
      sameSite: 'lax',
      maxAge,
      path: '/',
    })

    return response
  } catch (err) {
    console.error('[admin/login] error:', err)
    return NextResponse.json({ error: 'Erro interno.' }, { status: 500 })
  }
}
