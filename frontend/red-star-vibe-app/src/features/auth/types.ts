/** Mirrors the backend auth contracts in `RedStarVibe.Api/Contracts/AuthContracts.cs`. */
export interface User {
  id: string
  email: string
  displayName: string
  createdAtUtc: string
}

export interface RegisterRequest {
  email: string
  password: string
  displayName: string
}

export interface LoginRequest {
  email: string
  password: string
  rememberMe: boolean
}
