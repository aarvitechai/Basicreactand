// Public surface of the auth feature. Import from '@/features/auth' rather than deep paths.
export { ForgotPasswordForm } from './components/ForgotPasswordForm'
export { LoginForm } from './components/LoginForm'
export { RegisterForm } from './components/RegisterForm'
export { InvalidResetLink, ResetPasswordForm } from './components/ResetPasswordForm'
export { GuestOnly, RequireAuth, type LoginNoticeState, type RedirectState } from './components/RouteGuards'
export {
  authKeys,
  useCurrentUser,
  useForgotPassword,
  useLogin,
  useLogout,
  useRegister,
  useResetPassword,
} from './hooks/useAuth'
export type { User } from './types'
