import { ApiError, http } from '@/shared/api/httpClient'
import type {
  ForgotPasswordRequest,
  LoginRequest,
  MessageResponse,
  RegisterRequest,
  ResetPasswordRequest,
  User,
} from '../types'

const base = '/api/v1/auth'

export const authApi = {
  register: (body: RegisterRequest) => http.post<User>(`${base}/register`, body),
  login: (body: LoginRequest) => http.post<User>(`${base}/login`, body),
  logout: () => http.post<void>(`${base}/logout`),
  forgotPassword: (body: ForgotPasswordRequest) => http.post<MessageResponse>(`${base}/forgot-password`, body),
  resetPassword: (body: ResetPasswordRequest) => http.post<MessageResponse>(`${base}/reset-password`, body),

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
