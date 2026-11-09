// Public surface of the auth feature. Import from '@/features/auth' rather than deep paths.
export { LoginForm } from './components/LoginForm'
export { RegisterForm } from './components/RegisterForm'
export { GuestOnly, RequireAuth, type RedirectState } from './components/RouteGuards'
export { authKeys, useCurrentUser, useLogin, useLogout, useRegister } from './hooks/useAuth'
export type { User } from './types'
