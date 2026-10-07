export { auth as middleware } from '@/lib/auth'

export const config = {
  matcher: ['/chat/:path*', '/account/:path*', '/profile/:path*'],
}
