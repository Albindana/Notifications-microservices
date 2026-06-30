import { useForm } from 'react-hook-form'
import { Link, useNavigate } from 'react-router-dom'
import { AuthShell } from './AuthShell'
import { register as registerUser } from './authApi'
import { errorMessage } from '../../lib/api'
import { useToast } from '../../components/Toast'

interface FormValues {
  firstName: string
  lastName: string
  email: string
  password: string
}

export function RegisterPage() {
  const navigate = useNavigate()
  const { push } = useToast()
  const {
    register: field,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>()

  async function onSubmit(values: FormValues) {
    try {
      await registerUser(values)
      push('Account created — a welcome notification is on its way!', 'success')
      navigate('/', { replace: true })
    } catch (err) {
      push(errorMessage(err, 'Registration failed'), 'error')
    }
  }

  return (
    <AuthShell
      title="Create account"
      footer={
        <>
          Already have an account?{' '}
          <Link to="/login" className="font-medium text-brand-600">
            Sign in
          </Link>
        </>
      }
    >
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
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
            {...field('password', {
              required: 'Password is required',
              minLength: { value: 6, message: 'At least 6 characters' },
            })}
          />
          {errors.password && (
            <p className="mt-1 text-xs text-rose-600">
              {errors.password.message}
            </p>
          )}
        </div>
        <button
          type="submit"
          className="btn-primary w-full"
          disabled={isSubmitting}
        >
          {isSubmitting ? 'Creating…' : 'Create account'}
        </button>
      </form>
    </AuthShell>
  )
}
