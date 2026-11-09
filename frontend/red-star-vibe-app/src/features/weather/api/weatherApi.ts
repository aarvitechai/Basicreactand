import { http } from '@/shared/api/httpClient'
import type { WeatherForecast } from '../types'

export const weatherApi = {
  getForecasts: (days: number, signal?: AbortSignal) =>
    http.get<WeatherForecast[]>('/api/v1/weather-forecasts', { query: { days }, signal }),
}
