import { graphql } from '@/shared/api/graphql'

export const promoteItemDocument = graphql(`
  mutation PromoteItem($itemId: ID!, $promotedUntil: DateTime) {
    promoteItem(input: { itemId: $itemId, promotedUntil: $promotedUntil }) {
      success
      error
    }
  }
`)

export const unpromoteItemDocument = graphql(`
  mutation UnpromoteItem($itemId: ID!) {
    unpromoteItem(input: { itemId: $itemId }) {
      success
      error
    }
  }
`)
