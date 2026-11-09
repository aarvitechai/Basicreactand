import type { ReactNode } from 'react'
import { BoltIcon, ChartIcon, DeviceIcon, ShieldIcon } from './Icons'

interface Feature {
  icon: ReactNode
  title: string
  body: string
}

const FEATURES: Feature[] = [
  {
    icon: <ShieldIcon />,
    title: 'Secure by design',
    body: 'Strong password rules, automatic lockout after repeated failures and encrypted, httpOnly sessions.',
  },
  {
    icon: <BoltIcon />,
    title: 'Fast everywhere',
    body: 'A lightweight interface with smart caching, so pages load instantly and stay responsive.',
  },
  {
    icon: <DeviceIcon />,
    title: 'Works on any device',
    body: 'Designed mobile-first and fully accessible, from small phones to wide desktop screens.',
  },
  {
    icon: <ChartIcon />,
    title: 'Live data',
    body: 'Real-time information straight from our API, refreshed whenever you need it.',
  },
]

export function Features() {
  return (
    <section className="section" aria-labelledby="features-heading">
      <header className="section-header">
        <p className="eyebrow">Why choose us</p>
        <h2 id="features-heading">Built for the way you work</h2>
        <p className="muted">Everything is designed to be simple to use and safe by default.</p>
      </header>
      <ul className="feature-grid">
        {FEATURES.map((f) => (
          <li key={f.title} className="feature-card">
            <span className="feature-icon">{f.icon}</span>
            <h3>{f.title}</h3>
            <p className="muted">{f.body}</p>
          </li>
        ))}
      </ul>
    </section>
  )
}

const STEPS = [
  { title: 'Create your account', body: 'Just your name, email and a strong password.' },
  { title: 'You are signed in instantly', body: 'No waiting: your secure session starts right away.' },
  { title: 'Explore your dashboard', body: 'See your profile and live data whenever you like.' },
]

export function HowItWorks() {
  return (
    <section className="section" aria-labelledby="steps-heading">
      <header className="section-header">
        <p className="eyebrow">How it works</p>
        <h2 id="steps-heading">Up and running in three steps</h2>
      </header>
      <ol className="steps">
        {STEPS.map((s, i) => (
          <li key={s.title} className="step">
            <span className="step-number" aria-hidden="true">
              {i + 1}
            </span>
            <div>
              <h3>{s.title}</h3>
              <p className="muted">{s.body}</p>
            </div>
          </li>
        ))}
      </ol>
    </section>
  )
}
