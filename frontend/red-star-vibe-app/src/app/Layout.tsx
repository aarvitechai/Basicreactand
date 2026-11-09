import { NavLink, Outlet } from 'react-router'
import { env } from '@/shared/config/env'

export function Layout() {
  return (
    <div className="layout">
      <header className="app-header">
        <span className="brand">{env.appName}</span>
        <nav>
          <NavLink to="/" end>
            Home
          </NavLink>
        </nav>
      </header>
      <main className="app-main">
        <Outlet />
      </main>
    </div>
  )
}
