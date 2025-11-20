import { type AnyRoute, createRoute } from '@tanstack/react-router'

import { MetadataItemDetailPage } from '../pages/Detail'

let appLayoutRouteRef: AnyRoute | null = null

export const metadataItemDetailRoute = createRoute({
  component: MetadataItemDetailPage,
  getParentRoute: () => {
    if (!appLayoutRouteRef) {
      throw new Error('metadataItemDetailRoute parent route not registered yet')
    }
    return appLayoutRouteRef
  },
  path: 'section/$contentSourceId/details/$metadataItemId',
})

export function createMetadataRoutes({
  appLayoutRoute,
}: {
  appLayoutRoute: AnyRoute
}) {
  appLayoutRouteRef = appLayoutRoute
  return [metadataItemDetailRoute]
}
