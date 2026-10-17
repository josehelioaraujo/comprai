import { NextResponse } from 'next/server'

export async function GET() {
  return NextResponse.json({ ok: true, service: 'comprai-mobile-v2', ts: Date.now() })
}
