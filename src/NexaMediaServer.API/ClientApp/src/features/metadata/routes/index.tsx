import { type AnyRoute, createRoute, redirect } from '@tanstack/react-router'

import { type RouterContext } from '@/app/router/types'

import { MetadataItemDetailPage } from '../pages/Detail'
import { ContentSourceQuery, MetadataItemQuery } from '../queries'

let appLayoutRouteRef: AnyRoute | null = null

export const metadataItemDetailRoute = createRoute({
  component: MetadataItemDetailPage,
  getParentRoute: () => {
    if (!appLayoutRouteRef) {
      throw new Error('metadataItemDetailRoute parent route not registered yet')
    }
    return appLayoutRouteRef
  },
  loader: async ({
    context,
    params,
  }: {
    context: RouterContext
    params: { contentSourceId: string; metadataItemId: string }
  }) => {
    const { apolloClient } = context

    const [metadataItemResult] = await Promise.all([
      apolloClient.query({
        query: MetadataItemQuery,
        variables: { id: params.metadataItemId },
      }),
      apolloClient.query({
        query: ContentSourceQuery,
        variables: { contentSourceId: params.contentSourceId },
      }),
    ])

    if (!metadataItemResult.data?.metadataItem) {
      throw redirect({ to: '/' })
    }
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
