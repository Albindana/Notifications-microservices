import { useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  getAllNotifications,
  getUserNotifications,
  retryNotification,
} from './notificationsApi'
import { useAuthStore } from '../../store/auth'
import { StatusBadge } from '../../components/StatusBadge'
import { Spinner } from '../../components/Spinner'
import { useToast } from '../../components/Toast'
import { errorMessage } from '../../lib/api'
import type { Notification } from '../../types'

type Scope = 'mine' | 'all'
const STATUSES = ['All', 'Sent', 'Pending', 'Failed'] as const

export function NotificationsPage() {
  const userId = useAuthStore((s) => s.user?.id)
  const queryClient = useQueryClient()
  const { push } = useToast()

  const [scope, setScope] = useState<Scope>('mine')
  const [statusFilter, setStatusFilter] =
    useState<(typeof STATUSES)[number]>('All')
  const [selected, setSelected] = useState<Notification | null>(null)

  const query = useQuery({
    queryKey: ['notifications', scope, userId],
    queryFn: () =>
      scope === 'all'
        ? getAllNotifications()
        : getUserNotifications(userId as string),
    enabled: scope === 'all' || !!userId,
  })

  const retry = useMutation({
    mutationFn: retryNotification,
    onSuccess: () => {
      push('Retry queued', 'success')
      queryClient.invalidateQueries({ queryKey: ['notifications'] })
    },
    onError: (err) => push(errorMessage(err, 'Retry failed'), 'error'),
  })

  const rows = useMemo(() => {
    const list = query.data ?? []
    const sorted = [...list].sort(
      (a, b) => +new Date(b.createdAt) - +new Date(a.createdAt)
    )
    return statusFilter === 'All'
      ? sorted
      : sorted.filter((n) => n.status === statusFilter)
  }, [query.data, statusFilter])

  return (
    <div>
      <div className="mb-6 flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-2xl font-bold text-slate-800">Notifications</h1>
        <div className="flex gap-2">
          <div className="flex rounded-lg border border-slate-300 bg-white p-0.5 text-sm">
            {(['mine', 'all'] as Scope[]).map((s) => (
              <button
                key={s}
                onClick={() => setScope(s)}
                className={`rounded-md px-3 py-1 font-medium capitalize ${
                  scope === s
                    ? 'bg-brand-600 text-white'
                    : 'text-slate-600 hover:bg-slate-100'
                }`}
              >
                {s === 'mine' ? 'Mine' : 'All (admin)'}
              </button>
            ))}
          </div>
          <button
            onClick={() => query.refetch()}
            className="btn-secondary"
          >
            ↻ Refresh
          </button>
        </div>
      </div>

      <div className="mb-4 flex gap-1">
        {STATUSES.map((s) => (
          <button
            key={s}
            onClick={() => setStatusFilter(s)}
            className={`rounded-full px-3 py-1 text-xs font-medium ${
              statusFilter === s
                ? 'bg-slate-800 text-white'
                : 'bg-white text-slate-600 ring-1 ring-slate-200 hover:bg-slate-100'
            }`}
          >
            {s}
          </button>
        ))}
      </div>

      {query.isLoading ? (
        <Spinner label="Loading notifications…" />
      ) : query.isError ? (
        <div className="card p-6 text-sm text-rose-600">
          Couldn't load notifications.{' '}
          {scope === 'all' &&
            'The "All" view requires the gateway to expose GET /api/notifications — try "Mine" instead.'}
        </div>
      ) : rows.length === 0 ? (
        <div className="card p-10 text-center text-sm text-slate-500">
          No notifications yet. Try the Live Demo to generate one.
        </div>
      ) : (
        <div className="card overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-slate-50 text-left text-xs uppercase text-slate-500">
              <tr>
                <th className="px-4 py-3">Type</th>
                <th className="px-4 py-3">Recipient</th>
                <th className="px-4 py-3">Subject</th>
                <th className="px-4 py-3">Status</th>
                <th className="px-4 py-3">Created</th>
                <th className="px-4 py-3"></th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {rows.map((n) => (
                <tr key={n.id} className="hover:bg-slate-50">
                  <td className="px-4 py-3 font-medium">{n.type}</td>
                  <td className="px-4 py-3 text-slate-500">{n.recipientEmail}</td>
                  <td className="max-w-xs truncate px-4 py-3">{n.subject}</td>
                  <td className="px-4 py-3">
                    <StatusBadge status={n.status} />
                  </td>
                  <td className="px-4 py-3 text-slate-400">
                    {new Date(n.createdAt).toLocaleString()}
                  </td>
                  <td className="px-4 py-3 text-right">
                    <div className="flex justify-end gap-2">
                      <button
                        onClick={() => setSelected(n)}
                        className="text-xs font-medium text-brand-600 hover:underline"
                      >
                        View
                      </button>
                      {n.status === 'Failed' && (
                        <button
                          onClick={() => retry.mutate(n.id)}
                          disabled={retry.isPending}
                          className="text-xs font-medium text-amber-600 hover:underline"
                        >
                          Retry
                        </button>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {selected && (
        <DetailModal
          notification={selected}
          onClose={() => setSelected(null)}
          onRetry={() => {
            retry.mutate(selected.id)
            setSelected(null)
          }}
        />
      )}
    </div>
  )
}

function DetailModal({
  notification,
  onClose,
  onRetry,
}: {
  notification: Notification
  onClose: () => void
  onRetry: () => void
}) {
  return (
    <div
      className="fixed inset-0 z-40 flex items-center justify-center bg-black/40 p-4"
      onClick={onClose}
    >
      <div
        className="card w-full max-w-lg p-6"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="mb-4 flex items-start justify-between">
          <div>
            <h2 className="text-lg font-bold">{notification.subject}</h2>
            <p className="text-sm text-slate-500">
              {notification.type} · {notification.channel}
            </p>
          </div>
          <StatusBadge status={notification.status} />
        </div>
        <dl className="space-y-2 text-sm">
          <Row label="Recipient" value={notification.recipientEmail} />
          <Row
            label="Created"
            value={new Date(notification.createdAt).toLocaleString()}
          />
          <Row
            label="Sent"
            value={
              notification.sentAt
                ? new Date(notification.sentAt).toLocaleString()
                : '—'
            }
          />
          <Row label="Retries" value={String(notification.retryCount)} />
          {notification.errorMessage && (
            <Row label="Error" value={notification.errorMessage} />
          )}
        </dl>
        <div className="mt-4 rounded-lg bg-slate-50 p-3 text-sm text-slate-700">
          {notification.body}
        </div>
        <div className="mt-5 flex justify-end gap-2">
          {notification.status === 'Failed' && (
            <button onClick={onRetry} className="btn-secondary">
              Retry
            </button>
          )}
          <button onClick={onClose} className="btn-primary">
            Close
          </button>
        </div>
      </div>
    </div>
  )
}

function Row({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex justify-between gap-4">
      <dt className="text-slate-400">{label}</dt>
      <dd className="text-right font-medium text-slate-700">{value}</dd>
    </div>
  )
}
