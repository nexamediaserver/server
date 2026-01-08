/**
 * @module @/domain
 *
 * Domain layer - shared business logic and domain-specific components.
 *
 * This module re-exports all domain concepts for convenient importing.
 * For more granular imports, use specific submodules:
 *
 * @example
 * ```tsx
 * // Bulk import (tree-shakes in production)
 * import { ItemProgress, MetadataTypeIcon, getImageTranscodeUrl } from '@/domain'
 *
 * // Granular import
 * import { ItemProgress } from '@/domain/components'
 * import { getImageTranscodeUrl } from '@/domain/utils'
 * import { formatDuration } from '@/domain/utils/formatters'
 * ```
 */

// Re-export components
export * from './components'

// Re-export constants
export * from './constants'

// Re-export entity utilities
export * from './entities'

// Re-export types
export type * from './types'

// Re-export utilities
export * from './utils'
