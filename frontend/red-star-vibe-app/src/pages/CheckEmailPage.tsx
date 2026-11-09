import { Link, useLocation } from 'react-router'
import { ResendConfirmation } from '@/features/auth'

export interface CheckEmailState {
  email?: string
}

/** Shown right after registering: tells the user to click the link in their inbox. */
export function CheckEmailPage() {
  const email = (useLocation().state as CheckEmailState | null)?.email

  return (
    <div className="auth-page">
      <div className="card">
        <h1>Check your inbox</h1>
        <div className="form">
          <p className="form-intro">
            {email ? (
              <>
                We've sent a confirmation link to <strong>{email}</strong>.
              </>
            ) : (
              <>We've sent you a confirmation link.</>
            )}{' '}
            Click it to activate your account, then sign in. The link expires in 24 hours.
          </p>
          <p className="muted">Can't find it? Check your spam folder, or send it again.</p>
          <ResendConfirmation email={email} />
          <p className="muted form-footer">
            Already confirmed? <Link to="/login">Sign in</Link>
          </p>
        </div>
      </div>
    </div>
  )
}
