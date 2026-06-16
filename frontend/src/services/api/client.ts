import axios, { type AxiosError, type AxiosRequestConfig } from 'axios'
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

apiClient.interceptors.request.use((config) => {
  const token = useAuthStore.getState().accessToken
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

let refreshing: Promise<string | null> | null = null

async function refreshAccessToken(): Promise<string | null> {
  const { refreshToken, setTokens, clear } = useAuthStore.getState()
  if (!refreshToken) {
    clear()
    return null
  }
  try {
    const res = await axios.post(`${env.apiBaseUrl}/auth/refresh`, { refreshToken })
    const data = res.data?.data
    if (data?.accessToken) {
      setTokens(data.accessToken, data.refreshToken)
      return data.accessToken as string
    }
  } catch {
    /* fall through */
  }
  clear()
  return null
}

apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const original = error.config as (AxiosRequestConfig & { _retry?: boolean }) | undefined
    const status = error.response?.status
    const url = original?.url ?? ''

    if (status === 401 && original && !original._retry && !url.includes('/auth/')) {
      original._retry = true
      refreshing = refreshing ?? refreshAccessToken()
      const token = await refreshing
      refreshing = null

      if (token) {
        original.headers = { ...original.headers, Authorization: `Bearer ${token}` }
        return apiClient(original)
      }
      if (typeof window !== 'undefined') {
        window.location.assign('/login')
      }
    }

    return Promise.reject(error)
  },
)
