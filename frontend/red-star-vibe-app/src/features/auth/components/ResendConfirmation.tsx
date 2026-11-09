import { useState, type FormEvent } from 'react'
import { ApiError } from '@/shared/api/httpClient'
import { FormField } from '@/shared/components/FormField'
import { useResendConfirmation } from '../hooks/useAuth'
import { emailError } from '../validation'

interface ResendConfirmationProps {
  /** Known email (e.g. just registered). When omitted, the user is asked to type it. */
  email?: string
  label?: string
}

/** Button (or small form) that re-sends the confirmation link. The API always answers the same way. */
export function ResendConfirmation({ email: knownEmail, label = 'Resend confirmation email' }: ResendConfirmationProps) {
  const [email, setEmail] = useState(knownEmail ?? '')
  const [error, setError] = useState<string>()
  const resend = useResendConfirmation()

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault()
    const clientError = emailError(email)
    setError(clientError)
    if (!clientError) resend.mutate({ email: email.trim() })
  }

  const failure =
    resend.error instanceof ApiError && resend.error.status === 429
      ? 'Too many requests. Please wait a minute and try again.'
      : resend.isError
        ? 'Something went wrong. Please try again.'
        : null

  return (
    <form className="form" onSubmit={handleSubmit} noValidate>
      {!knownEmail && (
        <FormField
          label="Email"
          name="email"
          type="email"
          autoComplete="email"
          value={email}
          onChange={(e) => {
            setEmail(e.target.value)
            setError(undefined)
          }}
          error={error}
          required
        />
      )}
      <button type="submit" disabled={resend.isPending}>
        {resend.isPending ? 'Sending…' : label}
      </button>
      {resend.isSuccess && (
        <p className="notice notice-success" role="status">
          If that account still needs confirming, a new link is on its way. Check your inbox and spam folder.
        </p>
      )}
      {failure && (
        <p className="field-error" role="alert">
          {failure}
        </p>
      )}
    </form>
  )
}
