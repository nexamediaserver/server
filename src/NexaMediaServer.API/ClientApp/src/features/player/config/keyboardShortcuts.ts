import type { KeyboardShortcut } from '@/shared/hooks'

/**
 * Centralized keyboard shortcuts configuration for the media player
 *
 * This file contains all keyboard shortcuts used in the application.
 * Keeping them centralized makes it easier to:
 * - Document all available shortcuts
 * - Avoid conflicts between shortcuts
 * - Add/remove/modify shortcuts in one place
 */

/**
 * Creates keyboard shortcuts for the player
 *
 * @param handlers - Object containing handler functions for each action
 * @param conditions - Object containing condition functions for conditional shortcuts
 * @returns Array of keyboard shortcut configurations
 */
export function createPlayerShortcuts(
  handlers: {
    forward10Seconds: () => void
    jumpForward10Minutes: () => void
    rewind10Seconds: () => void
    skipBack10Minutes: () => void
    togglePlayPause: () => void
    toggleStats: () => void
  },
  conditions: {
    isPlayerMaximized: () => boolean
  },
): KeyboardShortcut[] {
  return [
    // Play/Pause - works only when player is maximized
    {
      condition: conditions.isPlayerMaximized,
      description: 'Play/Pause (when player is maximized)',
      handler: handlers.togglePlayPause,
      key: ['p', ' '], // P key or Spacebar
    },

    // Rewind 10 seconds - works only when player is maximized
    {
      condition: conditions.isPlayerMaximized,
      description: 'Rewind 10 seconds (when player is maximized)',
      handler: handlers.rewind10Seconds,
      key: 'ArrowLeft',
    },

    // Forward 10 seconds - works only when player is maximized
    {
      condition: conditions.isPlayerMaximized,
      description: 'Forward 10 seconds (when player is maximized)',
      handler: handlers.forward10Seconds,
      key: 'ArrowRight',
    },

    // Skip back 10 minutes - works only when player is maximized
    {
      condition: conditions.isPlayerMaximized,
      description: 'Skip back 10 minutes (when player is maximized)',
      handler: handlers.skipBack10Minutes,
      key: 'ArrowDown',
    },

    // Jump forward 10 minutes - works only when player is maximized
    {
      condition: conditions.isPlayerMaximized,
      description: 'Jump forward 10 minutes (when player is maximized)',
      handler: handlers.jumpForward10Minutes,
      key: 'ArrowUp',
    },

    // Toggle player stats - works only when player is maximized
    // Note: Also available via the player menu button for better accessibility
    {
      condition: conditions.isPlayerMaximized,
      description: 'Toggle player stats (when player is maximized)',
      handler: handlers.toggleStats,
      key: 's',
      modifiers: { shift: true },
    },
  ]
}

/**
 * Player keyboard shortcuts documentation
 *
 * Use this for displaying help text or generating documentation
 */
export const PLAYER_SHORTCUTS_DOCS = {
  global: [{ description: 'Toggle fullscreen', keys: ['F'] }],
  maximizedOnly: [
    { description: 'Play/Pause', keys: ['P', 'Spacebar'] },
    { description: 'Rewind 10 seconds', keys: ['←'] },
    { description: 'Forward 10 seconds', keys: ['→'] },
    { description: 'Skip back 10 minutes', keys: ['↓'] },
    { description: 'Jump forward 10 minutes', keys: ['↑'] },
    { description: 'Toggle player stats', keys: ['Shift+S'] },
  ],
} as const
