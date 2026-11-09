import { ApiError, http } from '@/shared/api/httpClient'
import type {
  ConfirmEmailRequest,
  EmailOnlyRequest,
  ForgotPasswordRequest,
  LoginRequest,
  MessageResponse,
  RegisterRequest,
  ResetPasswordRequest,
  SendPhoneCodeRequest,
  User,
  VerifyPhoneRequest,
} from '../types'

const base = '/api/v1/auth'
const phone = '/api/v1/account/phone'

export const authApi = {
  register: (body: RegisterRequest) => http.post<User>(`${base}/register`, body),
  confirmEmail: (body: ConfirmEmailRequest) => http.post<MessageResponse>(`${base}/confirm-email`, body),
  resendConfirmation: (body: EmailOnlyRequest) => http.post<MessageResponse>(`${base}/resend-confirmation`, body),
  login: (body: LoginRequest) => http.post<User>(`${base}/login`, body),
  logout: () => http.post<void>(`${base}/logout`),
  forgotPassword: (body: ForgotPasswordRequest) => http.post<MessageResponse>(`${base}/forgot-password`, body),
  resetPassword: (body: ResetPasswordRequest) => http.post<MessageResponse>(`${base}/reset-password`, body),
  sendPhoneCode: (body: SendPhoneCodeRequest) => http.post<MessageResponse>(`${phone}/send-code`, body),
  verifyPhone: (body: VerifyPhoneRequest) => http.post<User>(`${phone}/verify`, body),

  /** Returns the signed-in user, or null when there is no valid session. */
  me: async (signal?: AbortSignal): Promise<User | null> => {
    try {
      return await http.get<User>(`${base}/me`, { signal })
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) return null
      throw error
    }
  },
}
