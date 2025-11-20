import { graphql } from '@/shared/api/graphql'

export const refreshLibraryMetadataDocument = graphql(`
  mutation RefreshLibraryMetadata($librarySectionId: ID!) {
    refreshLibraryMetadata(input: { librarySectionId: $librarySectionId }) {
      success
      error
    }
  }
`)

export const refreshItemMetadataDocument = graphql(`
  mutation RefreshItemMetadata(
    $itemId: ID!
    $includeChildren: Boolean! = true
  ) {
    refreshItemMetadata(
      input: { itemId: $itemId, includeChildren: $includeChildren }
    ) {
      success
      error
    }
  }
`)
