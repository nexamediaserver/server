import { type AnyRoute, createRoute } from '@tanstack/react-router'

import { SearchPage } from '../pages/SearchPage'

let appLayoutRouteRef: AnyRoute | null = null

export interface SearchRouteSearch {
  pivot?: string
  q?: string
}

export const searchRoute = createRoute({
  component: SearchPage,
  getParentRoute: () => {
    if (!appLayoutRouteRef) {
      throw new Error('searchRoute parent route not registered yet')
    }
    return appLayoutRouteRef
  },
  path: 'search',
  validateSearch: (search: Record<string, unknown>): SearchRouteSearch => ({
    pivot: typeof search.pivot === 'string' ? search.pivot : undefined,
    q: typeof search.q === 'string' ? search.q : undefined,
  }),
})

export function createSearchRoutes({
  appLayoutRoute,
}: {
  appLayoutRoute: AnyRoute
}) {
  appLayoutRouteRef = appLayoutRoute
  return [searchRoute]
}
