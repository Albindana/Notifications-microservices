import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api, errorMessage } from '../../lib/api'
import { getUserNotifications } from '../notifications/notificationsApi'
import { useAuthStore } from '../../store/auth'
import { StatusBadge } from '../../components/StatusBadge'
import { useToast } from '../../components/Toast'
import type { UserProfile } from '../../types'

const POLL_MS = 2000

export function LiveDemoPage() {
  const user = useAuthStore((s) => s.user)
  const setUser = useAuthStore((s) => s.setUser)
  const userId = user?.id
  const queryClient = useQueryClient()
  const { push } = useToast()
  const [polling, setPolling] = useState(true)

  const feed = useQuery({
    queryKey: ['notifications', 'mine', userId],
    queryFn: () => getUserNotifications(userId as string),
    enabled: !!userId,
    refetchInterval: polling ? POLL_MS : false,
  })

  // Triggers UserProfileUpdatedEvent -> RabbitMQ -> a ProfileUpdate notification.
  const triggerProfile = useMutation({
    mutationFn: async () => {
      const { data } = await api.put<UserProfile>('/users/me', {
        firstName: user?.firstName ?? 'Demo',
        lastName: user?.lastName ?? 'User',
      })
      return data
    },
    onSuccess: (data) => {
      setUser(data)
      push('Profile update published — watch the feed!', 'success')
      queryClient.invalidateQueries({ queryKey: ['notifications'] })
    },
    onError: (err) => push(errorMessage(err, 'Trigger failed'), 'error'),
  })

  const items = [...(feed.data ?? [])].sort(
    (a, b) => +new Date(b.createdAt) - +new Date(a.createdAt)
  )

  return (
    <div>
      <h1 className="mb-1 text-2xl font-bold text-slate-800">Live event flow</h1>
      <p className="mb-6 max-w-2xl text-sm text-slate-500">
        Trigger an action and watch the resulting notification arrive. The
        request goes through the <strong>API Gateway</strong> to the{' '}
        <strong>UserService</strong>, which publishes an event to{' '}
        <strong>RabbitMQ</strong>; the <strong>NotificationService</strong>{' '}
        consumes it and creates the notification you see below.
      </p>

      <div className="grid gap-6 lg:grid-cols-[320px_1fr]">
        <div className="space-y-4">
          <div className="card p-5">
            <h2 className="mb-3 text-sm font-semibold text-slate-700">
              Trigger an event
            </h2>
            <button
              className="btn-primary w-full"
              onClick={() => triggerProfile.mutate()}
              disabled={triggerProfile.isPending}
            >
              {triggerProfile.isPending
                ? 'Publishing…'
                : 'Update my profile → ProfileUpdate'}
            </button>
            <p className="mt-2 text-xs text-slate-400">
              Re-saves your profile to emit a UserProfileUpdatedEvent.
            </p>
          </div>

          <div className="card p-5">
            <div className="flex items-center justify-between">
              <span className="text-sm font-semibold text-slate-700">
                Live polling
              </span>
              <button
                onClick={() => setPolling((p) => !p)}
                className={`flex items-center gap-2 rounded-full px-3 py-1 text-xs font-medium ${
                  polling
                    ? 'bg-emerald-100 text-emerald-700'
                    : 'bg-slate-100 text-slate-500'
                }`}
              >
                <span
                  className={`h-2 w-2 rounded-full ${
                    polling ? 'animate-pulse bg-emerald-500' : 'bg-slate-400'
                  }`}
                />
                {polling ? 'Live' : 'Paused'}
              </button>
            </div>
            <p className="mt-2 text-xs text-slate-400">
              Refreshing every {POLL_MS / 1000}s · {items.length} notifications
            </p>
          </div>
        </div>

        <div className="card">
          <div className="border-b border-slate-100 px-5 py-3 text-sm font-semibold text-slate-700">
            Notification feed
          </div>
          <div className="max-h-[28rem] divide-y divide-slate-100 overflow-y-auto">
            {items.length === 0 ? (
              <div className="p-10 text-center text-sm text-slate-400">
                No notifications yet — trigger an event to see one appear.
              </div>
            ) : (
              items.map((n) => (
                <div key={n.id} className="flex items-start gap-3 px-5 py-3">
                  <div className="flex-1">
                    <div className="flex items-center gap-2">
                      <span className="font-medium text-slate-800">
                        {n.type}
                      </span>
                      <StatusBadge status={n.status} />
                    </div>
                    <p className="truncate text-sm text-slate-500">
                      {n.subject}
                    </p>
                  </div>
                  <span className="whitespace-nowrap text-xs text-slate-400">
                    {new Date(n.createdAt).toLocaleTimeString()}
                  </span>
                </div>
              ))
            )}
          </div>
        </div>
      </div>
    </div>
  )
}
