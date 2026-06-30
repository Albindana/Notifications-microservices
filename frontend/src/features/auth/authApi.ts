import { api } from '../../lib/api'
import { useAuthStore } from '../../store/auth'
import type { AuthResponse, UserProfile } from '../../types'

// Logs in / registers, stores tokens, then loads the profile so the nav and
// notifications-by-user calls have the user id immediately.
async function completeAuth(auth: AuthResponse) {
  const { setTokens, setUser } = useAuthStore.getState()
  setTokens(auth)
  const { data: profile } = await api.get<UserProfile>('/users/me')
  setUser(profile)
}

export async function login(email: string, password: string) {
  const { data } = await api.post<AuthResponse>('/auth/login', { email, password })
  await completeAuth(data)
}

export async function register(input: {
  email: string
  password: string
  firstName: string
  lastName: string
}) {
  const { data } = await api.post<AuthResponse>('/auth/register', input)
  await completeAuth(data)
}

export async function forgotPassword(email: string) {
  await api.post('/users/forgot-password', { email })
}

export async function resetPassword(input: {
  email: string
  token: string
  newPassword: string
}) {
  await api.post('/users/reset-password', input)
}
