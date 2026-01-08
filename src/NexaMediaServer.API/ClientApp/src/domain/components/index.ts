/**
 * @module @/domain/components
 *
 * Domain-specific React components for media-server concepts.
 *
 * These components represent business domain entities and are shared
 * across multiple features. They're distinct from generic UI primitives
 * in `@/shared/components/ui`.
 *
 * @example
 * ```tsx
 * import { ItemProgress, MetadataTypeIcon } from '@/domain/components'
 *
 * function ItemCard({ item }) {
 *   return (
 *     <div>
 *       <MetadataTypeIcon item={item} />
 *       <ItemProgress length={item.duration} viewOffset={item.viewOffset} />
 *     </div>
 *   )
 * }
 * ```
 */

export { ItemProgress, type ItemProgressProps } from './ItemProgress'
export {
  MetadataTypeIcon,
  type MetadataTypeIconProps,
} from './MetadataTypeIcon'
