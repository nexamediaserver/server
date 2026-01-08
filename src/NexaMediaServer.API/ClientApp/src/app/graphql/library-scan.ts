import { graphql } from '@/shared/api/graphql'

export const startLibraryScanDocument = graphql(`
  mutation StartLibraryScan($librarySectionId: ID!) {
    startLibraryScan(input: { librarySectionId: $librarySectionId }) {
      success
      error
      scanId
    }
  }
`)
