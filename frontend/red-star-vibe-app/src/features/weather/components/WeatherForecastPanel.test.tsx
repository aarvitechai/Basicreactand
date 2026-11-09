import { screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { renderWithProviders } from '@/test/renderWithProviders'
import type { WeatherForecast } from '../types'
import { WeatherForecastPanel } from './WeatherForecastPanel'

function mockFetch(status: number, body: unknown) {
  return vi.spyOn(globalThis, 'fetch').mockResolvedValue(
    new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } }),
  )
}

describe('WeatherForecastPanel', () => {
  it('renders forecasts returned by the API', async () => {
    const forecasts: WeatherForecast[] = [
      { date: '2026-01-11', temperatureC: 21, temperatureF: 70, summary: 'Mild' },
      { date: '2026-01-12', temperatureC: -3, temperatureF: 27, summary: 'Freezing' },
    ]
    const fetchSpy = mockFetch(200, forecasts)

    renderWithProviders(<WeatherForecastPanel />)

    expect(await screen.findByText('Mild')).toBeInTheDocument()
    expect(screen.getByText('Freezing')).toBeInTheDocument()
    expect(fetchSpy).toHaveBeenCalledWith('/api/v1/weather-forecasts?days=5', expect.anything())
  })

  it('shows the problem-details error from the API', async () => {
    mockFetch(400, { title: 'One or more validation errors occurred.', errors: { days: ['Must be between 1 and 14.'] } })

    renderWithProviders(<WeatherForecastPanel />)

    expect(await screen.findByRole('alert')).toHaveTextContent('Must be between 1 and 14.')
  })
})
