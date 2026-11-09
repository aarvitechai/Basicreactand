import { isRouteErrorResponse, Link, useRouteError } from 'react-router'

/** Rendered by the router when a route throws or no route matches. */
export function RouteError() {
  const error = useRouteError()
  const notFound = isRouteErrorResponse(error) && error.status === 404

  return (
    <div className="centered">
      <h1>{notFound ? 'Page not found' : 'Something went wrong'}</h1>
      <p className="muted">
        {notFound ? "The page you're looking for doesn't exist." : 'An unexpected error occurred. Please try again.'}
      </p>
      <Link to="/">Back to home</Link>
    </div>
  )
}
