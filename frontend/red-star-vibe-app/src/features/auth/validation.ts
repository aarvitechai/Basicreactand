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

/** Returns an error message if the password breaks the policy, otherwise undefined. */
export function passwordError(password: string): string | undefined {
  const failed = PASSWORD_RULES.filter((r) => !r.test(password))
  if (failed.length > 0) return `Password needs: ${failed.map((r) => r.label.toLowerCase()).join(', ')}.`
  if (password.length > 128) return 'Password must be 128 characters or fewer.'
  return undefined
}

export function emailError(email: string): string | undefined {
  return EMAIL_PATTERN.test(email.trim()) ? undefined : 'Enter a valid email address.'
}

export function validateRegister(values: RegisterFormValues): FormErrors<RegisterFormValues> {
  const errors: FormErrors<RegisterFormValues> = {}
  const name = values.displayName.trim()

  if (name.length < 2) errors.displayName = 'Name must be at least 2 characters.'
  else if (name.length > 100) errors.displayName = 'Name must be 100 characters or fewer.'

  errors.email = emailError(values.email)
  errors.password = passwordError(values.password)
  if (values.confirmPassword !== values.password) errors.confirmPassword = 'Passwords do not match.'

  return withoutEmpty(errors)
}

export interface ResetPasswordFormValues {
  newPassword: string
  confirmPassword: string
}

export function validateResetPassword(values: ResetPasswordFormValues): FormErrors<ResetPasswordFormValues> {
  return withoutEmpty({
    newPassword: passwordError(values.newPassword),
    confirmPassword: values.confirmPassword !== values.newPassword ? 'Passwords do not match.' : undefined,
  })
}

/** Drops undefined entries so `Object.keys(errors).length` reflects only real errors. */
function withoutEmpty<T>(errors: FormErrors<T>): FormErrors<T> {
  return Object.fromEntries(Object.entries(errors).filter(([, v]) => v !== undefined)) as FormErrors<T>
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
