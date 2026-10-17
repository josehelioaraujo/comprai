import type { NextConfig } from 'next'

const API_URL = process.env.API_URL ?? 'http://localhost:5020'

const nextConfig: NextConfig = {
  output: 'standalone',
  images: {
    remotePatterns: [
      { protocol: 'https', hostname: '**' },
      { protocol: 'http',  hostname: '**' },
    ],
  },
  // Proxy /proxy/* → API (avoids CORS — browser only ever talks to same origin)
  async rewrites() {
    return [
      {
        source: '/proxy/:path*',
        destination: `${API_URL}/:path*`,
      },
    ]
  },
}

export default nextConfig
