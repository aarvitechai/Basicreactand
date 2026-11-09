import { useEffect, useState } from 'react'
import { useLocation, useNavigate, useSearchParams } from 'react-router'
import { ConfirmEmail, InvalidConfirmLink } from '@/features/auth'

interface ConfirmLinkState {
  userId?: string
  token?: string
}

/** Landing page for the link in the confirmation email: /confirm-email?userId=...&token=... */
export function ConfirmEmailPage() {
  const [params] = useSearchParams()
  const location = useLocation()
  const navigate = useNavigate()

  const [link] = useState<ConfirmLinkState>(() => {
    const fromState = (location.state as ConfirmLinkState | null) ?? {}
    return { userId: params.get('userId') ?? fromState.userId, token: params.get('token') ?? fromState.token }
  })

  // Remove the token from the address bar so it isn't kept in browser history.
  useEffect(() => {
    if (params.has('token')) navigate('/confirm-email', { replace: true, state: link })
  }, [params, navigate, link])

  return (
    <div className="auth-page">
      <div className="card">
        <h1>Confirm your email</h1>
        {link.userId && link.token ? <ConfirmEmail userId={link.userId} token={link.token} /> : <InvalidConfirmLink />}
      </div>
    </div>
  )
}
