/**
 * Apollo Client Mocks
 *
 * This module provides utilities for creating mock Apollo clients
 * for use in tests. Use these when you need to:
 *
 * - Test components that use useQuery/useMutation directly
 * - Verify cache behavior
 * - Test optimistic updates
 * - Test error states from Apollo
 *
 * WHEN TO USE MOCK APOLLO VS MSW:
 *
 * Use MSW (via server.use()) when:
 * - Testing real network request/response flow
 * - Testing loading states
 * - Testing error responses from the server
 * - Integration tests that should behave like production
 *
 * Use Mock Apollo Client when:
 * - Unit testing a hook that uses Apollo
 * - Testing cache-specific behavior
 * - Testing optimistic UI updates
 * - You need fine-grained control over Apollo internals
 *
 * EXAMPLE - Testing with pre-populated cache:
 * ```tsx
 * import { createMockClient, createMockCache } from '@/test-utils/mocks/apollo'
 *
 * const cache = createMockCache()
 * cache.writeQuery({
 *   query: MY_QUERY,
 *   data: { myData: 'test' }
 * })
 *
 * const client = createMockClient({ cache })
 * render(<MyComponent />, { apolloClient: client })
 * ```
 */

import { ApolloClient, ApolloLink, InMemoryCache } from '@apollo/client'
import { Observable } from '@apollo/client/utilities'

/**
 * Options for creating a mock Apollo client.
 */
interface MockClientOptions {
  /**
   * Pre-configured cache. If not provided, creates a new one.
   */
  cache?: InMemoryCache

  /**
   * Mock responses for specific operations.
   * Keys are operation names, values are the data to return.
   *
   * Example:
   * ```ts
   * {
   *   ServerInfo: { serverInfo: { versionString: '1.0.0' } }
   * }
   * ```
   */
  mocks?: Record<string, unknown>
}

/**
 * Creates an InMemoryCache with the same type policies as the real app.
 *
 * This ensures cache behavior in tests matches production.
 * Add new type policies here when they're added to the real ApolloProvider.
 */
export function createMockCache(): InMemoryCache {
  return new InMemoryCache({
    typePolicies: {
      // Match the cache policies from the real ApolloProvider
      Query: {
        fields: {
          // Node connections use cursor-based pagination
          librarySections: {
            keyArgs: false,
          },
          // Add other field policies as needed
        },
      },
    },
  })
}

/**
 * Creates a mock Apollo client for testing.
 *
 * This client uses a mock link that returns data from the mocks option
 * or undefined if no mock is provided. For most tests, prefer using
 * MSW to mock the network layer instead.
 *
 * @param options - Configuration options
 * @returns A mock ApolloClient instance
 *
 * EXAMPLE:
 * ```tsx
 * const client = createMockClient({
 *   mocks: {
 *     ServerInfo: {
 *       serverInfo: { versionString: '2.0.0', isDevelopment: true }
 *     }
 *   }
 * })
 *
 * render(<MyComponent />, { apolloClient: client })
 * ```
 */
export function createMockClient(
  options: MockClientOptions = {},
): ApolloClient<unknown> {
  const { cache = createMockCache(), mocks = {} } = options

  // Create a mock link that returns data based on operation name
  const mockLink = new ApolloLink((operation) => {
    return new Observable((observer) => {
      const operationName = operation.operationName
      const mockData: unknown = mocks[operationName]

      // Simulate async behavior like a real network request
      setTimeout(() => {
        if (mockData !== undefined) {
          observer.next({ data: mockData })
        } else {
          // Return empty data if no mock is provided
          // This prevents errors but won't have real data
          observer.next({ data: null })
        }
        observer.complete()
      }, 0)
    })
  })

  return new ApolloClient({
    cache,
    defaultOptions: {
      // Disable cache-first for tests to ensure we always hit the mock
      query: {
        fetchPolicy: 'no-cache',
      },
      watchQuery: {
        fetchPolicy: 'no-cache',
      },
    },
    link: mockLink,
  })
}
