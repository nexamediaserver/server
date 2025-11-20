import { graphql } from '@/shared/api/graphql'

export const librarySectionsQueryDocument = graphql(`
  query LibrarySectionsList(
    $first: Int
    $after: String
    $last: Int
    $before: String
  ) {
    librarySections(
      first: $first
      after: $after
      last: $last
      before: $before
    ) {
      nodes {
        id
        name
        type
      }
      pageInfo {
        hasNextPage
        hasPreviousPage
        startCursor
        endCursor
      }
    }
  }
`)
