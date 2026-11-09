/** Mirrors `WeatherForecastResponse` in the backend API contracts. */
export interface WeatherForecast {
  date: string
  temperatureC: number
  temperatureF: number
  summary: string | null
}
