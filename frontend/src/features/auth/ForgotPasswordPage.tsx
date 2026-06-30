import { useForm } from 'react-hook-form'
import { Link } from 'react-router-dom'
import { AuthShell } from './AuthShell'
import { forgotPassword } from './authApi'
import { errorMessage } from '../../lib/api'
import { useToast } from '../../components/Toast'

interface FormValues {
  email: string
}

export function ForgotPasswordPage() {
  const { push } = useToast()
  const {
    register: field,
    handleSubmit,
    formState: { errors, isSubmitting, isSubmitSuccessful },
  } = useForm<FormValues>()

  async function onSubmit(values: FormValues) {
    try {
      await forgotPassword(values.email)
      push('If that email exists, a reset link has been sent.', 'info')
    } catch (err) {
      push(errorMessage(err), 'error')
    }
  }

  return (
    <AuthShell
      title="Reset password"
      subtitle="Enter your email and we'll send a reset link"
      footer={
        <Link to="/login" className="font-medium text-brand-600">
          Back to sign in
        </Link>
      }
    >
      {isSubmitSuccessful ? (
        <p className="text-center text-sm text-slate-600">
          If an account exists for that email, a password-reset notification has
          been queued. Check the Notifications panel (or your inbox) for the
          reset token, then use the{' '}
          <Link to="/reset-password" className="font-medium text-brand-600">
            reset page
          </Link>
          .
        </p>
      ) : (
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div>
            <label className="label">Email</label>
            <input
              className="input"
              type="email"
              {...field('email', { required: 'Email is required' })}
            />
            {errors.email && (
              <p className="mt-1 text-xs text-rose-600">
                {errors.email.message}
              </p>
            )}
          </div>
          <button
            type="submit"
            className="btn-primary w-full"
            disabled={isSubmitting}
          >
            {isSubmitting ? 'Sending…' : 'Send reset link'}
          </button>
        </form>
      )}
    </AuthShell>
  )
}
