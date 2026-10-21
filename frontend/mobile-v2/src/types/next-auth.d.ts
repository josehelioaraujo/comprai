import type { DefaultSession } from 'next-auth'

declare module 'next-auth' {
  interface User {
    apiToken?:      string
    customerId?:    string
    emailVerified?: boolean
  }

  interface Session {
    user: {
      id:            string
      apiToken:      string
      emailVerified: boolean
    } & DefaultSession['user']
  }
}

declare module 'next-auth/jwt' {
  interface JWT {
    apiToken?:      string
    customerId?:    string
    emailVerified?: boolean
  }
}
