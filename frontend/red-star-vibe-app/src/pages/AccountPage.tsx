import { useCurrentUser } from '@/features/auth'

const dateFormat = new Intl.DateTimeFormat(undefined, { dateStyle: 'long' })

export function AccountPage() {
  // RequireAuth guarantees a user is present here.
  const { data: user } = useCurrentUser()
  if (!user) return null

  return (
    <>
      <h1>Welcome, {user.displayName}</h1>
      <section className="card">
        <h2>Your account</h2>
        <dl className="details">
          <dt>Name</dt>
          <dd>{user.displayName}</dd>
          <dt>Email</dt>
          <dd>{user.email}</dd>
          <dt>Member since</dt>
          <dd>{dateFormat.format(new Date(user.createdAtUtc))}</dd>
        </dl>
      </section>
    </>
  )
}
