import { graphql } from '@/shared/api/graphql'

export const analyzeItemDocument = graphql(`
  mutation AnalyzeItem($itemId: ID!) {
    analyzeItem(input: { itemId: $itemId }) {
      success
      error
    }
  }
`)
