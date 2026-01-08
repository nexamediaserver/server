/**
 * @module ItemProgress
 *
 * Displays playback progress for media items as a progress bar.
 *
 * This component is used across the application wherever we need to show
 * how much of a media item has been watched/played:
 * - Item cards in library views
 * - Detail page headers
 * - Continue watching sections
 * - Episode lists
 *
 * @example Basic usage
 * ```tsx
 * <ItemProgress
 *   length={item.duration}
 *   viewOffset={item.viewOffset}
 * />
 * ```
 *
 * @example With custom styling
 * ```tsx
 * <ItemProgress
 *   className="absolute bottom-0 left-0 right-0"
 *   length={episode.duration}
 *   viewOffset={episode.viewOffset}
 * />
 * ```
 */
import { useMemo } from 'react'

import { Progress } from '@/shared/components/ui/progress'

/**
 * Props for the ItemProgress component.
 */
export interface ItemProgressProps {
  /** Additional CSS classes to apply to the progress bar */
  className?: string

  /**
   * Total duration/length of the media item in milliseconds.
   * If null, undefined, or <= 0, the component renders nothing.
   */
  length?: null | number

  /**
   * Current playback position in milliseconds.
   * If null, undefined, or <= 0, the component renders nothing.
   */
  viewOffset?: null | number
}

/**
 * Renders a progress bar showing playback progress for a media item.
 *
 * The component calculates the percentage watched based on `viewOffset / length`
 * and renders a progress bar. If either value is missing or zero, the component
 * returns null (renders nothing).
 *
 * The progress is capped at 100% to handle edge cases where viewOffset
 * might exceed length due to seeking or timing inaccuracies.
 */
export function ItemProgress({
  className,
  length,
  viewOffset,
}: Readonly<ItemProgressProps>) {
  const value = useMemo(() => {
    const duration = length ?? 0
    const offset = viewOffset ?? 0

    // Don't show progress bar if we don't have valid data
    if (duration <= 0 || offset <= 0) {
      return 0
    }

    // Cap at 100% to handle edge cases
    return Math.min(100, (offset / duration) * 100)
  }, [length, viewOffset])

  // Don't render anything if there's no progress to show
  if (value <= 0) {
    return null
  }

  return <Progress className={className} value={value} />
}
