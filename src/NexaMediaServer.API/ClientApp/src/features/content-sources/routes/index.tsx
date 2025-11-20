import { type AnyRoute, createRoute } from '@tanstack/react-router'

import { ContentSourceIndex } from '../pages/Index'

let appLayoutRouteRef: AnyRoute | null = null

export const contentSourceIndexRoute = createRoute({
  component: ContentSourceIndex,
  getParentRoute: () => {
    if (!appLayoutRouteRef) {
      throw new Error('contentSourceIndexRoute parent route not registered yet')
    }
    return appLayoutRouteRef
  },
  path: 'section/$contentSourceId',
})

export function registerContentSourcesRoutes({
  appLayoutRoute,
}: {
  appLayoutRoute: AnyRoute
}) {
  appLayoutRouteRef = appLayoutRoute
  return [contentSourceIndexRoute]
}
