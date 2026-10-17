import type { NextConfig } from 'next'

const nextConfig: NextConfig = {
  output: 'standalone',
  images: {
    remotePatterns: [
      { protocol: 'https', hostname: '**' },
      { protocol: 'http',  hostname: '**' },
    ],
  },
  // /proxy/* is handled by src/app/proxy/[...path]/route.ts at runtime
  // (reads API_URL env var at request time, not at build time)
}

export default nextConfig
