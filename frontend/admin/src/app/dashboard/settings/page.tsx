'use client'

import { useState, useEffect, FormEvent } from 'react'
import { fetchProxy } from '@/lib/fetch-proxy'

interface MeResponse {
  name?: string
  phone?: string
  email?: string
}

export default function SettingsPage() {
  const [name, setName] = useState('')
  const [phone, setPhone] = useState('')
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [success, setSuccess] = useState(false)
  const [error, setError] = useState('')

  useEffect(() => {
    fetchProxy<MeResponse>('/api/auth/me')
      .then(data => {
        setName(data.name ?? '')
        setPhone(data.phone ?? '')
      })
      .catch(err => {
        // 'Unauthorized' → fetchProxy já redirecionou para /login
        if ((err as Error).message !== 'Unauthorized') {
          setError('Erro ao carregar dados.')
        }
      })
      .finally(() => setLoading(false))
  }, [])

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setSaving(true)
    setError('')
    setSuccess(false)
    try {
      await fetchProxy('/api/auth/me', {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ name, phone }),
      })
      setSuccess(true)
      // Atualizar o cookie admin_user com o novo nome (legível por JS)
      try {
        const existing = Object.fromEntries(
          document.cookie.split('; ').filter(Boolean).map(c => {
            const idx = c.indexOf('=')
            return [c.slice(0, idx), decodeURIComponent(c.slice(idx + 1))]
          })
        )
        const u = existing.admin_user ? JSON.parse(existing.admin_user) : {}
        const maxAge = 60 * 60 * 8
        document.cookie = `admin_user=${encodeURIComponent(JSON.stringify({ ...u, name }))}; path=/; max-age=${maxAge}; samesite=lax`
      } catch { /* ignore */ }
    } catch (err) {
      // 'Unauthorized' → fetchProxy já redirecionou para /login
      if ((err as Error).message !== 'Unauthorized') {
        setError((err as Error).message || 'Erro ao salvar.')
      }
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="max-w-lg">
      <div className="mb-6">
        <h2 className="text-xl font-bold text-white">Configurações</h2>
        <p className="text-gray-400 text-sm mt-1">Atualize os dados da sua conta</p>
      </div>

      <div className="bg-gray-900 rounded-2xl border border-gray-800 p-6">
        {loading ? (
          <div className="flex items-center justify-center py-8">
            <svg className="w-6 h-6 text-purple-500 animate-spin" fill="none" viewBox="0 0 24 24">
              <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
              <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8z" />
            </svg>
          </div>
        ) : (
          <form onSubmit={handleSubmit} className="space-y-5">
            {error && (
              <div className="p-3 rounded-lg bg-red-500/10 border border-red-500/30 text-red-400 text-sm">
                {error}
              </div>
            )}
            {success && (
              <div className="p-3 rounded-lg bg-green-500/10 border border-green-500/30 text-green-400 text-sm">
                Dados salvos com sucesso!
              </div>
            )}

            <div>
              <label className="block text-sm font-medium text-gray-300 mb-1.5">Nome</label>
              <input
                type="text"
                value={name}
                onChange={e => setName(e.target.value)}
                required
                className="w-full px-4 py-3 rounded-xl bg-gray-800 border border-gray-700 text-white
                           placeholder-gray-500 focus:outline-none focus:border-purple-500 focus:ring-1
                           focus:ring-purple-500 transition"
                placeholder="Seu nome"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-300 mb-1.5">Telefone</label>
              <input
                type="tel"
                value={phone}
                onChange={e => setPhone(e.target.value)}
                className="w-full px-4 py-3 rounded-xl bg-gray-800 border border-gray-700 text-white
                           placeholder-gray-500 focus:outline-none focus:border-purple-500 focus:ring-1
                           focus:ring-purple-500 transition"
                placeholder="(11) 99999-9999"
              />
            </div>

            <button
              type="submit"
              disabled={saving}
              className="w-full py-3 px-6 rounded-xl bg-purple-700 hover:bg-purple-600 text-white font-semibold
                         transition disabled:opacity-60 disabled:cursor-not-allowed"
            >
              {saving ? 'Salvando…' : 'Salvar alterações'}
            </button>
          </form>
        )}
      </div>
    </div>
  )
}
