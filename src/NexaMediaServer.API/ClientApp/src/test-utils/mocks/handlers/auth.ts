/**
 * Authentication MSW Handlers
 *
 * Handlers for authentication-related GraphQL operations.
 * These provide default mock responses for auth queries and mutations.
 *
 * INCLUDED OPERATIONS:
 * - ServerInfo query - Returns server version and environment info
 * - (Add more as needed: login, refresh, logout, etc.)
 *
 * CUSTOMIZING RESPONSES:
 * Override these in individual tests using server.use():
 *
 * ```tsx
 * import { server } from '@/test-utils/mocks/server'
 * import { graphql, HttpResponse } from 'msw'
 *
 * test('shows development badge in dev mode', async () => {
 *   server.use(
 *     graphql.query('ServerInfo', () => {
 *       return HttpResponse.json({
 *         data: {
 *           serverInfo: {
 *             versionString: '1.0.0-dev',
 *             isDevelopment: true,  // Override to test dev UI
 *           }
 *         }
 *       })
 *     })
 *   )
 *   // ... assertions
 * })
 * ```
 */

import { graphql, HttpResponse } from 'msw'

/**
 * Default ServerInfo query handler.
 *
 * Returns mock server information used by the login page
 * and other components that display version info.
 */
const serverInfoHandler = graphql.query('ServerInfo', () => {
  return HttpResponse.json({
    data: {
      serverInfo: {
        __typename: 'ServerInfo',
        isDevelopment: false,
        versionString: '1.0.0-test',
      },
    },
  })
})

/**
 * Export all auth handlers as an array.
 * This array is spread into the main handlers in ./index.ts
 */
export const authHandlers = [serverInfoHandler]
