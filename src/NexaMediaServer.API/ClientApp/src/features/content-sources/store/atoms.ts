import { atomWithStorage } from 'jotai/utils'

import type { ContentSourceViewMode } from '../routes'

/**
 * Per-library view mode preferences.
 * Maps library section ID to view mode ('browse' | 'discover').
 * Persisted to localStorage under the key 'ui:libraryViewModes'.
 */
export const libraryViewModesAtom = atomWithStorage<
  Record<string, ContentSourceViewMode>
>('ui:libraryViewModes', {})
