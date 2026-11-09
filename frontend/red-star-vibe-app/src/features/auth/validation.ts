// Client-side checks mirror the backend rules (Identity options in Infrastructure/DependencyInjection.cs)
// for instant feedback. The server remains the source of truth and re-validates everything.

export const PASSWORD_RULES: { test: (v: string) => boolean; label: string }[] = [
  { test: (v) => v.length >= 8, label: 'At least 8 characters' },
  { test: (v) => /[a-z]/.test(v), label: 'A lowercase letter' },
  { test: (v) => /[A-Z]/.test(v), label: 'An uppercase letter' },
  { test: (v) => /\d/.test(v), label: 'A number' },
  { test: (v) => /[^a-zA-Z0-9]/.test(v), label: 'A symbol' },
]

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

export interface RegisterFormValues {
  displayName: string
  email: string
  password: string
  confirmPassword: string
}

export type FormErrors<T> = Partial<Record<keyof T, string>>

export function validateRegister(values: RegisterFormValues): FormErrors<RegisterFormValues> {
  const errors: FormErrors<RegisterFormValues> = {}
  const name = values.displayName.trim()

  if (name.length < 2) errors.displayName = 'Name must be at least 2 characters.'
  else if (name.length > 100) errors.displayName = 'Name must be 100 characters or fewer.'

  if (!EMAIL_PATTERN.test(values.email.trim())) errors.email = 'Enter a valid email address.'

  const failed = PASSWORD_RULES.filter((r) => !r.test(values.password))
  if (failed.length > 0) errors.password = `Password needs: ${failed.map((r) => r.label.toLowerCase()).join(', ')}.`
  else if (values.password.length > 128) errors.password = 'Password must be 128 characters or fewer.'

  if (values.confirmPassword !== values.password) errors.confirmPassword = 'Passwords do not match.'

  return errors
}

export interface LoginFormValues {
  email: string
  password: string
  rememberMe: boolean
}

export function validateLogin(values: LoginFormValues): FormErrors<LoginFormValues> {
  const errors: FormErrors<LoginFormValues> = {}
  if (!EMAIL_PATTERN.test(values.email.trim())) errors.email = 'Enter a valid email address.'
  if (!values.password) errors.password = 'Enter your password.'
  return errors
}
