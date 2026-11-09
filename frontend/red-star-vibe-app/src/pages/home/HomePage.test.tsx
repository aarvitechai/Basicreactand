import { screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { renderWithProviders } from '@/test/renderWithProviders'
import { HomePage } from './HomePage'

const json = (status: number, body: unknown) =>
  new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })

function mockApi(me: { status: number; body: unknown }) {
  vi.spyOn(globalThis, 'fetch').mockImplementation(async (input) => {
    const url = String(input)
    if (url.includes('/auth/me')) return json(me.status, me.body)
    if (url.includes('/weather-forecasts')) return json(200, [])
    return json(404, {})
  })
}

describe('HomePage', () => {
  it('invites signed-out visitors to register or sign in', async () => {
    mockApi({ status: 401, body: { title: 'Unauthorized' } })
    renderWithProviders(<HomePage />)

    expect(await screen.findByRole('link', { name: /get started free/i })).toHaveAttribute('href', '/register')
    expect(screen.getByRole('link', { name: /already have an account/i })).toHaveAttribute('href', '/login')
    expect(screen.getByRole('heading', { name: /ready to get started/i })).toBeInTheDocument()
  })

  it('greets signed-in users and links to their account', async () => {
    mockApi({
      status: 200,
      body: { id: '1', email: 'jane@example.com', displayName: 'Jane', createdAtUtc: '2026-01-01T00:00:00Z' },
    })
    renderWithProviders(<HomePage />)

    expect(await screen.findByRole('heading', { name: /good to see you, jane/i })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /go to my account/i })).toHaveAttribute('href', '/account')
    expect(screen.queryByRole('link', { name: /get started free/i })).not.toBeInTheDocument()
  })
})
