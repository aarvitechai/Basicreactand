import { useNavigate } from 'react-router'
import { RegisterForm } from '@/features/auth'
import type { CheckEmailState } from './CheckEmailPage'

export function RegisterPage() {
  const navigate = useNavigate()

  return (
    <div className="auth-page">
      <div className="card">
        <h1>Create your account</h1>
        <RegisterForm
          onSuccess={(user) => {
            const state: CheckEmailState = { email: user.email }
            navigate('/check-email', { replace: true, state })
          }}
        />
      </div>
    </div>
  )
}
