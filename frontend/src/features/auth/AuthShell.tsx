import type { ReactNode } from 'react'

// Centered card used by every auth page.
export function AuthShell({
  title,
  subtitle,
  children,
  footer,
}: {
  title: string
  subtitle?: string
  children: ReactNode
  footer?: ReactNode
}) {
  return (
    <div className="flex min-h-screen items-center justify-center px-4">
      <div className="w-full max-w-md">
        <div className="mb-6 text-center">
          <div className="text-3xl">🔔</div>
          <h1 className="mt-2 text-2xl font-bold text-slate-800">{title}</h1>
          {subtitle && <p className="mt-1 text-sm text-slate-500">{subtitle}</p>}
        </div>
        <div className="card p-6">{children}</div>
        {footer && (
          <div className="mt-4 text-center text-sm text-slate-500">{footer}</div>
        )}
      </div>
    </div>
  )
}
