/**
 * MSW Server Setup
 *
 * This module configures Mock Service Worker (MSW) for intercepting
 * network requests during tests. MSW intercepts requests at the network
 * level, making it ideal for integration testing.
 *
 * HOW IT WORKS:
 * 1. MSW intercepts fetch/XHR requests before they leave the app
 * 2. Requests are matched against registered handlers
 * 3. Handlers return mock responses without hitting real servers
 *
 * WHY MSW?
 * - Tests run faster (no network latency)
 * - Tests are deterministic (same response every time)
 * - No need for a running backend
 * - Tests can simulate error states easily
 *
 * ADDING TEST-SPECIFIC HANDLERS:
 * ```tsx
 * import { server } from '@/test-utils/mocks/server'
 * import { graphql, HttpResponse } from 'msw'
 *
 * test('handles error state', async () => {
 *   // Override the default handler for this test only
 *   server.use(
 *     graphql.query('MyQuery', () => {
 *       return HttpResponse.json({
 *         errors: [{ message: 'Something went wrong' }]
 *       })
 *     })
 *   )
 *
 *   // ... test error handling
 * })
 * ```
 *
 * The handler will be reset after the test due to server.resetHandlers()
 * in the setup file.
 */

import { setupServer } from 'msw/node'

import { handlers } from './handlers'

/**
 * MSW server instance for Node.js (test environment).
 *
 * This server is started in setup.ts and intercepts all network requests.
 * Default handlers are registered from ./handlers/index.ts.
 *
 * Use server.use() in individual tests to add or override handlers.
 */
export const server = setupServer(...handlers)
