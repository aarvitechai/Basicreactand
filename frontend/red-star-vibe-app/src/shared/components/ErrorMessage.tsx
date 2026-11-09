import { ApiError } from '@/shared/api/httpClient'

interface ErrorMessageProps {
  error: unknown
  onRetry?: () => void
}

function describe(error: unknown): string {
  if (error instanceof ApiError) {
    const fieldErrors = error.problem?.errors ? Object.values(error.problem.errors).flat().join(' ') : ''
    return [error.message, fieldErrors].filter(Boolean).join(' ')
  }
  if (error instanceof Error) return error.message
  return 'Something went wrong.'
}

export function ErrorMessage({ error, onRetry }: ErrorMessageProps) {
  return (
    <div className="error" role="alert">
      <p>{describe(error)}</p>
      {onRetry && (
        <button type="button" onClick={onRetry}>
          Try again
        </button>
      )}
    </div>
  )
}
