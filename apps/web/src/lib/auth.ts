import NextAuth      from 'next-auth'
import Google        from 'next-auth/providers/google'
import GitHub        from 'next-auth/providers/github'
import MicrosoftEntraId from 'next-auth/providers/microsoft-entra-id'
import Credentials   from 'next-auth/providers/credentials'

const BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5020'

export const { handlers, signIn, signOut, auth } = NextAuth({
  providers: [
    Google({
      clientId:     process.env.GOOGLE_CLIENT_ID!,
      clientSecret: process.env.GOOGLE_CLIENT_SECRET!,
    }),
    GitHub({
      clientId:     process.env.GITHUB_CLIENT_ID!,
      clientSecret: process.env.GITHUB_CLIENT_SECRET!,
    }),
    MicrosoftEntraId({
      clientId:     process.env.MICROSOFT_CLIENT_ID!,
      clientSecret: process.env.MICROSOFT_CLIENT_SECRET!,
      tenantId:     process.env.MICROSOFT_TENANT_ID ?? 'common',
    }),
    Credentials({
      credentials: {
        email:    { label: 'Email', type: 'email' },
        password: { label: 'Senha', type: 'password' },
      },
      async authorize(credentials) {
        try {
          const res = await fetch(`${BASE_URL}/api/auth/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email: credentials.email, password: credentials.password }),
          })
          if (!res.ok) return null
          const data = await res.json()
          return {
            id:       data.customer.id,
            name:     data.customer.name,
            email:    data.customer.email,
            image:    data.customer.avatarUrl ?? null,
            token:    data.token,
            provider: 'credentials',
          }
        } catch { return null }
      },
    }),
  ],

  callbacks: {
    async signIn({ user, account }) {
      if (account?.provider && account.provider !== 'credentials') {
        try {
          await fetch(`${BASE_URL}/api/auth/callback`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
              provider:   account.provider,
              providerId: account.providerAccountId,
              name:       user.name  ?? '',
              email:      user.email ?? '',
              avatarUrl:  user.image ?? null,
            }),
          })
        } catch { /* continua mesmo com erro */ }
      }
      return true
    },

    async jwt({ token, user, account }) {
      if (user) {
        token.customerId   = (user as any).id
        token.provider     = account?.provider ?? 'credentials'
        token.backendToken = (user as any).token ?? null
      }
      return token
    },

    async session({ session, token }) {
      session.user.id       = token.customerId as string
      session.user.provider = token.provider   as string
      ;(session as any).backendToken = token.backendToken
      return session
    },
  },

  pages: {
    signIn: '/auth/login',
    error:  '/auth/login',
  },

  session: { strategy: 'jwt' },
})
