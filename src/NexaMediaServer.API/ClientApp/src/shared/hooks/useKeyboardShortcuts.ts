import { useEffect } from 'react'

/**
 * Keyboard shortcut configuration
 */
export interface KeyboardShortcut {
  /**
   * Whether to allow this shortcut when an input element is focused
   * @default false
   */
  allowInInput?: boolean
  /**
   * Whether this shortcut should only work when a specific condition is met
   */
  condition?: () => boolean
  /**
   * Description of what this shortcut does (for documentation)
   */
  description: string
  /**
   * The callback function to execute when the shortcut is triggered
   */
  handler: (event: KeyboardEvent) => void
  /**
   * The key(s) that trigger this shortcut
   * Can be a single key or an array of keys (for multiple shortcuts that do the same thing)
   */
  key: string | string[]
  /**
   * Whether to prevent the default browser behavior for this key
   * @default true
   */
  preventDefault?: boolean
}

/**
 * Utility function to get all registered shortcuts for documentation
 */
export function getShortcutDocumentation(
  shortcuts: KeyboardShortcut[],
): { description: string; keys: string[] }[] {
  return shortcuts.map((shortcut) => ({
    description: shortcut.description,
    keys: Array.isArray(shortcut.key) ? shortcut.key : [shortcut.key],
  }))
}

/**
 * Hook to register global keyboard shortcuts
 *
 * @param shortcuts - Array of keyboard shortcut configurations
 * @param enabled - Whether the shortcuts are enabled (default: true)
 *
 * @example
 * ```tsx
 * useKeyboardShortcuts([
 *   {
 *     key: 'f',
 *     description: 'Toggle fullscreen',
 *     handler: toggleFullscreen,
 *   },
 *   {
 *     key: ['p', ' '], // Multiple keys for same action
 *     description: 'Play/Pause',
 *     handler: togglePlayPause,
 *     condition: () => isPlayerActive,
 *   },
 * ])
 * ```
 */
export function useKeyboardShortcuts(
  shortcuts: KeyboardShortcut[],
  enabled = true,
) {
  useEffect(() => {
    if (!enabled) return

    const handleKeyDown = (event: KeyboardEvent) => {
      // Check if user is typing in an input/textarea/contenteditable
      const target = event.target as HTMLElement
      const isInputFocused =
        target.tagName === 'INPUT' ||
        target.tagName === 'TEXTAREA' ||
        target.isContentEditable

      for (const shortcut of shortcuts) {
        // Check if shortcut is allowed in input elements
        if (isInputFocused && !shortcut.allowInInput) {
          continue
        }

        // Check if the condition is met (if provided)
        if (shortcut.condition && !shortcut.condition()) {
          continue
        }

        // Normalize key(s) to array
        const keys = Array.isArray(shortcut.key) ? shortcut.key : [shortcut.key]

        // Check if any of the keys match
        const keyMatches = keys.some((key) => {
          // Handle special keys
          if (key.toLowerCase() === event.key.toLowerCase()) {
            return true
          }
          // Handle space as a special case
          if (key === ' ' && event.key === ' ') {
            return true
          }
          return false
        })

        if (keyMatches) {
          // Prevent default if specified (default: true)
          if (shortcut.preventDefault !== false) {
            event.preventDefault()
          }

          shortcut.handler(event)
          // Only execute the first matching shortcut
          break
        }
      }
    }

    globalThis.addEventListener('keydown', handleKeyDown)

    return () => {
      globalThis.removeEventListener('keydown', handleKeyDown)
    }
  }, [shortcuts, enabled])
}
