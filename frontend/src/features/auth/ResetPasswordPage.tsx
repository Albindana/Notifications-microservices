import { useForm } from 'react-hook-form'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { AuthShell } from './AuthShell'
import { resetPassword } from './authApi'
import { errorMessage } from '../../lib/api'
import { useToast } from '../../components/Toast'

interface FormValues {
  email: string
  token: string
  newPassword: string
}

export function ResetPasswordPage() {
  const [params] = useSearchParams()
  const navigate = useNavigate()
  const { push } = useToast()
  const {
    register: field,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    defaultValues: {
      email: params.get('email') ?? '',
      token: params.get('token') ?? '',
    },
  })

  async function onSubmit(values: FormValues) {
    try {
      await resetPassword(values)
      push('Password reset — please sign in.', 'success')
      navigate('/login', { replace: true })
    } catch (err) {
      push(errorMessage(err, 'Reset failed'), 'error')
    }
  }

  return (
    <AuthShell
      title="Set a new password"
      footer={
        <Link to="/login" className="font-medium text-brand-600">
          Back to sign in
        </Link>
      }
    >
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        <div>
          <label className="label">Email</label>
          <input
            className="input"
            type="email"
            {...field('email', { required: 'Email is required' })}
          />
          {errors.email && (
            <p className="mt-1 text-xs text-rose-600">{errors.email.message}</p>
          )}
        </div>
        <div>
          <label className="label">Reset token</label>
          <input
            className="input"
            {...field('token', { required: 'Token is required' })}
          />
          {errors.token && (
            <p className="mt-1 text-xs text-rose-600">{errors.token.message}</p>
          )}
        </div>
        <div>
          <label className="label">New password</label>
          <input
            className="input"
            type="password"
            {...field('newPassword', {
              required: 'Password is required',
              minLength: { value: 6, message: 'At least 6 characters' },
            })}
          />
          {errors.newPassword && (
            <p className="mt-1 text-xs text-rose-600">
              {errors.newPassword.message}
            </p>
          )}
        </div>
        <button
          type="submit"
          className="btn-primary w-full"
          disabled={isSubmitting}
        >
          {isSubmitting ? 'Saving…' : 'Reset password'}
        </button>
      </form>
    </AuthShell>
  )
}
