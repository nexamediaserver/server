import { graphql } from '@/shared/api/graphql'

export const searchQueryDocument = graphql(`
  query Search($query: String!, $pivot: SearchPivot, $limit: Int) {
    search(query: $query, pivot: $pivot, limit: $limit) {
      id
      title
      metadataType
      score
      year
      thumbUri
      librarySectionId
    }
  }
`)
