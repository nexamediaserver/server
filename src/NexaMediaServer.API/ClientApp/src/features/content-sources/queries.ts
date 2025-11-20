import { graphql } from '@/shared/api/graphql'

export const PAGE_SIZE = 100

export const LibrarySectionQuery = graphql(`
  query LibrarySection($contentSourceId: ID!) {
    librarySection(id: $contentSourceId) {
      id
      name
      type
    }
  }
`)

export const LibrarySectionChildrenQuery = graphql(`
  query LibrarySectionChildren(
    $contentSourceId: ID!
    $metadataType: MetadataType!
    $skip: Int
    $take: Int
  ) {
    librarySection(id: $contentSourceId) {
      id
      children(
        metadataType: $metadataType
        skip: $skip
        take: $take
        order: { title: ASC }
      ) {
        items {
          id
          title
          year
          thumbUri
          metadataType
          length
          viewOffset
        }
        pageInfo {
          hasNextPage
          hasPreviousPage
        }
        totalCount
      }
    }
  }
`)

export const LibrarySectionLetterIndexQuery = graphql(`
  query LibrarySectionLetterIndex(
    $contentSourceId: ID!
    $metadataType: MetadataType!
  ) {
    librarySection(id: $contentSourceId) {
      id
      letterIndex(metadataType: $metadataType) {
        letter
        count
        firstItemOffset
      }
    }
  }
`)
