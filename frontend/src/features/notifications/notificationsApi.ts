import { api } from '../../lib/api'
import type { Notification } from '../../types'

// Admin-style "all" list. The gateway routes /api/notifications/{everything};
// a bare /api/notifications may not match, so callers should fall back to the
// per-user list if this rejects.
export async function getAllNotifications(): Promise<Notification[]> {
  const { data } = await api.get<Notification[]>('/notifications')
  return data
}

export async function getUserNotifications(
  userId: string
): Promise<Notification[]> {
  const { data } = await api.get<Notification[]>(`/notifications/user/${userId}`)
  return data
}

export async function retryNotification(id: string): Promise<Notification> {
  const { data } = await api.put<Notification>(`/notifications/${id}/retry`)
  return data
}
