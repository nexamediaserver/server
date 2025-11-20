import type { ReactNode } from 'react'

import { useMemo } from 'react'

import { handleErrorStandalone, useKeyboardShortcuts } from '@/shared/hooks'

import { createGlobalShortcuts } from '../config/keyboardShortcuts'

/**
 * Component that registers global keyboard shortcuts for the entire application
 */
export function GlobalKeyboardShortcuts(): ReactNode {
  const toggleFullscreen = () => {
    if (document.fullscreenElement) {
      // Exit fullscreen
      document.exitFullscreen().catch((err: unknown) => {
        handleErrorStandalone(err, {
          context: 'GlobalKeyboardShortcuts.fullscreen',
          notify: false,
        })
      })
    } else {
      // Enter fullscreen on the entire document
      document.documentElement.requestFullscreen().catch((err: unknown) => {
        handleErrorStandalone(err, {
          context: 'GlobalKeyboardShortcuts.fullscreen',
          notify: false,
        })
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
