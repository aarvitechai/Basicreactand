import { Navigate, Outlet, useLocation } from 'react-router'
import { ErrorMessage } from '@/shared/components/ErrorMessage'
import { Spinner } from '@/shared/components/Spinner'
import { useCurrentUser } from '../hooks/useAuth'

export interface RedirectState {
  from?: string
}

/** Router state for showing a one-off success message on the sign-in page. */
export interface LoginNoticeState {
  notice?: string
}

/** Renders child routes only for signed-in users; otherwise redirects to /login and remembers where they were. */
export function RequireAuth() {
  const { data: user, isPending, isError, error, refetch } = useCurrentUser()
  const location = useLocation()

  if (isPending) return <Spinner label="Checking your session…" />
  if (isError) return <ErrorMessage error={error} onRetry={() => refetch()} />
  if (!user) {
    const state: RedirectState = { from: location.pathname + location.search }
    return <Navigate to="/login" replace state={state} />
  }
  return <Outlet />
}

/** For /login and /register: signed-in users are sent on to their account. */
export function GuestOnly() {
  const { data: user, isPending } = useCurrentUser()

  if (isPending) return <Spinner label="Checking your session…" />
  if (user) return <Navigate to="/account" replace />
  return <Outlet />
}
