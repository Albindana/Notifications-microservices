// DTOs mirroring the backend JSON shapes (see *.Application/DTOs in each service).

export interface AuthResponse {
  accessToken: string
  refreshToken: string
  expiresAt: string
}

export interface UserProfile {
  id: string
  email: string
  firstName: string
  lastName: string
  createdAt: string
}

export type NotificationStatus = 'Pending' | 'Sent' | 'Failed'
export type NotificationChannel = 'Email' | 'InApp' | 'SMS'
export type NotificationType =
  | 'Welcome'
  | 'PasswordReset'
  | 'ProfileUpdate'
  | 'OrderConfirmation'

export interface Notification {
  id: string
  userId: string
  recipientEmail: string
  type: NotificationType | string
  channel: NotificationChannel | string
  subject: string
  body: string
  status: NotificationStatus | string
  sentAt: string | null
  createdAt: string
  errorMessage: string | null
  retryCount: number
}

export interface NotificationTemplate {
  id: string
  type: string
  subject: string
  bodyTemplate: string
  createdAt: string
  updatedAt: string
}
