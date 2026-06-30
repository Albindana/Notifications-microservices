import { create } from 'zustand'
import { persist } from 'zustand/middleware'
import type { AuthResponse, UserProfile } from '../types'

interface AuthState {
  accessToken: string | null
  refreshToken: string | null
  expiresAt: string | null
  user: UserProfile | null
  setTokens: (auth: AuthResponse) => void
  setUser: (user: UserProfile | null) => void
  logout: () => void
  isAuthenticated: () => boolean
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
      accessToken: null,
      refreshToken: null,
      expiresAt: null,
      user: null,
      setTokens: (auth) =>
        set({
          accessToken: auth.accessToken,
          refreshToken: auth.refreshToken,
          expiresAt: auth.expiresAt,
        }),
      setUser: (user) => set({ user }),
      logout: () =>
        set({
          accessToken: null,
          refreshToken: null,
          expiresAt: null,
          user: null,
        }),
      isAuthenticated: () => !!get().accessToken,
    }),
    { name: 'notif-auth' }
  )
)
