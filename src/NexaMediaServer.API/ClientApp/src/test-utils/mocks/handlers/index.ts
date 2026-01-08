/**
 * MSW Handler Registry
 *
 * This file aggregates all MSW handlers from domain-specific modules.
 * Handlers are organized by domain for maintainability as the test suite grows.
 *
 * ORGANIZATION:
 * - auth.ts    - Authentication queries/mutations (login, refresh, serverInfo)
 * - library.ts - Library section queries (list, get, create)
 * - metadata.ts - Metadata item queries (detail, search, edit)
 *
 * ADDING NEW HANDLERS:
 * 1. Create a new file for the domain (e.g., playback.ts)
 * 2. Export an array of handlers
 * 3. Import and spread into the handlers array below
 *
 * HANDLER PRIORITY:
 * Handlers are matched in order. Put more specific handlers before generic ones.
 */

import { authHandlers } from './auth'
import { libraryHandlers } from './library'
import { metadataHandlers } from './metadata'

/**
 * Combined array of all default handlers.
 *
 * These handlers are registered when the MSW server starts.
 * They provide default responses for common queries.
 *
 * Tests can override these using server.use() for specific scenarios.
 */
export const handlers = [
  ...authHandlers,
  ...libraryHandlers,
  ...metadataHandlers,
]
