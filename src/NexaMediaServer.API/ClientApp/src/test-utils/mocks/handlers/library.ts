/**
 * Library Section MSW Handlers
 *
 * Handlers for library/content source GraphQL operations.
 * These provide default mock responses for library queries.
 *
 * INCLUDED OPERATIONS:
 * - LibrarySectionsList query - Returns paginated library sections
 *
 * CUSTOMIZING RESPONSES:
 * Override in individual tests for specific scenarios:
 *
 * ```tsx
 * server.use(
 *   graphql.query('LibrarySectionsList', () => {
 *     return HttpResponse.json({
 *       data: {
 *         librarySections: {
 *           nodes: [], // Test empty state
 *           pageInfo: { hasNextPage: false, ... }
 *         }
 *       }
 *     })
 *   })
 * )
 * ```
 */

import { graphql, HttpResponse } from 'msw'

/**
 * Default LibrarySectionsList query handler.
 *
 * Returns a mock list of library sections with different types.
 * Used by the sidebar, home page, and library management screens.
 */
const librarySectionsListHandler = graphql.query('LibrarySectionsList', () => {
  return HttpResponse.json({
    data: {
      librarySections: {
        __typename: 'LibrarySectionsConnection',
        nodes: [
          {
            __typename: 'LibrarySection',
            id: 'lib-1',
            name: 'Movies',
            type: 'MOVIES',
          },
          {
            __typename: 'LibrarySection',
            id: 'lib-2',
            name: 'TV Shows',
            type: 'TV_SHOWS',
          },
          {
            __typename: 'LibrarySection',
            id: 'lib-3',
            name: 'Music',
            type: 'MUSIC',
          },
        ],
        pageInfo: {
          __typename: 'PageInfo',
          endCursor: 'cursor-3',
          hasNextPage: false,
          hasPreviousPage: false,
          startCursor: 'cursor-1',
        },
      },
    },
  })
})

/**
 * Export all library handlers as an array.
 */
export const libraryHandlers = [librarySectionsListHandler]
