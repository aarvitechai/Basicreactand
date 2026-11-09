import { useLocation, useNavigate } from 'react-router'
import { LoginForm, type LoginNoticeState, type RedirectState } from '@/features/auth'

/** Only allow same-app relative redirects (prevents open-redirects via crafted state). */
function safeRedirect(from: string | undefined): string {
  return from && from.startsWith('/') && !from.startsWith('//') ? from : '/account'
}

export function LoginPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const state = (location.state as (RedirectState & LoginNoticeState) | null) ?? {}

  return (
    <div className="auth-page">
      <div className="card">
        <h1>Sign in</h1>
        {state.notice && (
          <div className="notice notice-success" role="status">
            <p>{state.notice}</p>
          </div>
        )}
        <LoginForm onSuccess={() => navigate(safeRedirect(state.from), { replace: true })} />
      </div>
    </div>
  )
}
