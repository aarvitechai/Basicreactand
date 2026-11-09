import { useLocation, useNavigate } from 'react-router'
import { LoginForm, type RedirectState } from '@/features/auth'

/** Only allow same-app relative redirects (prevents open-redirects via crafted state). */
function safeRedirect(from: string | undefined): string {
  return from && from.startsWith('/') && !from.startsWith('//') ? from : '/account'
}

export function LoginPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const from = (location.state as RedirectState | null)?.from

  return (
    <div className="auth-page">
      <div className="card">
        <h1>Sign in</h1>
        <LoginForm onSuccess={() => navigate(safeRedirect(from), { replace: true })} />
      </div>
    </div>
  )
}
