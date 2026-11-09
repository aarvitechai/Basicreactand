import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { renderWithProviders } from '@/test/renderWithProviders'
import { ForgotPasswordForm } from './ForgotPasswordForm'
import { ResetPasswordForm } from './ResetPasswordForm'

const json = (status: number, body: unknown) =>
  new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })

describe('ForgotPasswordForm', () => {
  it('validates the email before calling the API', async () => {
    const fetchSpy = vi.spyOn(globalThis, 'fetch')
    renderWithProviders(<ForgotPasswordForm />)
    const user = userEvent.setup()

    await user.type(screen.getByLabelText('Email'), 'not-an-email')
    await user.click(screen.getByRole('button', { name: 'Send reset link' }))

    expect(screen.getByText('Enter a valid email address.')).toBeInTheDocument()
    expect(fetchSpy).not.toHaveBeenCalled()
  })

  it('shows the same neutral confirmation after submitting', async () => {
    const fetchSpy = vi.spyOn(globalThis, 'fetch').mockResolvedValue(json(202, { message: 'ok' }))
    renderWithProviders(<ForgotPasswordForm />)
    const user = userEvent.setup()

    await user.type(screen.getByLabelText('Email'), 'jane@example.com')
    await user.click(screen.getByRole('button', { name: 'Send reset link' }))

    expect(await screen.findByText('Check your inbox')).toBeInTheDocument()
    expect(screen.getByRole('status')).toHaveTextContent('If an account exists for jane@example.com')
    const [url, init] = fetchSpy.mock.calls[0]
    expect(url).toBe('/api/v1/auth/forgot-password')
    expect(JSON.parse(init!.body as string)).toEqual({ email: 'jane@example.com' })
  })
})

describe('ResetPasswordForm', () => {
  const renderForm = (onSuccess = vi.fn()) => {
    renderWithProviders(<ResetPasswordForm email="jane@example.com" token="abc123" onSuccess={onSuccess} />)
    return { onSuccess, user: userEvent.setup() }
  }

  it('requires a policy-compliant, matching password', async () => {
    const fetchSpy = vi.spyOn(globalThis, 'fetch')
    const { user } = renderForm()

    await user.type(screen.getByLabelText('New password'), 'weak')
    await user.type(screen.getByLabelText('Confirm new password'), 'other')
    await user.click(screen.getByRole('button', { name: 'Reset password' }))

    expect(screen.getByText(/Password needs:/)).toBeInTheDocument()
    expect(screen.getByText('Passwords do not match.')).toBeInTheDocument()
    expect(fetchSpy).not.toHaveBeenCalled()
  })

  it('sends email, token and new password, then calls onSuccess', async () => {
    const fetchSpy = vi.spyOn(globalThis, 'fetch').mockResolvedValue(json(200, { message: 'done' }))
    const { user, onSuccess } = renderForm()

    await user.type(screen.getByLabelText('New password'), 'N3w!Passw0rd')
    await user.type(screen.getByLabelText('Confirm new password'), 'N3w!Passw0rd')
    await user.click(screen.getByRole('button', { name: 'Reset password' }))

    await vi.waitFor(() => expect(onSuccess).toHaveBeenCalled())
    const [url, init] = fetchSpy.mock.calls[0]
    expect(url).toBe('/api/v1/auth/reset-password')
    expect(JSON.parse(init!.body as string)).toEqual({
      email: 'jane@example.com',
      token: 'abc123',
      newPassword: 'N3w!Passw0rd',
    })
  })

  it('offers a new link when the token is expired or invalid', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      json(400, { status: 400, title: 'Validation', errors: { token: ['This reset link is invalid or has expired.'] } }),
    )
    const { user } = renderForm()

    await user.type(screen.getByLabelText('New password'), 'N3w!Passw0rd')
    await user.type(screen.getByLabelText('Confirm new password'), 'N3w!Passw0rd')
    await user.click(screen.getByRole('button', { name: 'Reset password' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('invalid or has expired')
    expect(screen.getByRole('link', { name: 'Request a new link' })).toHaveAttribute('href', '/forgot-password')
  })
})
