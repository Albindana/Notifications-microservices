import axios, {
  AxiosError,
  type AxiosRequestConfig,
  type InternalAxiosRequestConfig,
} from 'axios'
import { useAuthStore } from '../store/auth'
import type { AuthResponse } from '../types'

// Same-origin: Vite proxy (dev) / nginx (docker) forwards /api -> gateway.
export const api = axios.create({ baseURL: '/api' })

// A bare client without interceptors, used for the refresh call itself so a
// 401 on /auth/refresh does not recurse back into the refresh logic.
const bare = axios.create({ baseURL: '/api' })

// Attach the bearer token to every outgoing request.
api.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = useAuthStore.getState().accessToken
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

// Single in-flight refresh shared by all queued 401s.
let refreshing: Promise<string | null> | null = null

async function refreshAccessToken(): Promise<string | null> {
  const { refreshToken, setTokens, logout } = useAuthStore.getState()
  if (!refreshToken) {
    logout()
    return null
  }
  try {
    const { data } = await bare.post<AuthResponse>('/auth/refresh', {
      refreshToken,
    })
    setTokens(data)
    return data.accessToken
  } catch {
    logout()
    return null
  }
}

api.interceptors.response.use(
  (res) => res,
  async (error: AxiosError) => {
    const original = error.config as
      | (AxiosRequestConfig & { _retry?: boolean })
      | undefined

    // Only attempt a refresh on a genuine 401 that we have not already retried.
    if (error.response?.status === 401 && original && !original._retry) {
      original._retry = true
      refreshing = refreshing ?? refreshAccessToken()
      const newToken = await refreshing
      refreshing = null

      if (newToken) {
        original.headers = original.headers ?? {}
        ;(original.headers as Record<string, string>).Authorization =
          `Bearer ${newToken}`
        return api(original)
      }
      // Refresh failed -> bounce to login.
      if (typeof window !== 'undefined') {
        window.location.assign('/login')
      }
    }
    return Promise.reject(error)
  }
)

// Helper to surface backend error messages in a consistent way.
export function errorMessage(err: unknown, fallback = 'Something went wrong'): string {
  if (axios.isAxiosError(err)) {
    const data = err.response?.data as
      | { message?: string; title?: string; errors?: Record<string, string[]> }
      | undefined
    if (data?.message) return data.message
    if (data?.title) return data.title
    if (data?.errors) {
      const first = Object.values(data.errors)[0]
      if (first?.[0]) return first[0]
    }
    if (err.response?.status === 401) return 'Invalid credentials'
  }
  return fallback
}
