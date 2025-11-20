import type { KeyboardShortcut } from '@/shared/hooks'

/**
 * Global keyboard shortcuts that work throughout the application
 */

/**
 * Creates global keyboard shortcuts
 *
 * @param handlers - Object containing handler functions for each action
 * @returns Array of keyboard shortcut configurations
 */
export function createGlobalShortcuts(handlers: {
  toggleFullscreen: () => void
}): KeyboardShortcut[] {
  return [
    {
      description: 'Toggle fullscreen',
      handler: handlers.toggleFullscreen,
      key: 'f',
    },
  ]
}

/**
 * Global keyboard shortcuts documentation
 *
 * Use this for displaying help text or generating documentation
 */
export const GLOBAL_SHORTCUTS_DOCS = {
  shortcuts: [{ description: 'Toggle fullscreen', keys: ['F'] }],
} as const
