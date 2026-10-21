// NextAuth v5 re-exports User/Session from @auth/core/types, so module
// augmentation must target @auth/core/types (not next-auth) to take effect.

declare module '@auth/core/types' {
  interface User {
    apiToken?:        string
    customerId?:      string
    isEmailVerified?: boolean
  }

  interface Session {
    user: {
      id:              string
      name?:           string | null
      email?:          string | null
      image?:          string | null
      apiToken:        string
      isEmailVerified: boolean
    }
  }
}

declare module 'next-auth/jwt' {
  interface JWT {
    apiToken?:        string
    customerId?:      string
    isEmailVerified?: boolean
  }
}

export {}
