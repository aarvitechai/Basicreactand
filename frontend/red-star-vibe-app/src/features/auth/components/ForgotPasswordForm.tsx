import { useState, type FormEvent } from 'react'
import { Link } from 'react-router'
import { ApiError } from '@/shared/api/httpClient'
import { FormField } from '@/shared/components/FormField'
import { useForgotPassword } from '../hooks/useAuth'
import { emailError } from '../validation'

export function ForgotPasswordForm() {
  const [email, setEmail] = useState('')
  const [error, setError] = useState<string>()
  const [formError, setFormError] = useState<string | null>(null)
  const forgot = useForgotPassword()

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault()
    setFormError(null)

    const clientError = emailError(email)
    setError(clientError)
    if (clientError) return

    forgot.mutate(
      { email: email.trim() },
      {
        onError: (err) =>
          setFormError(
            err instanceof ApiError && err.status === 429
              ? 'Too many requests. Please wait a minute and try again.'
              : 'Something went wrong. Please try again.',
          ),
      },
    )
  }

  if (forgot.isSuccess) {
    return (
      <div className="form">
        <div className="notice" role="status">
          <strong>Check your inbox</strong>
          <p>
            If an account exists for <strong>{email.trim()}</strong>, we've sent a link to reset your password. The link
            expires in 1 hour.
          </p>
          <p className="muted">Didn't get it? Check your spam folder, or try again in a few minutes.</p>
        </div>
        <button type="button" onClick={() => forgot.reset()}>
          Use a different email
        </button>
        <p className="muted form-footer">
          <Link to="/login">Back to sign in</Link>
        </p>
      </div>
    )
  }

  return (
    <form className="form" onSubmit={handleSubmit} noValidate>
      <p className="muted form-intro">Enter the email you registered with and we'll send you a link to reset your password.</p>

      {formError && (
        <div className="error" role="alert">
          <p>{formError}</p>
        </div>
      )}

      <FormField
        label="Email"
        name="email"
        type="email"
        autoComplete="email"
        value={email}
        onChange={(e) => {
          setEmail(e.target.value)
          if (error) setError(undefined)
        }}
        error={error}
        maxLength={256}
        required
      />

      <button type="submit" className="primary" disabled={forgot.isPending}>
        {forgot.isPending ? 'Sending…' : 'Send reset link'}
      </button>

      <p className="muted form-footer">
        Remembered it? <Link to="/login">Back to sign in</Link>
      </p>
    </form>
  )
}
