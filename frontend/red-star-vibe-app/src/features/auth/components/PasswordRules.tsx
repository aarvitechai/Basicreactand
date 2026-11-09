import { PASSWORD_RULES } from '../validation'

/** Live checklist of the password policy; each rule ticks green as it's satisfied. */
export function PasswordRules({ password }: { password: string }) {
  return (
    <ul className="password-rules">
      {PASSWORD_RULES.map((rule) => {
        const ok = rule.test(password)
        return (
          <li key={rule.label} className={ok ? 'ok' : undefined}>
            <span aria-hidden="true">{ok ? '✓' : '•'}</span> {rule.label}
          </li>
        )
      })}
    </ul>
  )
}
