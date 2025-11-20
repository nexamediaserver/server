import { type AnyRoute, createRoute, redirect } from '@tanstack/react-router'

import Login from '../pages/Login'
import { getStoredAccessToken } from '../utils/token'

export function createAuthRoutes({ rootRoute }: { rootRoute: AnyRoute }) {
  const loginRoute = createRoute({
    validateSearch: (search: Record<string, unknown>) => {
      let next: string | undefined
      if (typeof search.next === 'string' && search.next.startsWith('/')) {
        next = search.next
      }
      return { next }
    },
    component: Login,
    getParentRoute: () => rootRoute,
    path: 'login',
    beforeLoad: ({ search }) => {
      const token = getStoredAccessToken()
      const isLogin = location.pathname === '/login'

      if (!(token || isLogin)) return

      throw redirect({
        replace: true,
        to: (search as { next?: string }).next ?? '/',
      })
    },
  })

  return [loginRoute]
}
