import { Link } from 'react-router'
import { useCurrentUser } from '@/features/auth'
import { WeatherForecastPanel } from '@/features/weather'
import { Features, HowItWorks } from './Features'
import { Hero } from './Hero'
import { ArrowRightIcon } from './Icons'

export function HomePage() {
  const { data: user } = useCurrentUser()

  return (
    <div className="home">
      <Hero user={user} />
      <Features />
      {!user && <HowItWorks />}

      <section id="live" className="section" aria-label="Live data">
        <header className="section-header">
          <p className="eyebrow">Live preview</p>
          <h2>Straight from our API</h2>
          <p className="muted">This forecast is served by the backend in real time. Try changing the range.</p>
        </header>
        <WeatherForecastPanel />
      </section>

      {!user && (
        <section className="cta-band" aria-labelledby="cta-heading">
          <div>
            <h2 id="cta-heading">Ready to get started?</h2>
            <p>Join today. It's free and takes less than a minute.</p>
          </div>
          <Link to="/register" className="btn btn-light">
            Create my account <ArrowRightIcon />
          </Link>
        </section>
      )}
    </div>
  )
}
