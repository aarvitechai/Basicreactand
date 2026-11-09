/**
 * Typed access to build-time environment variables (see `.env.example`).
 * Only variables prefixed with `VITE_` are exposed to the browser — never put secrets here.
 */
export const env = {
  /** Base URL for the backend API. Empty = same origin (dev proxy / nginx reverse proxy). */
  apiBaseUrl: (import.meta.env.VITE_API_BASE_URL ?? '').replace(/\/+$/, ''),
  appName: import.meta.env.VITE_APP_NAME ?? 'Red Star Vibe',
} as const
