import { NextRequest, NextResponse } from 'next/server'

export function middleware(req: NextRequest) {
  const token = req.cookies.get('admin_token')?.value
  const { pathname } = req.nextUrl

  // Rotas públicas
  if (pathname.startsWith('/login') || pathname.startsWith('/reset-password') || pathname.startsWith('/api/auth')) {
    // Se já autenticado e tentar acessar login, redireciona pro dashboard
    if (token && pathname === '/login') {
      return NextResponse.redirect(new URL('/dashboard', req.url))
    }
    return NextResponse.next()
  }

  // Proteger tudo mais
  if (!token) {
    return NextResponse.redirect(new URL('/login', req.url))
  }

  return NextResponse.next()
}

export const config = {
  matcher: ['/((?!_next/static|_next/image|favicon.ico).*)'],
}
