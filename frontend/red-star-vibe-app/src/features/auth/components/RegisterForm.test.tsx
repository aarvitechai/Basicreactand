import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { renderWithProviders } from '@/test/renderWithProviders'
import { RegisterForm } from './RegisterForm'

const jsonResponse = (status: number, body: unknown) =>
  new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })

async function fillForm(overrides: Partial<Record<'name' | 'email' | 'password' | 'confirm', string>> = {}) {
  const user = userEvent.setup()
  const v = { name: 'Jane Doe', email: 'jane@example.com', password: 'Str0ng!Pass', confirm: 'Str0ng!Pass', ...overrides }
  await user.type(screen.getByLabelText('Name'), v.name)
  await user.type(screen.getByLabelText('Email'), v.email)
  await user.type(screen.getByLabelText('Password'), v.password)
  await user.type(screen.getByLabelText('Confirm password'), v.confirm)
  await user.click(screen.getByRole('button', { name: 'Create account' }))
}

describe('RegisterForm', () => {
  it('shows client-side errors and does not call the API when input is invalid', async () => {
    const fetchSpy = vi.spyOn(globalThis, 'fetch')
    renderWithProviders(<RegisterForm onSuccess={vi.fn()} />)

    await fillForm({ email: 'nope', password: 'weak', confirm: 'different' })

    expect(screen.getByText('Enter a valid email address.')).toBeInTheDocument()
    expect(screen.getByText(/Password needs:/)).toBeInTheDocument()
    expect(screen.getByText('Passwords do not match.')).toBeInTheDocument()
    expect(screen.getByLabelText('Email')).toHaveAttribute('aria-invalid', 'true')
    expect(fetchSpy).not.toHaveBeenCalled()
  })

  it('submits valid data and calls onSuccess with the created user', async () => {
    const created = { id: '1', email: 'jane@example.com', displayName: 'Jane Doe', createdAtUtc: '2026-01-01T00:00:00Z' }
    const fetchSpy = vi.spyOn(globalThis, 'fetch').mockResolvedValue(jsonResponse(201, created))
    const onSuccess = vi.fn()
    renderWithProviders(<RegisterForm onSuccess={onSuccess} />)

    await fillForm()

    await vi.waitFor(() => expect(onSuccess).toHaveBeenCalled())
    expect(onSuccess.mock.calls[0][0]).toEqual(created)
    const [url, init] = fetchSpy.mock.calls[0]
    expect(url).toBe('/api/v1/auth/register')
    expect(JSON.parse(init!.body as string)).toEqual({
      displayName: 'Jane Doe',
      email: 'jane@example.com',
      password: 'Str0ng!Pass',
    })
  })

  it('shows the server conflict message when the email is taken', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      jsonResponse(409, { status: 409, title: 'An account with this email already exists.' }),
    )
    renderWithProviders(<RegisterForm onSuccess={vi.fn()} />)

    await fillForm()

    expect(await screen.findByRole('alert')).toHaveTextContent('An account with this email already exists.')
  })

  it('maps server field errors onto the matching inputs', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      jsonResponse(400, { status: 400, title: 'Validation failed', errors: { Email: ['Email is invalid.'] } }),
    )
    renderWithProviders(<RegisterForm onSuccess={vi.fn()} />)

    await fillForm()

    expect(await screen.findByText('Email is invalid.')).toBeInTheDocument()
  })
})
