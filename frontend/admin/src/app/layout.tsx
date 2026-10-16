import type { Metadata } from 'next'
import './globals.css'

export const metadata: Metadata = {
  title: 'Comprai Admin',
  description: 'Painel administrativo Comprai',
}

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="pt-BR" className="dark">
      <body>{children}</body>
    </html>
  )
}
