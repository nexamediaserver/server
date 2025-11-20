import type { ReactNode } from 'react'

import { useMemo } from 'react'

import { useKeyboardShortcuts } from '@/shared/hooks'

import { createGlobalShortcuts } from '../config/keyboardShortcuts'

/**
 * Component that registers global keyboard shortcuts for the entire application
 */
export function GlobalKeyboardShortcuts(): ReactNode {
  const toggleFullscreen = () => {
    if (!document.fullscreenElement) {
      // Enter fullscreen on the entire document
      document.documentElement.requestFullscreen().catch((err: unknown) => {
        console.error('Error attempting to enable fullscreen:', err)
      })
    } else {
      // Exit fullscreen
      document.exitFullscreen().catch((err: unknown) => {
        console.error('Error attempting to exit fullscreen:', err)
      })
    }
  }

  const shortcuts = useMemo(
    () =>
      createGlobalShortcuts({
        toggleFullscreen,
      }),
    [],
  )

  useKeyboardShortcuts(shortcuts, true)

  return null
}
