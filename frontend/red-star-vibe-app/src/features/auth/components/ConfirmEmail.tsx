import { useEffect, useRef } from 'react'
import { Link } from 'react-router'
import { getFieldErrors } from '@/shared/api/httpClient'
import { Spinner } from '@/shared/components/Spinner'
import { useConfirmEmail } from '../hooks/useAuth'
import { ResendConfirmation } from './ResendConfirmation'

interface ConfirmEmailProps {
  userId: string
  token: string
}

/**
 * Confirms the email as soon as the page opens. Done with a POST from JavaScript (not on GET), so email
 * security scanners that pre-fetch links can't confirm accounts on the user's behalf.
 */
export function ConfirmEmail({ userId, token }: ConfirmEmailProps) {
  const confirm = useConfirmEmail()
  const started = useRef(false)

  useEffect(() => {
    // Guard against React StrictMode's double-invoked effects in development.
    if (started.current) return
    started.current = true
    confirm.mutate({ userId, token })
  }, [confirm, userId, token])

  if (confirm.isSuccess) {
    return (
      <div className="form">
        <div className="notice notice-success" role="status">
          <p>
            <strong>Your email is confirmed.</strong> You can now sign in.
          </p>
        </div>
        <Link to="/login" className="btn btn-primary btn-block">
          Sign in
        </Link>
      </div>
    )
  }

  if (confirm.isError) {
    const message =
      getFieldErrors(confirm.error).token ?? 'We couldn’t confirm your email. The link may be invalid or expired.'
    return (
      <div className="form">
        <div className="error" role="alert">
          <p>{message}</p>
        </div>
        <p className="muted form-intro">Enter your email and we'll send you a fresh confirmation link.</p>
        <ResendConfirmation label="Send a new link" />
      </div>
    )
  }

  return <Spinner label="Confirming your email…" />
}

export function InvalidConfirmLink() {
  return (
    <div className="form">
      <div className="error" role="alert">
        <p>This confirmation link is incomplete. Please use the full link from your email, or request a new one.</p>
      </div>
      <ResendConfirmation label="Send a new link" />
      <p className="muted form-footer">
        <Link to="/login">Back to sign in</Link>
      </p>
    </div>
  )
}
