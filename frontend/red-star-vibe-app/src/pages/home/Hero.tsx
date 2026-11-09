import { Link } from 'react-router'
import type { User } from '@/features/auth'
import { env } from '@/shared/config/env'
import { ArrowRightIcon, StarIcon } from './Icons'

interface HeroProps {
  user: User | null | undefined
}

export function Hero({ user }: HeroProps) {
  return (
    <section className="hero" aria-labelledby="hero-heading">
      <div className="hero-copy">
        <p className="eyebrow">
          <StarIcon /> Welcome to {env.appName}
        </p>

        {user ? (
          <>
            <h1 id="hero-heading">
              Good to see you, <span className="accent">{user.displayName}</span>.
            </h1>
            <p className="lead">Your account is ready. Pick up where you left off or check today's live data below.</p>
            <div className="hero-actions">
              <Link to="/account" className="btn btn-primary">
                Go to my account <ArrowRightIcon />
              </Link>
              <a href="#live" className="btn btn-ghost">
                View live data
              </a>
            </div>
          </>
        ) : (
          <>
            <h1 id="hero-heading">
              Everything you need, <span className="accent">one secure place.</span>
            </h1>
            <p className="lead">
              Create your free account in under a minute. Fast, secure and built to work beautifully on any device.
            </p>
            <div className="hero-actions">
              <Link to="/register" className="btn btn-primary">
                Get started free <ArrowRightIcon />
              </Link>
              <Link to="/login" className="btn btn-ghost">
                I already have an account
              </Link>
            </div>
            <ul className="hero-trust" aria-label="Highlights">
              <li>No credit card</li>
              <li>Secure by default</li>
              <li>Sign up in 60 seconds</li>
            </ul>
          </>
        )}
      </div>

      <div className="hero-visual" aria-hidden="true">
        <div className="hero-card hero-card-main">
          <div className="hero-card-row">
            <span className="avatar">{user ? user.displayName.charAt(0).toUpperCase() : 'R'}</span>
            <div>
              <strong>{user ? user.displayName : 'Your profile'}</strong>
              <span className="muted">{user ? 'Signed in' : 'Ready in seconds'}</span>
            </div>
          </div>
          <div className="hero-bars">
            <span style={{ height: '45%' }} />
            <span style={{ height: '70%' }} />
            <span style={{ height: '55%' }} />
            <span style={{ height: '90%' }} />
            <span style={{ height: '65%' }} />
          </div>
        </div>
        <div className="hero-card hero-card-float">
          <ShieldBadge />
        </div>
      </div>
    </section>
  )
}

function ShieldBadge() {
  return (
    <>
      <span className="badge-dot" /> Account protected
    </>
  )
}
