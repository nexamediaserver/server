/**
 * Test Utilities - Main Export
 *
 * This is the main entry point for test utilities. Import everything
 * you need for testing from this module:
 *
 * ```tsx
 * import { render, screen, waitFor, userEvent } from '@/test-utils'
 * ```
 *
 * WHAT'S INCLUDED:
 * - Custom render function with all providers
 * - All @testing-library/react exports (screen, waitFor, etc.)
 * - userEvent for user interaction simulation
 * - Mock utilities for Apollo Client
 *
 * WHAT'S NOT INCLUDED (import separately):
 * - MSW server (import from '@/test-utils/mocks/server')
 * - MSW utilities (import from 'msw')
 */

// Export Apollo mock utilities
export { createMockCache, createMockClient } from './mocks/apollo'

// Re-export everything from render (includes all @testing-library/react exports)
export * from './render'
