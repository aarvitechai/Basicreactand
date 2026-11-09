import { useEffect, useState, type FormEvent } from 'react'
import { ApiError, getFieldErrors } from '@/shared/api/httpClient'
import { FormField } from '@/shared/components/FormField'
import { useSendPhoneCode, useVerifyPhone } from '../hooks/useAuth'
import type { User } from '../types'
import { normalizePhone, PHONE_HINT, phoneError } from '../validation'

const RESEND_COOLDOWN_SECONDS = 60

function describeSendError(error: unknown): string {
  if (error instanceof ApiError) {
    if (error.status === 429) return 'Too many codes requested. Please wait a while before trying again.'
    const field = getFieldErrors(error).phoneNumber
    if (field) return field
    return error.message
  }
  return 'We couldn’t send the code. Please try again.'
}

/** Account-page card: add/change a phone number and verify it with a 6-digit SMS code. */
export function PhoneVerification({ user }: { user: User }) {
  const [editing, setEditing] = useState(!user.phoneNumber)
  const [phone, setPhone] = useState(user.phoneNumber ?? '')
  const [phoneErr, setPhoneErr] = useState<string>()
  const [sentTo, setSentTo] = useState<string | null>(null)
  const [code, setCode] = useState('')
  const [codeErr, setCodeErr] = useState<string>()
  const [cooldown, setCooldown] = useState(0)
  const sendCode = useSendPhoneCode()
  const verify = useVerifyPhone()

  useEffect(() => {
    if (cooldown <= 0) return
    const id = window.setTimeout(() => setCooldown((s) => s - 1), 1000)
    return () => window.clearTimeout(id)
  }, [cooldown])

  const send = (e?: FormEvent) => {
    e?.preventDefault()
    const err = phoneError(phone, { required: true })
    setPhoneErr(err)
    if (err) return
    const normalized = normalizePhone(phone)!
    sendCode.mutate(
      { phoneNumber: normalized },
      {
        onSuccess: () => {
          setSentTo(normalized)
          setCode('')
          setCodeErr(undefined)
          setCooldown(RESEND_COOLDOWN_SECONDS)
        },
        onError: (error) => setPhoneErr(describeSendError(error)),
      },
    )
  }

  const submitCode = (e: FormEvent) => {
    e.preventDefault()
    if (!/^\d{6}$/.test(code.trim())) {
      setCodeErr('Enter the 6-digit code from the text message.')
      return
    }
    verify.mutate(
      { phoneNumber: sentTo!, code: code.trim() },
      {
        onSuccess: () => {
          setSentTo(null)
          setEditing(false)
        },
        onError: (error) =>
          setCodeErr(
            error instanceof ApiError && error.status === 429
              ? 'Too many attempts. Please wait a while and request a new code.'
              : (getFieldErrors(error).code ?? 'That code didn’t work. Please try again.'),
          ),
      },
    )
  }

  // Step 2: enter the code we just texted.
  if (sentTo) {
    return (
      <form className="form" onSubmit={submitCode} noValidate>
        <p className="muted form-intro">
          We texted a 6-digit code to <strong>{sentTo}</strong>.
        </p>
        <FormField
          label="Verification code"
          name="code"
          inputMode="numeric"
          autoComplete="one-time-code"
          pattern="\d{6}"
          maxLength={6}
          value={code}
          onChange={(e) => {
            setCode(e.target.value.replace(/\D/g, ''))
            setCodeErr(undefined)
          }}
          error={codeErr}
          required
        />
        <div className="form-row">
          <button type="submit" className="primary" disabled={verify.isPending}>
            {verify.isPending ? 'Verifying…' : 'Verify'}
          </button>
          <button type="button" className="link-button" onClick={() => send()} disabled={cooldown > 0 || sendCode.isPending}>
            {cooldown > 0 ? `Resend code in ${cooldown}s` : 'Resend code'}
          </button>
        </div>
        <button type="button" className="link-button" onClick={() => setSentTo(null)}>
          Use a different number
        </button>
      </form>
    )
  }

  // Verified and not editing: show status.
  if (!editing && user.phoneNumber) {
    return (
      <div className="phone-status">
        <span>{user.phoneNumber}</span>
        {user.phoneNumberConfirmed ? (
          <span className="badge badge-success">Verified</span>
        ) : (
          <span className="badge badge-warning">Not verified</span>
        )}
        <div className="form-row">
          {!user.phoneNumberConfirmed && (
            <button type="button" className="primary" onClick={() => send()} disabled={sendCode.isPending}>
              {sendCode.isPending ? 'Sending…' : 'Send verification code'}
            </button>
          )}
          <button type="button" onClick={() => setEditing(true)}>
            Change number
          </button>
        </div>
        {phoneErr && (
          <p className="field-error" role="alert">
            {phoneErr}
          </p>
        )}
      </div>
    )
  }

  // Step 1: enter a number.
  return (
    <form className="form" onSubmit={send} noValidate>
      <FormField
        label="Mobile number"
        name="phoneNumber"
        type="tel"
        inputMode="tel"
        autoComplete="tel"
        placeholder="+91 98765 43210"
        value={phone}
        onChange={(e) => {
          setPhone(e.target.value)
          setPhoneErr(undefined)
        }}
        error={phoneErr}
        hint={PHONE_HINT}
        maxLength={32}
        required
      />
      <div className="form-row">
        <button type="submit" className="primary" disabled={sendCode.isPending}>
          {sendCode.isPending ? 'Sending…' : 'Text me a code'}
        </button>
        {user.phoneNumber && (
          <button type="button" className="link-button" onClick={() => setEditing(false)}>
            Cancel
          </button>
        )}
      </div>
    </form>
  )
}
