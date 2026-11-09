import { env } from '@/shared/config/env'

/** RFC 7807 problem-details body returned by the backend on errors. */
export interface ProblemDetails {
  type?: string
  title?: string
  status?: number
  detail?: string
  errors?: Record<string, string[]>
}

export class ApiError extends Error {
  readonly status: number
  readonly problem?: ProblemDetails

  constructor(status: number, problem?: ProblemDetails) {
    super(problem?.title ?? `Request failed with status ${status}`)
    this.name = 'ApiError'
    this.status = status
    this.problem = problem
  }
}

/**
 * Field-level validation errors from a problem-details response, keyed by camelCase field name
 * (the backend may send "Email" or "email"). Errors not tied to a field are under "".
 */
export function getFieldErrors(error: unknown): Record<string, string> {
  if (!(error instanceof ApiError) || !error.problem?.errors) return {}
  const result: Record<string, string> = {}
  for (const [key, messages] of Object.entries(error.problem.errors)) {
    const field = key.replace(/^\$\./, '')
    const camel = field ? field.charAt(0).toLowerCase() + field.slice(1) : ''
    result[camel] = messages.join(' ')
  }
  return result
}

type Query = Record<string, string | number | boolean | undefined>

interface RequestOptions extends Omit<RequestInit, 'body'> {
  query?: Query
  body?: unknown
}

function buildUrl(path: string, query?: Query): string {
  const url = `${env.apiBaseUrl}${path}`
  if (!query) return url
  const params = new URLSearchParams()
  for (const [key, value] of Object.entries(query)) {
    if (value !== undefined) params.append(key, String(value))
  }
  const qs = params.toString()
  return qs ? `${url}?${qs}` : url
}

async function request<T>(path: string, { query, body, headers, ...init }: RequestOptions = {}): Promise<T> {
  const response = await fetch(buildUrl(path, query), {
    // Send the httpOnly auth cookie. Same-origin by default; also works cross-origin when CORS allows credentials.
    credentials: 'include',
    ...init,
    headers: {
      Accept: 'application/json',
      ...(body !== undefined && { 'Content-Type': 'application/json' }),
      ...headers,
    },
    body: body !== undefined ? JSON.stringify(body) : undefined,
  })

  if (!response.ok) {
    let problem: ProblemDetails | undefined
    try {
      problem = (await response.json()) as ProblemDetails
    } catch {
      problem = undefined
    }
    throw new ApiError(response.status, problem)
  }

  if (response.status === 204) return undefined as T
  return (await response.json()) as T
}

export const http = {
  get: <T>(path: string, options?: Omit<RequestOptions, 'body'>) => request<T>(path, { ...options, method: 'GET' }),
  post: <T>(path: string, body?: unknown, options?: RequestOptions) =>
    request<T>(path, { ...options, body, method: 'POST' }),
  put: <T>(path: string, body?: unknown, options?: RequestOptions) =>
    request<T>(path, { ...options, body, method: 'PUT' }),
  delete: <T>(path: string, options?: RequestOptions) => request<T>(path, { ...options, method: 'DELETE' }),
}
