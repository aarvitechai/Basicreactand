import { PhoneVerification, useCurrentUser } from '@/features/auth'

const dateFormat = new Intl.DateTimeFormat(undefined, { dateStyle: 'long' })

export function AccountPage() {
  // RequireAuth guarantees a user is present here.
  const { data: user } = useCurrentUser()
  if (!user) return null

  return (
    <>
      <h1>Welcome, {user.displayName}</h1>
      <div className="account-grid">
        <section className="card" aria-labelledby="account-heading">
          <h2 id="account-heading">Your account</h2>
          <dl className="details">
            <dt>Name</dt>
            <dd>{user.displayName}</dd>
            <dt>Email</dt>
            <dd>
              {user.email}{' '}
              {user.emailConfirmed && <span className="badge badge-success">Verified</span>}
            </dd>
            <dt>Member since</dt>
            <dd>{dateFormat.format(new Date(user.createdAtUtc))}</dd>
          </dl>
        </section>

        <section className="card" aria-labelledby="phone-heading">
          <h2 id="phone-heading">Mobile number</h2>
          <p className="muted">
            {user.phoneNumberConfirmed
              ? 'Your number is verified.'
              : 'Add and verify a number with a one-time SMS code.'}
          </p>
          {/* key resets the card's local state when the saved number changes */}
          <PhoneVerification key={`${user.phoneNumber}-${user.phoneNumberConfirmed}`} user={user} />
        </section>
      </div>
    </>
  )
}
