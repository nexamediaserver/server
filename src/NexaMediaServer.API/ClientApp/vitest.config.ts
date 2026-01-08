import { resolve } from 'node:path'
import icons from 'unplugin-icons/vite'
import { defineConfig } from 'vitest/config'

/**
 * Vitest configuration for the Nexa Media Server web client.
 *
 * This configuration extends the Vite setup with testing-specific settings:
 * - jsdom environment for DOM testing
 * - Global test APIs (describe, it, expect) without imports
 * - Path aliases matching tsconfig
 * - Setup file for @testing-library/jest-dom matchers
 * - Icon plugin for components using unplugin-icons
 */
export default defineConfig({
  plugins: [
    // Enable unplugin-icons for tests (used by many components)
    icons({
      autoInstall: true,
      compiler: 'jsx',
      jsx: 'react',
    }),
  ],
  resolve: {
    alias: {
      '@': resolve(__dirname, './src'),
    },
  },
  test: {
    // Coverage configuration
    coverage: {
      exclude: [
        'src/**/*.test.{ts,tsx}',
        'src/**/*.spec.{ts,tsx}',
        'src/test-utils/**',
        'src/**/__tests__/**',
        'src/shared/api/graphql/**', // Generated files
      ],
      include: ['src/**/*.{ts,tsx}'],
      provider: 'v8',
      reporter: ['text', 'json', 'html'],
    },

    // Use jsdom for DOM testing with React components
    environment: 'jsdom',

    // Enable global test APIs (describe, it, expect, vi)
    // This avoids needing to import them in every test file
    globals: true,

    // Include test files matching these patterns
    include: ['src/**/*.{test,spec}.{ts,tsx}'],

    // Setup file runs before each test file
    // Includes @testing-library/jest-dom matchers
    setupFiles: ['./src/test-utils/setup.ts'],
  },
})
