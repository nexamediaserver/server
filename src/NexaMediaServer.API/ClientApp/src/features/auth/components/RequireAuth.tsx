import { useLocation, useRouter } from '@tanstack/react-router'
import { useEffect, useRef } from 'react'

import { useAuth } from '@/features/auth'

export function RequireAuth({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, status } = useAuth()
  const router = useRouter()
  const location = useLocation()
  const hasRedirectedRef = useRef(false)

  useEffect(() => {
    if (status === 'loading' || isAuthenticated) return

    const isLoginRoute = location.pathname === '/login'
    if (isLoginRoute) return
    if (hasRedirectedRef.current) return
    hasRedirectedRef.current = true
    const nextPath =
      location.pathname + (location.searchStr ? location.searchStr : '')
    void router.navigate({
      replace: true,
      search: { next: nextPath === '/login' ? '/' : nextPath || '/' },
      to: '/login',
    })
  }, [status, isAuthenticated, router, location])

  if (!isAuthenticated) {
    return (
      <p aria-live="polite" className="p-4" role="status">
        {status === 'loading' ? 'Checking session…' : 'Redirecting to sign in…'}
      </p>
    )
  }

  return <>{children}</>
}
