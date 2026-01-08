import { type AnyRoute, createRoute, redirect } from '@tanstack/react-router'

import { type RouterContext } from '@/app/router/types'
import {
  LibrarySectionBrowseOptionsQuery,
  LibrarySectionChildrenQuery,
  LibrarySectionQuery,
  PAGE_SIZE,
} from '@/features/content-sources/queries'
import { MetadataType, SortEnumType } from '@/shared/api/graphql/graphql'

import { ContentSourceIndex } from '../pages/Index'

let appLayoutRouteRef: AnyRoute | null = null

export interface ContentSourceIndexSearch {
  view?: ContentSourceViewMode
}

export type ContentSourceViewMode = 'browse' | 'discover'

export const contentSourceIndexRoute = createRoute({
  component: ContentSourceIndex,
  getParentRoute: () => {
    if (!appLayoutRouteRef) {
      throw new Error('contentSourceIndexRoute parent route not registered yet')
    }
    return appLayoutRouteRef
  },
  validateSearch: (
    search: Record<string, unknown>,
  ): ContentSourceIndexSearch => ({
    view:
      search.view === 'browse' || search.view === 'discover'
        ? search.view
        : undefined,
  }),
  path: 'section/$contentSourceId',
  loader: async ({
    context,
    params,
  }: {
    context: RouterContext
    params: { contentSourceId: string }
  }) => {
    const { apolloClient } = context

    const [librarySectionResult] = await Promise.all([
      apolloClient.query({
        query: LibrarySectionQuery,
        variables: { contentSourceId: params.contentSourceId },
      }),
      apolloClient.query({
        query: LibrarySectionBrowseOptionsQuery,
        variables: { contentSourceId: params.contentSourceId },
      }),
      apolloClient.query({
        query: LibrarySectionChildrenQuery,
        variables: {
          contentSourceId: params.contentSourceId,
          metadataTypes: [MetadataType.Movie],
          order: [{ title: SortEnumType.Asc }],
          take: PAGE_SIZE,
        },
      }),
    ])

    if (!librarySectionResult.data?.librarySection) {
      throw redirect({ to: '/' })
    }
  },
})

export function registerContentSourcesRoutes({
  appLayoutRoute,
}: {
  appLayoutRoute: AnyRoute
}) {
  appLayoutRouteRef = appLayoutRoute
  return [contentSourceIndexRoute]
}
