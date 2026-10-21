import type { DefaultSession } from 'next-auth'

declare module 'next-auth' {
  interface User {
    apiToken?:      string
    customerId?:    string
    emailVerified?: boolean
  }

  interface Session {
    user: Omit<DefaultSession['user'], 'emailVerified'> & {
      id:            string
      apiToken:      string
      emailVerified: boolean
    }
  }
}

declare module 'next-auth/jwt' {
  interface JWT {
    apiToken?:      string
    customerId?:    string
    emailVerified?: boolean
  }
}
