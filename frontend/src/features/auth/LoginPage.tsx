import { useForm } from 'react-hook-form'
import { Link, useNavigate } from 'react-router-dom'
import { AuthShell } from './AuthShell'
import { login } from './authApi'
import { errorMessage } from '../../lib/api'
import { useToast } from '../../components/Toast'

interface FormValues {
  email: string
  password: string
}

export function LoginPage() {
  const navigate = useNavigate()
  const { push } = useToast()
  const {
    register: field,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    defaultValues: { email: 'admin@notify.com', password: 'Admin123!' },
  })

  async function onSubmit(values: FormValues) {
    try {
      await login(values.email, values.password)
      push('Welcome back!', 'success')
      navigate('/', { replace: true })
    } catch (err) {
      push(errorMessage(err, 'Login failed'), 'error')
    }
  }

  return (
    <AuthShell
      title="Sign in"
      subtitle="Seeded admin: admin@notify.com / Admin123!"
      footer={
        <>
          No account?{' '}
          <Link to="/register" className="font-medium text-brand-600">
            Create one
          </Link>
        </>
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
          <label className="label">Password</label>
          <input
            className="input"
            type="password"
            {...field('password', { required: 'Password is required' })}
          />
          {errors.password && (
            <p className="mt-1 text-xs text-rose-600">
              {errors.password.message}
            </p>
          )}
        </div>
        <div className="text-right">
          <Link
            to="/forgot-password"
            className="text-xs font-medium text-brand-600"
          >
            Forgot password?
          </Link>
        </div>
        <button
          type="submit"
          className="btn-primary w-full"
          disabled={isSubmitting}
        >
          {isSubmitting ? 'Signing in…' : 'Sign in'}
        </button>
      </form>
    </AuthShell>
  )
}
