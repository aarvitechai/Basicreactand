import { useState, type FormEvent } from 'react'
import { Link } from 'react-router'
import { ApiError } from '@/shared/api/httpClient'
import { FormField } from '@/shared/components/FormField'
import { useLogin } from '../hooks/useAuth'
import { EMAIL_NOT_CONFIRMED, type User } from '../types'
import { validateLogin, type FormErrors, type LoginFormValues } from '../validation'
import { ResendConfirmation } from './ResendConfirmation'

interface LoginFormProps {
  onSuccess: (user: User) => void
}

export function LoginForm({ onSuccess }: LoginFormProps) {
  const [values, setValues] = useState<LoginFormValues>({ email: '', password: '', rememberMe: false })
  const [errors, setErrors] = useState<FormErrors<LoginFormValues>>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [unconfirmedEmail, setUnconfirmedEmail] = useState<string | null>(null)
  const login = useLogin()

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault()
    setFormError(null)

    const clientErrors = validateLogin(values)
    setErrors(clientErrors)
    if (Object.keys(clientErrors).length > 0) return

    login.mutate(
      { ...values, email: values.email.trim() },
      {
        onSuccess,
        onError: (error) => {
          if (error instanceof ApiError && error.problem?.code === EMAIL_NOT_CONFIRMED) {
            setUnconfirmedEmail(values.email.trim())
            setFormError(error.message)
          } else if (error instanceof ApiError && error.status === 429) {
            setFormError('Too many attempts. Please wait a minute and try again.')
          } else {
            setFormError(error instanceof ApiError ? error.message : 'Sign-in failed. Please try again.')
          }
        },
      },
    )
  }

  if (unconfirmedEmail) {
    return (
      <div className="form">
        <div className="error" role="alert">
          <p>{formError}</p>
        </div>
        <ResendConfirmation email={unconfirmedEmail} />
        <button type="button" className="link-button" onClick={() => setUnconfirmedEmail(null)}>
          Back to sign in
        </button>
      </div>
    )
  }

  return (
    <form className="form" onSubmit={handleSubmit} noValidate>
      {formError && (
        <div className="error" role="alert">
          <p>{formError}</p>
        </div>
      )}

      <FormField
        label="Email"
        name="email"
        type="email"
        autoComplete="email"
        value={values.email}
        onChange={(e) => setValues((v) => ({ ...v, email: e.target.value }))}
        error={errors.email}
        required
      />
      <FormField
        label="Password"
        name="password"
        type="password"
        autoComplete="current-password"
        value={values.password}
        onChange={(e) => setValues((v) => ({ ...v, password: e.target.value }))}
        error={errors.password}
        required
      />

      <div className="form-row">
        <label className="checkbox">
          <input
            type="checkbox"
            checked={values.rememberMe}
            onChange={(e) => setValues((v) => ({ ...v, rememberMe: e.target.checked }))}
          />
          Keep me signed in
        </label>
        <Link to="/forgot-password" className="small-link">
          Forgot password?
        </Link>
      </div>

      <button type="submit" className="primary" disabled={login.isPending}>
        {login.isPending ? 'Signing in…' : 'Sign in'}
      </button>

      <p className="muted form-footer">
        New here? <Link to="/register">Create an account</Link>
      </p>
    </form>
  )
}
