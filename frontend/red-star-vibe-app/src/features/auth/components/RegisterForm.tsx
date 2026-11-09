import { useState, type ChangeEvent, type FormEvent } from 'react'
import { Link } from 'react-router'
import { ApiError, getFieldErrors } from '@/shared/api/httpClient'
import { FormField } from '@/shared/components/FormField'
import { useRegister } from '../hooks/useAuth'
import type { User } from '../types'
import { normalizePhone, PHONE_HINT, validateRegister, type FormErrors, type RegisterFormValues } from '../validation'
import { PasswordRules } from './PasswordRules'

interface RegisterFormProps {
  onSuccess: (user: User) => void
}

const initialValues: RegisterFormValues = {
  displayName: '',
  email: '',
  phoneNumber: '',
  password: '',
  confirmPassword: '',
}

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
      {
        displayName: values.displayName.trim(),
        email: values.email.trim(),
        password: values.password,
        phoneNumber: normalizePhone(values.phoneNumber) ?? undefined,
      },
      {
        onSuccess,
        onError: (error) => {
          const f = getFieldErrors(error)
          setErrors({ displayName: f.displayName, email: f.email, phoneNumber: f.phoneNumber, password: f.password })
          if (!(f.displayName || f.email || f.phoneNumber || f.password)) {
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
        label="Mobile number (optional)"
        name="phoneNumber"
        type="tel"
        inputMode="tel"
        autoComplete="tel"
        placeholder="+91 98765 43210"
        value={values.phoneNumber}
        onChange={update('phoneNumber')}
        error={errors.phoneNumber}
        maxLength={32}
        hint={`${PHONE_HINT} You can verify it by SMS after signing in.`}
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
