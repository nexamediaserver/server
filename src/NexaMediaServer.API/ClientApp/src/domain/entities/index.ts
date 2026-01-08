/**
 * @module @/domain/entities
 *
 * Entity-specific utilities, types, and helpers for domain concepts.
 *
 * Each subdirectory represents a domain entity (item, library, metadata)
 * and contains utilities for working with that entity.
 *
 * @example
 * ```tsx
 * import { getImageUrl, IMAGE_SIZES } from '@/domain/entities'
 * // or more specifically:
 * import { getImageUrl } from '@/domain/entities/item'
 * ```
 */

export * from './item'
export * from './library'
export * from './metadata'
