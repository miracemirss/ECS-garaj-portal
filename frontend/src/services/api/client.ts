import axios from 'axios'
import { env } from '@/lib/env'
import { useAuthStore } from '@/features/auth/store'

/**
 * Shared Axios instance. Feature api/ modules build on top of this — components
 * never call axios directly.
 */
export const apiClient = axios.create({
  baseURL: env.apiBaseUrl,
  headers: { 'Content-Type': 'application/json' },
})

// Attach the access token to every request.
apiClient.interceptors.request.use((config) => {
  const token = useAuthStore.getState().accessToken
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

// Normalize errors; the refresh-token flow is wired up in the auth prompt.
apiClient.interceptors.response.use(
  (response) => response,
  (error) => Promise.reject(error),
)
