import NextAuth         from 'next-auth'
import Google           from 'next-auth/providers/google'
import GitHub           from 'next-auth/providers/github'
import MicrosoftEntraId from 'next-auth/providers/microsoft-entra-id'
import Credentials      from 'next-auth/providers/credentials'
import type { User }    from 'next-auth'

const BASE_URL = process.env.API_URL ?? process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5020'
const tenantId = process.env.MICROSOFT_TENANT_ID ?? 'common'

export const { handlers, signIn, signOut, auth } = NextAuth({
  providers: [
    Google({
      clientId:     process.env.GOOGLE_CLIENT_ID,
      clientSecret: process.env.GOOGLE_CLIENT_SECRET,
    }),
    GitHub({
      clientId:     process.env.GITHUB_CLIENT_ID,
      clientSecret: process.env.GITHUB_CLIENT_SECRET,
    }),
    MicrosoftEntraId({
      clientId:     process.env.MICROSOFT_CLIENT_ID,
      clientSecret: process.env.MICROSOFT_CLIENT_SECRET,
      issuer:       `https://login.microsoftonline.com/${tenantId}/v2.0`,
    }),
    Credentials({
      credentials: {
        email:    { label: 'Email',    type: 'email'    },
        password: { label: 'Senha',    type: 'password' },
        // V060-F2: campos para autenticação pré-validada (pós-2FA ou registro)
        token:    { label: 'Token',    type: 'text'     },
        customer: { label: 'Customer', type: 'text'     },
      },
      authorize: async (credentials): Promise<User | null> => {
        try {
          // Caminho pré-autenticado: LoginPage ou 2FA verify já chamou o backend
          // e passa o token + customer diretamente.
          if (credentials?.token && credentials?.customer) {
            const c = JSON.parse(credentials.customer as string) as {
              id: string; name: string; email: string; avatarUrl?: string
            }
            return {
              id:       c.id,
              name:     c.name,
              email:    c.email,
              image:    c.avatarUrl ?? null,
              apiToken: credentials.token as string,
            } as User & { apiToken: string }
          }

          // Caminho legado (usado por SSO callback ou testes)
          const res = await fetch(`${BASE_URL}/api/auth/login`, {
            method:  'POST',
            headers: { 'Content-Type': 'application/json' },
            body:    JSON.stringify({
              email:    credentials?.email    ?? '',
              password: credentials?.password ?? '',
            }),
          })
          if (!res.ok) return null
          const data = await res.json() as {
            requires2fa?: boolean
            token?: string
            customer?: { id: string; name: string; email: string; avatarUrl?: string }
          }
          // Se exige 2FA, rejeita — o frontend deve redirecionar para /auth/2fa/verify
          if (data.requires2fa || !data.token || !data.customer) return null
          return {
            id:       data.customer.id,
            name:     data.customer.name,
            email:    data.customer.email,
            image:    data.customer.avatarUrl ?? null,
            apiToken: data.token,
          } as User & { apiToken: string }
        } catch {
          return null
        }
      },
    }),
  ],

  callbacks: {
    async signIn({ user, account }) {
      if (account?.provider && account.provider !== 'credentials') {
        try {
          await fetch(`${BASE_URL}/api/auth/callback`, {
            method:  'POST',
            headers: { 'Content-Type': 'application/json' },
            body:    JSON.stringify({
              provider:   account.provider,
              providerId: account.providerAccountId,
              name:       user.name  ?? '',
              email:      user.email ?? '',
              avatarUrl:  user.image ?? null,
            }),
          })
        } catch { /* best-effort */ }
      }
      return true
    },

    async jwt({ token, user, account }) {
      if (user) {
        token.customerId = user.id
        token.provider   = account?.provider ?? 'credentials'
        // V060-F2: preserva o JWT da API para chamadas autenticadas
        const u = user as User & { apiToken?: string }
        if (u.apiToken) token.apiToken = u.apiToken
      }
      return token
    },

    async session({ session, token }) {
      if (session.user) {
        session.user.id = token.customerId as string
        // V060-F2: expõe o token da API em session.user.token
        ;(session.user as any).token = token.apiToken as string | undefined
      }
      return session
    },
  },

  pages: {
    signIn: '/auth/login',
    error:  '/auth/login',
  },

  session: { strategy: 'jwt' },
})
