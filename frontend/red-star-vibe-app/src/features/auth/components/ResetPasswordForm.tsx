import { useState, type ChangeEvent, type FormEvent } from 'react'
import { Link } from 'react-router'
import { ApiError, getFieldErrors } from '@/shared/api/httpClient'
import { FormField } from '@/shared/components/FormField'
import { useResetPassword } from '../hooks/useAuth'
import { validateResetPassword, type FormErrors, type ResetPasswordFormValues } from '../validation'
import { PasswordRules } from './PasswordRules'

interface ResetPasswordFormProps {
  email: string
  token: string
  onSuccess: () => void
}

export function ResetPasswordForm({ email, token, onSuccess }: ResetPasswordFormProps) {
  const [values, setValues] = useState<ResetPasswordFormValues>({ newPassword: '', confirmPassword: '' })
  const [errors, setErrors] = useState<FormErrors<ResetPasswordFormValues>>({})
  const [linkError, setLinkError] = useState<string | null>(null)
  const [formError, setFormError] = useState<string | null>(null)
  const reset = useResetPassword()

  const update = (field: keyof ResetPasswordFormValues) => (e: ChangeEvent<HTMLInputElement>) => {
    setValues((v) => ({ ...v, [field]: e.target.value }))
    if (errors[field]) setErrors((err) => ({ ...err, [field]: undefined }))
  }

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault()
    setFormError(null)

    const clientErrors = validateResetPassword(values)
    setErrors(clientErrors)
    if (Object.keys(clientErrors).length > 0) return

    reset.mutate(
      { email, token, newPassword: values.newPassword },
      {
        onSuccess,
        onError: (error) => {
          const fieldErrors = getFieldErrors(error)
          if (fieldErrors.token) {
            setLinkError(fieldErrors.token)
            return
          }
          const passwordMessage = fieldErrors.password ?? fieldErrors.newPassword
          if (passwordMessage) {
            setErrors({ newPassword: passwordMessage })
            return
          }
          setFormError(
            error instanceof ApiError && error.status === 429
              ? 'Too many attempts. Please wait a minute and try again.'
              : 'Something went wrong. Please try again.',
          )
        },
      },
    )
  }

  if (linkError) {
    return <InvalidResetLink message={linkError} />
  }

  return (
    <form className="form" onSubmit={handleSubmit} noValidate>
      <p className="muted form-intro">
        Choose a new password for <strong>{email}</strong>.
      </p>

      {formError && (
        <div className="error" role="alert">
          <p>{formError}</p>
        </div>
      )}

      {/* Hidden username field helps password managers save the new password against the right account. */}
      <input type="email" name="username" autoComplete="username" value={email} readOnly hidden />

      <FormField
        label="New password"
        name="newPassword"
        type="password"
        autoComplete="new-password"
        value={values.newPassword}
        onChange={update('newPassword')}
        error={errors.newPassword}
        maxLength={128}
        required
        hint={<PasswordRules password={values.newPassword} />}
      />
      <FormField
        label="Confirm new password"
        name="confirmPassword"
        type="password"
        autoComplete="new-password"
        value={values.confirmPassword}
        onChange={update('confirmPassword')}
        error={errors.confirmPassword}
        required
      />

      <button type="submit" className="primary" disabled={reset.isPending}>
        {reset.isPending ? 'Saving…' : 'Reset password'}
      </button>
    </form>
  )
}

export function InvalidResetLink({ message }: { message?: string }) {
  return (
    <div className="form">
      <div className="error" role="alert">
        <p>{message ?? 'This reset link is invalid or incomplete. Please request a new one.'}</p>
      </div>
      <Link to="/forgot-password" className="btn btn-primary btn-block">
        Request a new link
      </Link>
      <p className="muted form-footer">
        <Link to="/login">Back to sign in</Link>
      </p>
    </div>
  )
}
