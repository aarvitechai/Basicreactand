import { NavLink, Outlet, useNavigate } from 'react-router'
import { useCurrentUser, useLogout } from '@/features/auth'
import { env } from '@/shared/config/env'

export function Layout() {
  const { data: user } = useCurrentUser()
  const logout = useLogout()
  const navigate = useNavigate()

  const handleLogout = () => logout.mutate(undefined, { onSettled: () => navigate('/', { replace: true }) })

  return (
    <div className="layout">
      <header className="app-header">
        <span className="brand">{env.appName}</span>
        <nav aria-label="Main">
          <NavLink to="/" end>
            Home
          </NavLink>
          {user ? (
            <>
              <NavLink to="/account">{user.displayName}</NavLink>
              <button type="button" className="link-button" onClick={handleLogout} disabled={logout.isPending}>
                Sign out
              </button>
            </>
          ) : (
            user === null && (
              <>
                <NavLink to="/login">Sign in</NavLink>
                <NavLink to="/register" className="nav-cta">
                  Register
                </NavLink>
              </>
            )
          )}
        </nav>
      </header>
      <main className="app-main">
        <Outlet />
      </main>
    </div>
  )
}
