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

export const LibrarySectionBrowseOptionsQuery = graphql(`
  query LibrarySectionBrowseOptions($contentSourceId: ID!) {
    librarySection(id: $contentSourceId) {
      id
      availableRootItemTypes {
        displayName
        metadataTypes
      }
      availableSortFields {
        key
        displayName
        requiresUserData
      }
    }
  }
`)

export const LibrarySectionChildrenQuery = graphql(`
  query LibrarySectionChildren(
    $contentSourceId: ID!
    $metadataTypes: [MetadataType!]!
    $skip: Int
    $take: Int
    $order: [ItemSortInput!]
  ) {
    librarySection(id: $contentSourceId) {
      id
      children(
        metadataTypes: $metadataTypes
        skip: $skip
        take: $take
        order: $order
      ) {
        items {
          id
          isPromoted
          librarySectionId
          title
          year
          thumbUri
          metadataType
          length
          viewCount
          viewOffset
          primaryPerson {
            id
            title
            metadataType
          }
          persons {
            id
            title
            metadataType
          }
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
    $metadataTypes: [MetadataType!]!
  ) {
    librarySection(id: $contentSourceId) {
      id
      letterIndex(metadataTypes: $metadataTypes) {
        letter
        count
        firstItemOffset
      }
    }
  }
`)
