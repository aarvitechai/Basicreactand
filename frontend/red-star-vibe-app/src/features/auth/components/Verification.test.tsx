import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { renderWithProviders } from '@/test/renderWithProviders'
import type { User } from '../types'
import { ConfirmEmail } from './ConfirmEmail'
import { LoginForm } from './LoginForm'
import { PhoneVerification } from './PhoneVerification'

const json = (status: number, body: unknown) =>
  new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })

const baseUser: User = {
  id: '1',
  email: 'jane@example.com',
  displayName: 'Jane',
  createdAtUtc: '2026-01-01T00:00:00Z',
  emailConfirmed: true,
  phoneNumber: null,
  phoneNumberConfirmed: false,
}

describe('LoginForm with unconfirmed email', () => {
  it('explains the problem and offers to resend the confirmation link', async () => {
    const fetchSpy = vi
      .spyOn(globalThis, 'fetch')
      .mockResolvedValueOnce(
        json(403, { status: 403, title: 'Please confirm your email address before signing in.', code: 'email_not_confirmed' }),
      )
      .mockResolvedValueOnce(json(202, { message: 'ok' }))
    renderWithProviders(<LoginForm onSuccess={vi.fn()} />)
    const user = userEvent.setup()

    await user.type(screen.getByLabelText('Email'), 'jane@example.com')
    await user.type(screen.getByLabelText('Password'), 'Str0ng!Pass')
    await user.click(screen.getByRole('button', { name: 'Sign in' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('confirm your email')
    await user.click(screen.getByRole('button', { name: 'Resend confirmation email' }))

    expect(await screen.findByRole('status')).toHaveTextContent('a new link is on its way')
    const [url, init] = fetchSpy.mock.calls[1]
    expect(url).toBe('/api/v1/auth/resend-confirmation')
    expect(JSON.parse(init!.body as string)).toEqual({ email: 'jane@example.com' })
  })
})

describe('ConfirmEmail', () => {
  it('confirms automatically and links to sign in', async () => {
    const fetchSpy = vi.spyOn(globalThis, 'fetch').mockResolvedValue(json(200, { message: 'ok' }))
    renderWithProviders(<ConfirmEmail userId="u1" token="t1" />)

    expect(await screen.findByText('Your email is confirmed.')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Sign in' })).toHaveAttribute('href', '/login')
    expect(fetchSpy).toHaveBeenCalledTimes(1)
    expect(JSON.parse(fetchSpy.mock.calls[0][1]!.body as string)).toEqual({ userId: 'u1', token: 't1' })
  })

  it('offers a new link when the token is invalid or expired', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      json(400, { status: 400, errors: { token: ['This confirmation link is invalid or has expired.'] } }),
    )
    renderWithProviders(<ConfirmEmail userId="u1" token="bad" />)

    expect(await screen.findByRole('alert')).toHaveTextContent('invalid or has expired')
    expect(screen.getByRole('button', { name: 'Send a new link' })).toBeInTheDocument()
  })
})

describe('PhoneVerification', () => {
  it('rejects numbers without a country code before calling the API', async () => {
    const fetchSpy = vi.spyOn(globalThis, 'fetch')
    renderWithProviders(<PhoneVerification user={baseUser} />)
    const user = userEvent.setup()

    await user.type(screen.getByLabelText('Mobile number'), '98765 43210')
    await user.click(screen.getByRole('button', { name: 'Text me a code' }))

    expect(screen.getByText(/valid international number/)).toBeInTheDocument()
    expect(fetchSpy).not.toHaveBeenCalled()
  })

  it('sends a code to the normalized number, then verifies it', async () => {
    const verified = { ...baseUser, phoneNumber: '+919876543210', phoneNumberConfirmed: true }
    const fetchSpy = vi
      .spyOn(globalThis, 'fetch')
      .mockResolvedValueOnce(json(202, { message: 'sent' }))
      .mockResolvedValueOnce(json(200, verified))
    renderWithProviders(<PhoneVerification user={baseUser} />)
    const user = userEvent.setup()

    await user.type(screen.getByLabelText('Mobile number'), '+91 98765 43210')
    await user.click(screen.getByRole('button', { name: 'Text me a code' }))

    expect(await screen.findByText('+919876543210')).toBeInTheDocument()
    expect(JSON.parse(fetchSpy.mock.calls[0][1]!.body as string)).toEqual({ phoneNumber: '+919876543210' })
    expect(screen.getByRole('button', { name: /Resend code in/ })).toBeDisabled()

    await user.type(screen.getByLabelText('Verification code'), '123456')
    await user.click(screen.getByRole('button', { name: 'Verify' }))

    await vi.waitFor(() => expect(fetchSpy).toHaveBeenCalledTimes(2))
    const [url, init] = fetchSpy.mock.calls[1]
    expect(url).toBe('/api/v1/account/phone/verify')
    expect(JSON.parse(init!.body as string)).toEqual({ phoneNumber: '+919876543210', code: '123456' })
  })

  it('shows the server message for a wrong code', async () => {
    vi.spyOn(globalThis, 'fetch')
      .mockResolvedValueOnce(json(202, { message: 'sent' }))
      .mockResolvedValueOnce(json(400, { status: 400, errors: { code: ['That code is incorrect or has expired.'] } }))
    renderWithProviders(<PhoneVerification user={baseUser} />)
    const user = userEvent.setup()

    await user.type(screen.getByLabelText('Mobile number'), '+919876543210')
    await user.click(screen.getByRole('button', { name: 'Text me a code' }))
    await user.type(await screen.findByLabelText('Verification code'), '000000')
    await user.click(screen.getByRole('button', { name: 'Verify' }))

    expect(await screen.findByText('That code is incorrect or has expired.')).toBeInTheDocument()
  })
})
