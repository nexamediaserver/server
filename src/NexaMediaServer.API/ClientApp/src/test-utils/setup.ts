/**
 * Test Setup File
 *
 * This file runs before each test file and sets up the testing environment.
 * It's referenced in vitest.config.ts via the setupFiles option.
 *
 * What this file does:
 * 1. Imports @testing-library/jest-dom for extended DOM matchers
 * 2. Sets up MSW server to intercept network requests
 * 3. Provides global cleanup and error handling
 */

// Import jest-dom matchers for enhanced DOM assertions
// This adds matchers like: toBeInTheDocument, toHaveClass, toBeVisible, etc.
import '@testing-library/jest-dom/vitest'
import { afterAll, afterEach, beforeAll } from 'vitest'

import { server } from './mocks/server'

/**
 * MSW Server Lifecycle
 *
 * Before all tests: Start the MSW server to intercept requests
 * After each test: Reset handlers to default (removes test-specific overrides)
 * After all tests: Close the server
 *
 * This ensures each test starts with a clean slate and
 * test-specific handlers don't leak between tests.
 */
beforeAll(() => {
  server.listen({
    // Warn when requests are made without a matching handler
    // Change to 'error' for stricter testing
    onUnhandledRequest: 'warn',
  })
})

afterEach(() => {
  // Reset handlers between tests so test-specific handlers don't persist
  server.resetHandlers()
  // Clear localStorage between tests
  localStorage.clear()
})

afterAll(() => {
  server.close()
})

/**
 * Global test environment setup
 *
 * Mock browser APIs that aren't available in jsdom but are used by the app.
 */

// Mock localStorage for tests
const localStorageMock = (() => {
  let store: Record<string, string> = {}
  return {
    clear: () => {
      store = {}
    },
    getItem: (key: string) => store[key] ?? null,
    key: (index: number) => Object.keys(store)[index] ?? null,
    get length() {
      return Object.keys(store).length
    },
    removeItem: (key: string) => {
      // Using Reflect.deleteProperty instead of delete for computed keys
      Reflect.deleteProperty(store, key)
    },
    setItem: (key: string, value: string) => {
      store[key] = value
    },
  }
})()
Object.defineProperty(window, 'localStorage', { value: localStorageMock })

// Noop function for mock implementations
const noop = () => {
  /* intentionally empty - mock implementation */
}

// Mock window.matchMedia for responsive component tests
Object.defineProperty(window, 'matchMedia', {
  value: (query: string) => ({
    addEventListener: noop,
    addListener: noop, // deprecated
    dispatchEvent: () => false,
    matches: false,
    media: query,
    onchange: null,
    removeEventListener: noop,
    removeListener: noop, // deprecated
  }),
  writable: true,
})

// Mock ResizeObserver for components that use it
class ResizeObserverMock {
  disconnect = noop
  observe = noop
  unobserve = noop
}
window.ResizeObserver = ResizeObserverMock

// Mock IntersectionObserver for lazy-loading components
class IntersectionObserverMock {
  disconnect = noop
  observe = noop
  readonly root = null
  readonly rootMargin = ''
  readonly thresholds: readonly number[] = []
  unobserve = noop
  takeRecords(): IntersectionObserverEntry[] {
    return []
  }
}
window.IntersectionObserver = IntersectionObserverMock
