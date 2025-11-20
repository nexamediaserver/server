import { graphql } from '@/shared/api/graphql'

export const removeLibrarySectionDocument = graphql(`
  mutation RemoveLibrarySection($librarySectionId: ID!) {
    removeLibrarySection(input: { librarySectionId: $librarySectionId }) {
      success
      error
    }
  }
`)
