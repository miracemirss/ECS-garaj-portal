import { create } from 'zustand'
import { persist } from 'zustand/middleware'
import type { AuthUser } from '@/types/models'

interface AuthState {
  accessToken: string | null
  refreshToken: string | null
  user: AuthUser | null
  setSession: (accessToken: string, refreshToken: string, user: AuthUser) => void
  setTokens: (accessToken: string, refreshToken: string) => void
  setUser: (user: AuthUser | null) => void
  clear: () => void
}

/** Client-side auth state (tokens + user). Server state lives in React Query. */
export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      accessToken: null,
      refreshToken: null,
      user: null,
      setSession: (accessToken, refreshToken, user) => set({ accessToken, refreshToken, user }),
      setTokens: (accessToken, refreshToken) => set({ accessToken, refreshToken }),
      setUser: (user) => set({ user }),
      clear: () => set({ accessToken: null, refreshToken: null, user: null }),
    }),
    { name: 'ecs-auth' },
  ),
)
