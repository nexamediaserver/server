/**
 * Metadata Item MSW Handlers
 *
 * Handlers for metadata/media item GraphQL operations.
 * These provide default mock responses for item queries.
 *
 * INCLUDED OPERATIONS:
 * - (Add as needed: ItemDetail, SearchItems, etc.)
 *
 * MOCK DATA PATTERNS:
 * - Use realistic but minimal data
 * - Include __typename for Apollo cache
 * - Use predictable IDs for assertions
 */

// import { graphql, HttpResponse } from 'msw'

/**
 * Metadata handlers placeholder.
 *
 * Add handlers here as tests require them:
 *
 * const itemDetailHandler = graphql.query('ItemDetail', ({ variables }) => {
 *   return HttpResponse.json({
 *     data: {
 *       node: {
 *         __typename: 'MetadataItem',
 *         id: variables.id,
 *         title: 'Test Movie',
 *         metadataType: 'MOVIE',
 *         // ... other fields
 *       }
 *     }
 *   })
 * })
 */

/**
 * Export all metadata handlers as an array.
 * Currently empty - add handlers as tests require them.
 */
export const metadataHandlers: [] = []
