import type { WeatherForecast } from '../types'

interface WeatherTableProps {
  forecasts: WeatherForecast[]
}

const dateFormat = new Intl.DateTimeFormat(undefined, { weekday: 'short', month: 'short', day: 'numeric' })

export function WeatherTable({ forecasts }: WeatherTableProps) {
  if (forecasts.length === 0) {
    return <p className="muted">No forecasts available.</p>
  }

  return (
    <table className="table">
      <thead>
        <tr>
          <th scope="col">Date</th>
          <th scope="col">Temp (°C)</th>
          <th scope="col">Temp (°F)</th>
          <th scope="col">Summary</th>
        </tr>
      </thead>
      <tbody>
        {forecasts.map((f) => (
          <tr key={f.date}>
            <td>{dateFormat.format(new Date(`${f.date}T00:00:00`))}</td>
            <td>{f.temperatureC}</td>
            <td>{f.temperatureF}</td>
            <td>{f.summary ?? '—'}</td>
          </tr>
        ))}
      </tbody>
    </table>
  )
}
