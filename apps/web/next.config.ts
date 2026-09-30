import type { NextConfig } from 'next'

const nextConfig: NextConfig = {
  output: 'standalone',           // Docker standalone build
  images: {
    remotePatterns: [
      { protocol: 'https', hostname: '**' },  // produtos de qualquer CDN
      { protocol: 'http',  hostname: '**' },
    ],
  },
}

export default nextConfig
