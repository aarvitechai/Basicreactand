import { useEffect, useState } from 'react'
import { useLocation, useNavigate, useSearchParams } from 'react-router'
import { InvalidResetLink, ResetPasswordForm, type LoginNoticeState } from '@/features/auth'

interface ResetLinkState {
  email?: string
  token?: string
}

/** Landing page for the link in the reset email: /reset-password?email=...&token=... */
export function ResetPasswordPage() {
  const [params] = useSearchParams()
  const location = useLocation()
  const navigate = useNavigate()

  // Capture the link values once (from the URL, or from router state after we've cleaned the URL).
  const [link] = useState<ResetLinkState>(() => {
    const fromState = (location.state as ResetLinkState | null) ?? {}
    return { email: params.get('email') ?? fromState.email, token: params.get('token') ?? fromState.token }
  })

  // Remove the token from the address bar so it isn't kept in browser history or shared by copy-paste.
  useEffect(() => {
    if (params.has('token')) {
      navigate('/reset-password', { replace: true, state: link })
    }
  }, [params, navigate, link])

  const handleSuccess = () => {
    const state: LoginNoticeState = { notice: 'Your password has been reset. Sign in with your new password.' }
    navigate('/login', { replace: true, state })
  }

  return (
    <div className="auth-page">
      <div className="card">
        <h1>Set a new password</h1>
        {link.email && link.token ? (
          <ResetPasswordForm email={link.email} token={link.token} onSuccess={handleSuccess} />
        ) : (
          <InvalidResetLink />
        )}
      </div>
    </div>
  )
}
