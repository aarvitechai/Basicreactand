import { useState, type ChangeEvent, type FormEvent } from 'react'
import { Link } from 'react-router'
import { ApiError, getFieldErrors } from '@/shared/api/httpClient'
import { FormField } from '@/shared/components/FormField'
import { useRegister } from '../hooks/useAuth'
import type { User } from '../types'
import { validateRegister, type FormErrors, type RegisterFormValues } from '../validation'
import { PasswordRules } from './PasswordRules'

interface RegisterFormProps {
  onSuccess: (user: User) => void
}

const initialValues: RegisterFormValues = { displayName: '', email: '', password: '', confirmPassword: '' }

export function RegisterForm({ onSuccess }: RegisterFormProps) {
  const [values, setValues] = useState(initialValues)
  const [errors, setErrors] = useState<FormErrors<RegisterFormValues>>({})
  const [formError, setFormError] = useState<string | null>(null)
  const register = useRegister()

  const update = (field: keyof RegisterFormValues) => (e: ChangeEvent<HTMLInputElement>) => {
    setValues((v) => ({ ...v, [field]: e.target.value }))
    if (errors[field]) setErrors((err) => ({ ...err, [field]: undefined }))
  }

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault()
    setFormError(null)

    const clientErrors = validateRegister(values)
    setErrors(clientErrors)
    if (Object.keys(clientErrors).length > 0) return

    register.mutate(
      { displayName: values.displayName.trim(), email: values.email.trim(), password: values.password },
      {
        onSuccess,
        onError: (error) => {
          const fieldErrors = getFieldErrors(error)
          setErrors({ displayName: fieldErrors.displayName, email: fieldErrors.email, password: fieldErrors.password })
          const hasFieldErrors = Boolean(fieldErrors.displayName || fieldErrors.email || fieldErrors.password)
          if (!hasFieldErrors) {
            setFormError(error instanceof ApiError ? error.message : 'Registration failed. Please try again.')
          }
        },
      },
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
        label="Name"
        name="displayName"
        autoComplete="name"
        value={values.displayName}
        onChange={update('displayName')}
        error={errors.displayName}
        maxLength={100}
        required
      />
      <FormField
        label="Email"
        name="email"
        type="email"
        autoComplete="email"
        value={values.email}
        onChange={update('email')}
        error={errors.email}
        maxLength={256}
        required
      />
      <FormField
        label="Password"
        name="password"
        type="password"
        autoComplete="new-password"
        value={values.password}
        onChange={update('password')}
        error={errors.password}
        maxLength={128}
        required
        hint={<PasswordRules password={values.password} />}
      />
      <FormField
        label="Confirm password"
        name="confirmPassword"
        type="password"
        autoComplete="new-password"
        value={values.confirmPassword}
        onChange={update('confirmPassword')}
        error={errors.confirmPassword}
        required
      />

      <button type="submit" className="primary" disabled={register.isPending}>
        {register.isPending ? 'Creating account…' : 'Create account'}
      </button>

      <p className="muted form-footer">
        Already have an account? <Link to="/login">Sign in</Link>
      </p>
    </form>
  )
}
