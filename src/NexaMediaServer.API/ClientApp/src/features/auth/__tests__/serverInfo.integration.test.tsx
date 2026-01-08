/**
 * ServerInfo Integration Test
 *
 * This file demonstrates best practices for integration testing with:
 * - MSW (Mock Service Worker) for GraphQL mocking
 * - Custom render with providers
 * - Testing async data loading
 * - Testing different server states
 *
 * INTEGRATION TESTS VS UNIT TESTS:
 *
 * Integration tests:
 * - Test components with their dependencies (providers, API calls)
 * - Use MSW to mock network requests
 * - Test realistic user workflows
 * - Slower but more confidence
 *
 * Unit tests:
 * - Test components in isolation
 * - Mock dependencies directly
 * - Test specific logic
 * - Fast and focused
 *
 * KEY PATTERNS DEMONSTRATED:
 * 1. Using MSW to mock GraphQL queries
 * 2. Overriding default handlers for specific test cases
 * 3. Testing loading states
 * 4. Testing error states
 * 5. Waiting for async operations
 */

import { useQuery } from '@apollo/client/react'
import { graphql, HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'

import { graphql as gql } from '@/shared/api/graphql'
import { render, screen, waitFor } from '@/test-utils'
import { server } from '@/test-utils/mocks/server'

/**
 * GraphQL query for server info.
 * This is the same query used in the Login page.
 */
const serverInfoQuery = gql(`
  query ServerInfo {
    serverInfo {
      versionString
      isDevelopment
    }
  }
`)

/**
 * Test component that displays server info.
 *
 * In real tests, you'd test the actual Login page component.
 * This simplified component demonstrates the testing patterns.
 */
function ServerInfoDisplay() {
  const { data, error, loading } = useQuery(serverInfoQuery)

  if (loading) {
    return <div data-testid="loading">Loading...</div>
  }

  if (error) {
    return <div data-testid="error">Error: {error.message}</div>
  }

  return (
    <div data-testid="server-info">
      <span data-testid="version">{data?.serverInfo.versionString}</span>
      {data?.serverInfo.isDevelopment && (
        <span data-testid="dev-badge">Development</span>
      )}
    </div>
  )
}

/**
 * Integration tests for ServerInfo query.
 *
 * These tests demonstrate how to:
 * - Use the default MSW handlers
 * - Override handlers for specific scenarios
 * - Test loading, success, and error states
 */
describe('ServerInfo Integration', () => {
  /**
   * Test using default MSW handler.
   *
   * The default handler is defined in test-utils/mocks/handlers/auth.ts
   * and returns { versionString: '1.0.0-test', isDevelopment: false }
   */
  it('displays server version from default handler', async () => {
    // ARRANGE: Render component with all providers
    // The custom render function wraps the component with Apollo, Router, etc.
    render(<ServerInfoDisplay />)

    // ACT & ASSERT: Wait for the query to resolve
    // waitFor retries until the assertion passes or times out
    await waitFor(() => {
      expect(screen.getByTestId('version')).toHaveTextContent('1.0.0-test')
    })

    // The default handler returns isDevelopment: false
    // So the dev badge should NOT be present
    expect(screen.queryByTestId('dev-badge')).not.toBeInTheDocument()
  })

  /**
   * Test with custom MSW handler override.
   *
   * Use server.use() to override the default handler for a specific test.
   * The override is automatically reset after the test (in setup.ts).
   */
  it('displays development badge when server is in dev mode', async () => {
    // ARRANGE: Override the default handler for this test
    server.use(
      graphql.query('ServerInfo', () => {
        return HttpResponse.json({
          data: {
            serverInfo: {
              __typename: 'ServerInfo',
              isDevelopment: true, // Override to true
              versionString: '2.0.0-dev',
            },
          },
        })
      }),
    )

    render(<ServerInfoDisplay />)

    // ASSERT: Wait for custom data to appear
    await waitFor(() => {
      expect(screen.getByTestId('version')).toHaveTextContent('2.0.0-dev')
    })

    // Dev badge should now be visible
    expect(screen.getByTestId('dev-badge')).toBeInTheDocument()
  })

  /**
   * Test loading state.
   *
   * Use MSW's delay to simulate slow network and capture loading state.
   */
  it('shows loading state while query is in flight', async () => {
    // ARRANGE: Add a delay to the response
    server.use(
      graphql.query('ServerInfo', async () => {
        // Delay response to ensure we see loading state
        await new Promise((resolve) => setTimeout(resolve, 100))
        return HttpResponse.json({
          data: {
            serverInfo: {
              __typename: 'ServerInfo',
              isDevelopment: false,
              versionString: '1.0.0',
            },
          },
        })
      }),
    )

    render(<ServerInfoDisplay />)

    // ASSERT: Loading state should appear immediately
    expect(screen.getByTestId('loading')).toBeInTheDocument()

    // Eventually, loading should disappear and data should appear
    await waitFor(() => {
      expect(screen.queryByTestId('loading')).not.toBeInTheDocument()
      expect(screen.getByTestId('server-info')).toBeInTheDocument()
    })
  })

  /**
   * Test error handling.
   *
   * Use MSW to return a GraphQL error response.
   */
  it('displays error message when query fails', async () => {
    // ARRANGE: Return a GraphQL error
    server.use(
      graphql.query('ServerInfo', () => {
        return HttpResponse.json({
          data: null,
          errors: [
            {
              message: 'Unable to reach server',
            },
          ],
        })
      }),
    )

    render(<ServerInfoDisplay />)

    // ASSERT: Error should be displayed
    await waitFor(() => {
      expect(screen.getByTestId('error')).toHaveTextContent(
        'Error: Unable to reach server',
      )
    })
  })

  /**
   * Test network error (not GraphQL error).
   *
   * Simulates complete network failure.
   */
  it('handles network errors gracefully', async () => {
    // ARRANGE: Simulate network failure
    server.use(
      graphql.query('ServerInfo', () => {
        return HttpResponse.error()
      }),
    )

    render(<ServerInfoDisplay />)

    // ASSERT: Error should be displayed
    // The exact message may vary, but error state should be shown
    await waitFor(() => {
      expect(screen.getByTestId('error')).toBeInTheDocument()
    })
  })
})
