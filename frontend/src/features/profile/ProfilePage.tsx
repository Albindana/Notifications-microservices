import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { useQueryClient } from '@tanstack/react-query'
import { api, errorMessage } from '../../lib/api'
import { useAuthStore } from '../../store/auth'
import { useToast } from '../../components/Toast'
import type { UserProfile } from '../../types'

interface FormValues {
  firstName: string
  lastName: string
}

export function ProfilePage() {
  const user = useAuthStore((s) => s.user)
  const setUser = useAuthStore((s) => s.setUser)
  const { push } = useToast()
  const queryClient = useQueryClient()
  const {
    register: field,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting, isDirty },
  } = useForm<FormValues>({
    defaultValues: { firstName: user?.firstName, lastName: user?.lastName },
  })

  useEffect(() => {
    if (user) reset({ firstName: user.firstName, lastName: user.lastName })
  }, [user, reset])

  async function onSubmit(values: FormValues) {
    try {
      const { data } = await api.put<UserProfile>('/users/me', values)
      setUser(data)
      reset({ firstName: data.firstName, lastName: data.lastName })
      push('Profile updated — watch for a ProfileUpdate notification!', 'success')
      // The update publishes an event that produces a notification.
      queryClient.invalidateQueries({ queryKey: ['notifications'] })
    } catch (err) {
      push(errorMessage(err, 'Update failed'), 'error')
    }
  }

  return (
    <div className="max-w-xl">
      <h1 className="mb-1 text-2xl font-bold text-slate-800">Profile</h1>
      <p className="mb-6 text-sm text-slate-500">
        Saving publishes a <code>UserProfileUpdatedEvent</code> to RabbitMQ,
        which the NotificationService turns into a notification.
      </p>

      <div className="card p-6">
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div>
            <label className="label">Email</label>
            <input className="input bg-slate-50" value={user?.email ?? ''} disabled />
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="label">First name</label>
              <input
                className="input"
                {...field('firstName', { required: 'Required' })}
              />
              {errors.firstName && (
                <p className="mt-1 text-xs text-rose-600">
                  {errors.firstName.message}
                </p>
              )}
            </div>
            <div>
              <label className="label">Last name</label>
              <input
                className="input"
                {...field('lastName', { required: 'Required' })}
              />
              {errors.lastName && (
                <p className="mt-1 text-xs text-rose-600">
                  {errors.lastName.message}
                </p>
              )}
            </div>
          </div>
          <div className="flex items-center gap-3 pt-2">
            <button
              type="submit"
              className="btn-primary"
              disabled={isSubmitting || !isDirty}
            >
              {isSubmitting ? 'Saving…' : 'Save changes'}
            </button>
            {user && (
              <span className="text-xs text-slate-400">
                Member since {new Date(user.createdAt).toLocaleDateString()}
              </span>
            )}
          </div>
        </form>
      </div>
    </div>
  )
}
