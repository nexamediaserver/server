import { atom } from 'jotai'
import { atomWithStorage } from 'jotai/utils'

/**
 * Shared UI atoms – used across multiple features.
 * Feature-specific atoms live in their respective feature folders:
 *   - Auth: @/features/auth/store
 *   - Player/Playback: @/features/player/store
 */

/** Sidebar collapsed state */
export const sidebarCollapsedAtom = atom(false)

/** Global notification message */
export const notificationAtom = atom<null | string>(null)

/**
 * Global UI: ItemCard width (Tailwind spacing token between 32 and 52)
 * Store the numeric Tailwind spacing key; default matches current UI (w-52)
 */
export const itemCardWidthAtom = atomWithStorage<number>('ui:itemCardWidth', 40)
