// Public surface of the auth feature. Import from '@/features/auth' rather than deep paths.
export { ConfirmEmail, InvalidConfirmLink } from './components/ConfirmEmail'
export { ForgotPasswordForm } from './components/ForgotPasswordForm'
export { LoginForm } from './components/LoginForm'
export { PhoneVerification } from './components/PhoneVerification'
export { RegisterForm } from './components/RegisterForm'
export { ResendConfirmation } from './components/ResendConfirmation'
export { InvalidResetLink, ResetPasswordForm } from './components/ResetPasswordForm'
export { GuestOnly, RequireAuth, type LoginNoticeState, type RedirectState } from './components/RouteGuards'
export {
  authKeys,
  useConfirmEmail,
  useCurrentUser,
  useForgotPassword,
  useLogin,
  useLogout,
  useRegister,
  useResendConfirmation,
  useResetPassword,
  useSendPhoneCode,
  useVerifyPhone,
} from './hooks/useAuth'
export type { User } from './types'
