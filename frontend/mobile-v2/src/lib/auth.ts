import NextAuth    from 'next-auth'
import Credentials from 'next-auth/providers/credentials'
import Google      from 'next-auth/providers/google'
import type { User } from 'next-auth'
import type { AuthResponse } from '@/types'

const BASE_URL = process.env.API_URL ?? process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5020'

export const { handlers, signIn, signOut, auth } = NextAuth({
  providers: [
    Google({
      clientId:     process.env.GOOGLE_CLIENT_ID,
      clientSecret: process.env.GOOGLE_CLIENT_SECRET,
    }),
    Credentials({
      credentials: {
        email:    { label: 'Email',    type: 'email'    },
        password: { label: 'Password', type: 'password' },
        // pre-auth path: token já obtido externamente (register/2fa)
        token:    { label: 'Token',    type: 'text'     },
        customer: { label: 'Customer', type: 'text'     },
      },
      authorize: async (credentials): Promise<User | null> => {
        // pre-auth path — register/2fa já resolveram e passam token + customer direto
        if (credentials?.token && credentials?.customer) {
          try {
            const c = JSON.parse(credentials.customer as string) as AuthResponse['customer']
            return {
              id:            c.id,
              name:          c.name,
              email:         c.email,
              image:         c.avatarUrl ?? null,
              apiToken:      credentials.token as string,
              customerId:    c.id,
              emailVerified: c.emailVerified ?? false,
            }
          } catch { return null }
        }

        // standard login path
        try {
          const res = await fetch(`${BASE_URL}/api/auth/login`, {
            method:  'POST',
            headers: { 'Content-Type': 'application/json' },
            body:    JSON.stringify({ email: credentials?.email ?? '', password: credentials?.password ?? '' }),
          })
          if (!res.ok) return null
          const data = await res.json() as AuthResponse
          return {
            id:            data.customer.id,
            name:          data.customer.name,
            email:         data.customer.email,
            image:         data.customer.avatarUrl ?? null,
            apiToken:      data.token,
            customerId:    data.customer.id,
            emailVerified: data.customer.emailVerified ?? false,
          }
        } catch { return null }
      },
    }),
  ],

  callbacks: {
    async signIn({ user, account }) {
      if (account?.provider && account.provider !== 'credentials') {
        try {
          const res = await fetch(`${BASE_URL}/api/auth/callback`, {
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
          if (res.ok) {
            const data = await res.json() as AuthResponse
            user.apiToken   = data.token
            user.customerId = data.customer.id
          }
        } catch { /* best-effort */ }
      }
      return true
    },

    async jwt({ token, user, trigger, session: updateData }) {
      if (user) {
        token.apiToken      = user.apiToken
        token.customerId    = user.customerId ?? user.id
        token.emailVerified = (user as { emailVerified?: boolean }).emailVerified ?? false
      }
      // Atualização explícita após verificação de e-mail
      if (trigger === 'update' && updateData?.emailVerified === true) {
        token.emailVerified = true
      }
      return token
    },

    async session({ session, token }) {
      if (session.user) {
        session.user.id            = token.customerId    as string
        session.user.apiToken      = token.apiToken      as string
        session.user.emailVerified = Boolean(token.emailVerified)
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
