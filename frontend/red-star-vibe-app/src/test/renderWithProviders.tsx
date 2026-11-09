import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, type RenderOptions } from '@testing-library/react'
import type { ReactElement } from 'react'
import { MemoryRouter } from 'react-router'

/**
 * Renders a component inside a fresh, retry-less QueryClient and an in-memory router
 * so tests are isolated and fast.
 */
export function renderWithProviders(ui: ReactElement, options?: RenderOptions & { route?: string }) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } })
  const { route = '/', ...renderOptions } = options ?? {}
  return {
    client,
    ...render(
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={[route]}>{ui}</MemoryRouter>
      </QueryClientProvider>,
      renderOptions,
    ),
  }
}
