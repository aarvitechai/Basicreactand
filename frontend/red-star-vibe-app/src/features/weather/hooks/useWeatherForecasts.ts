import { useQuery } from '@tanstack/react-query'
import { weatherApi } from '../api/weatherApi'

export const weatherKeys = {
  all: ['weather'] as const,
  forecasts: (days: number) => [...weatherKeys.all, 'forecasts', days] as const,
}

export function useWeatherForecasts(days: number) {
  return useQuery({
    queryKey: weatherKeys.forecasts(days),
    queryFn: ({ signal }) => weatherApi.getForecasts(days, signal),
  })
}
