import { useNavigate } from 'react-router'
import { RegisterForm } from '@/features/auth'

export function RegisterPage() {
  const navigate = useNavigate()

  return (
    <div className="auth-page">
      <div className="card">
        <h1>Create your account</h1>
        <RegisterForm onSuccess={() => navigate('/account', { replace: true })} />
      </div>
    </div>
  )
}
