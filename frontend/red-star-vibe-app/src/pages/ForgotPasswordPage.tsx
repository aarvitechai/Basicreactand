import { ForgotPasswordForm } from '@/features/auth'

export function ForgotPasswordPage() {
  return (
    <div className="auth-page">
      <div className="card">
        <h1>Forgot your password?</h1>
        <ForgotPasswordForm />
      </div>
    </div>
  )
}
