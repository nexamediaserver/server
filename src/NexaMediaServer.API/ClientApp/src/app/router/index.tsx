import { useApolloClient } from '@apollo/client/react'
import {
  createRootRoute,
  createRoute,
  createRouter,
  Outlet,
  redirect,
  type RouterProps,
  RouterProvider,
} from '@tanstack/react-router'

import { createAuthRoutes } from '@/features/auth/routes'
import { getStoredAccessToken } from '@/features/auth/utils/token'
import { registerContentSourcesRoutes } from '@/features/content-sources/routes'
import { HomePage } from '@/features/hubs/pages/HomePage'
import { createMetadataRoutes } from '@/features/metadata/routes'
import { createSearchRoutes } from '@/features/search'
import { Toaster } from '@/shared/components/ui/sonner'

import type { RouterContext } from './types'

import { AppLayout } from '../layout'
import { APP_SHELL_SCROLL_REGION_ID } from '../layout/constants'
import { ErrorHandler } from '../layout/ErrorHandler'

const rootRoute = createRootRoute({
  component: () => (
    <>
      <ErrorHandler>
        <Outlet />
      </ErrorHandler>
      <Toaster position="bottom-right" />
    </>
  ),
})

const authenticatedRoute = createRoute({
  beforeLoad: ({ location }) => {
    const token = getStoredAccessToken()
    const isLogin = location.pathname === '/login'

    // Support cookie-based auth: presence of a stored user object (jotai atomWithStorage)
    // indicates a previously validated session.
    let hasStoredUser = false
    try {
      const raw = localStorage.getItem('auth:user')
      if (raw && raw !== 'null') {
        const parsed = JSON.parse(raw) as null | {
          id?: string
          username?: string
        }
        hasStoredUser = Boolean(parsed && (parsed.id ?? parsed.username))
      }
    } catch {
      hasStoredUser = false
    }

    // If we have either a bearer token, a stored user (cookie-auth), or we're on login, allow through.
    if (token || hasStoredUser || isLogin) return

    const nextTarget = location.href
    throw redirect({
      replace: true,
      search: { next: nextTarget || '/' },
      to: '/login',
    })
  },
  getParentRoute: () => rootRoute,
  id: 'authenticated',
})

const authFeatureRoutes = createAuthRoutes({ rootRoute })

// Layout route (pathless) wrapping authenticated application routes.
const appLayoutRoute = createRoute({
  component: AppLayout,
  getParentRoute: () => authenticatedRoute,
  id: 'app-layout',
})

const indexRoute = createRoute({
  component: HomePage,
  getParentRoute: () => appLayoutRoute,
  path: '/',
})

const appFeatureRoutes = [
  ...registerContentSourcesRoutes({ appLayoutRoute }),
  ...createMetadataRoutes({ appLayoutRoute }),
  ...createSearchRoutes({ appLayoutRoute }),
]

const routeTree = rootRoute.addChildren([
  ...authFeatureRoutes,
  authenticatedRoute.addChildren([
    appLayoutRoute.addChildren([indexRoute, ...appFeatureRoutes]),
  ]),
])

const router = createRouter({
  basepath: '/web/',
  context: {
    apolloClient: null as unknown as RouterContext['apolloClient'],
  } satisfies RouterContext,
  defaultPreload: 'intent',
  defaultStructuralSharing: true,
  routeTree,
  scrollRestoration: true,
  scrollToTopSelectors: [
    `[data-scroll-restoration-id="${APP_SHELL_SCROLL_REGION_ID}"]`,
  ],
})

export function Provider(props: Readonly<Omit<RouterProps, 'router'>>) {
  const apolloClient = useApolloClient()

  return (
    <RouterProvider context={{ apolloClient }} router={router} {...props} />
  )
}

declare module '@tanstack/react-router' {
  interface Register {
    router: typeof router
  }
}
