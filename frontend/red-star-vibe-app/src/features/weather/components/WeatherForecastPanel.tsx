import { useState } from 'react'
import { ErrorMessage } from '@/shared/components/ErrorMessage'
import { Spinner } from '@/shared/components/Spinner'
import { useWeatherForecasts } from '../hooks/useWeatherForecasts'
import { WeatherTable } from './WeatherTable'

const DAY_OPTIONS = [3, 5, 7, 14] as const

export function WeatherForecastPanel() {
  const [days, setDays] = useState<number>(5)
  const { data, isPending, isError, error, refetch, isFetching } = useWeatherForecasts(days)

  return (
    <section className="card" aria-labelledby="weather-heading">
      <header className="card-header">
        <h2 id="weather-heading">Weather forecast</h2>
        <div className="controls">
          <label htmlFor="days">Days</label>
          <select id="days" value={days} onChange={(e) => setDays(Number(e.target.value))}>
            {DAY_OPTIONS.map((d) => (
              <option key={d} value={d}>
                {d}
              </option>
            ))}
          </select>
          <button type="button" onClick={() => refetch()} disabled={isFetching}>
            Refresh
          </button>
        </div>
      </header>

      {isPending ? (
        <Spinner label="Loading forecast…" />
      ) : isError ? (
        <ErrorMessage error={error} onRetry={() => refetch()} />
      ) : (
        <WeatherTable forecasts={data} />
      )}
    </section>
  )
}
