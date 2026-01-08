/**
 * App Component Test
 *
 * This is a basic smoke test to ensure the App component renders
 * without crashing. It's intentionally minimal because the App
 * component is mostly a composition of providers.
 *
 * For more meaningful tests, test the individual features:
 * - features/auth/__tests__ - Authentication flows
 * - features/player/__tests__ - Playback functionality
 * - shared/components/*.test.tsx - UI components
 */

import { render } from '@testing-library/react'
import { describe, it } from 'vitest'

import { App } from '.'

describe('App', () => {
  /**
   * Smoke test: App renders without throwing.
   *
   * This catches issues like:
   * - Missing providers
   * - Import errors
   * - Initial state errors
   *
   * Note: We use raw @testing-library/react render here because
   * App already includes its own providers. Using the custom render
   * from test-utils would double-wrap with providers.
   */
  it('renders without crashing', () => {
    // This test just verifies the App can mount without throwing
    // We wrap in try/catch to get a cleaner error if it fails
    const { unmount } = render(<App />)
    // Clean up
    unmount()
  })
})
