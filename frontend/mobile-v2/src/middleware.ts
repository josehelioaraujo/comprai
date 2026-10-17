export { auth as middleware } from '@/lib/auth'

export const config = {
  matcher: ['/checkout', '/orders', '/orders/:path*', '/account/profile'],
}
