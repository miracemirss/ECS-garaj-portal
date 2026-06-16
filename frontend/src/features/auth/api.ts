import { httpGet, httpPost } from '@/services/api/http'
import type { AuthResult, AuthUser } from '@/types/models'

export interface LoginRequest {
  email: string
  password: string
}

export function login(body: LoginRequest) {
  return httpPost<AuthResult>('/auth/login', body)
}

export function logout(refreshToken: string) {
  return httpPost<void>('/auth/logout', { refreshToken })
}

export function getCurrentUser() {
  return httpGet<AuthUser>('/auth/me')
}
