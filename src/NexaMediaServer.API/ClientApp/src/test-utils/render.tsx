/**
 * Custom Render Function for Testing
 *
 * This module provides a custom render function that wraps components
 * with all the providers needed in the application. Use this instead
 * of the raw @testing-library/react render for integration tests.
 *
 * USAGE:
 * ```tsx
 * import { render, screen } from '@/test-utils'
 *
 * test('renders component', async () => {
 *   render(<MyComponent />)
 *   expect(screen.getByText('Hello')).toBeInTheDocument()
 * })
 * ```
 *
 * WHY CUSTOM RENDER?
 * - Components often depend on context providers (Apollo, Auth, Router, etc.)
 * - Without providers, tests fail with "Cannot read property of undefined"
 * - This wrapper ensures components have access to everything they need
 */

/* eslint-disable react-refresh/only-export-components -- Test utilities mix components with non-component exports */

import type { ReactElement, ReactNode } from 'react'

import { ApolloClient, HttpLink } from '@apollo/client'
import { ApolloProvider } from '@apollo/client/react'
import { render, type RenderOptions } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Provider as JotaiProvider } from 'jotai'

import { createMockCache } from './mocks/apollo'

/**
 * Options for customizing the test render.
 *
 * @property apolloClient - Custom Apollo client (uses mock by default)
 * @property renderOptions - Standard @testing-library render options
 */
interface CustomRenderOptions {
  apolloClient?: ApolloClient
  renderOptions?: Omit<RenderOptions, 'wrapper'>
}

/**
 * Creates a test Apollo client that uses MSW for mocking.
 *
 * This client makes real fetch requests which are intercepted by MSW,
 * providing a more realistic testing environment.
 */
function createTestApolloClient(): ApolloClient {
  return new ApolloClient({
    cache: createMockCache(),
    defaultOptions: {
      // Use network-only to ensure we always hit MSW handlers
      query: {
        fetchPolicy: 'network-only',
      },
      watchQuery: {
        fetchPolicy: 'network-only',
      },
    },
    // Use HttpLink which will be intercepted by MSW
    link: new HttpLink({
      uri: '/graphql',
    }),
  })
}

// Module-level client to reuse across tests in the same file
let testClient: ApolloClient | null = null

/**
 * Custom render function that wraps components with all app providers.
 *
 * This is the main export for integration tests. It provides:
 * - Apollo Client with MSW-compatible setup
 * - Jotai provider for atomic state
 * - userEvent instance for interaction simulation
 *
 * EXAMPLE - Basic render:
 * ```tsx
 * const { getByText } = render(<MyComponent />)
 * expect(getByText('Hello')).toBeInTheDocument()
 * ```
 *
 * EXAMPLE - User interactions:
 * ```tsx
 * const { user } = render(<LoginForm />)
 * await user.type(screen.getByLabelText('Email'), 'test@example.com')
 * await user.click(screen.getByRole('button', { name: 'Submit' }))
 * ```
 *
 * @returns All @testing-library queries plus a userEvent instance
 */
export function customRender(
  ui: ReactElement,
  options: CustomRenderOptions = {},
) {
  const { renderOptions } = options

  // Create a userEvent instance for simulating user interactions
  // userEvent is more realistic than fireEvent as it simulates actual browser behavior
  const user = userEvent.setup()

  const result = render(ui, {
    ...renderOptions,
    wrapper: TestWrapper,
  })

  return {
    ...result,
    // Include user for convenient interaction testing
    user,
  }
}

function getTestClient(): ApolloClient {
  testClient ??= createTestApolloClient()

  return testClient
}

/**
 * Test wrapper component that provides all necessary providers.
 *
 * Provider hierarchy (outer to inner):
 * 1. JotaiProvider - Atomic state management
 * 2. ApolloProvider - GraphQL client and cache
 */
function TestWrapper({ children }: { children: ReactNode }): React.JSX.Element {
  const client = getTestClient()

  return (
    <JotaiProvider>
      <ApolloProvider client={client}>{children}</ApolloProvider>
    </JotaiProvider>
  )
}

// Re-export selected utilities from @testing-library/react
// We don't use `export *` to avoid conflicts with our custom render
export {
  cleanup,
  fireEvent,
  screen,
  waitFor,
  within,
} from '@testing-library/react'

// Export types
export type { RenderResult } from '@testing-library/react'

// Export our custom render as the default render function
export { customRender as render }

// Export userEvent for tests that need direct access
export { userEvent }
