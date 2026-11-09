/** Mirrors the backend auth contracts in `RedStarVibe.Api/Contracts/AuthContracts.cs`. */
export interface User {
  id: string
  email: string
  displayName: string
  createdAtUtc: string
  emailConfirmed: boolean
  phoneNumber: string | null
  phoneNumberConfirmed: boolean
}

export interface RegisterRequest {
  email: string
  password: string
  displayName: string
  phoneNumber?: string
}

export interface ConfirmEmailRequest {
  userId: string
  token: string
}

export interface EmailOnlyRequest {
  email: string
}

export type ForgotPasswordRequest = EmailOnlyRequest

export interface ResetPasswordRequest {
  email: string
  token: string
  newPassword: string
}

export interface SendPhoneCodeRequest {
  phoneNumber: string
}

export interface VerifyPhoneRequest {
  phoneNumber: string
  code: string
}

export interface MessageResponse {
  message: string
}

export interface LoginRequest {
  email: string
  password: string
  rememberMe: boolean
}

/** Problem "code" returned with 403 when the password is right but the email isn't confirmed. */
export const EMAIL_NOT_CONFIRMED = 'email_not_confirmed'
