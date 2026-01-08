import type { ReactNode, RefObject } from 'react'

import { cn } from '@/shared/lib/utils'

interface ProgressBarProps {
  /** Current buffered time in milliseconds */
  bufferedTime: number
  /** Current playback time in milliseconds */
  currentTime: number
  /** Total duration in milliseconds */
  duration: number
  /** Handler for progress change (seek) */
  onProgressChange: (event: React.ChangeEvent<HTMLInputElement>) => void
  /** Handler for mouse movement over progress bar */
  onProgressMouseMove: (e: React.MouseEvent<HTMLDivElement>) => void
  /** Handler for tooltip visibility */
  onTooltipVisibilityChange: (visible: boolean) => void
  /** Ref to the progress bar input element */
  progressBarRef: RefObject<HTMLInputElement | null>
  /** Thumbnail data for preview */
  thumbnail: null | {
    height: number
    imageHeight: number
    imageUrl: string
    imageWidth: number
    positionX: number
    positionY: number
    width: number
  }
  /** Position and time for tooltip */
  tooltipPosition: { percentage: number; time: number }
  /** Ref to the tooltip element */
  tooltipRef: RefObject<HTMLDivElement | null>
  /** Whether tooltip is visible */
  tooltipVisible: boolean
}

/**
 * Progress bar component with seek functionality and thumbnail preview.
 */
export function ProgressBar({
  bufferedTime,
  currentTime,
  duration,
  onProgressChange,
  onProgressMouseMove,
  onTooltipVisibilityChange,
  progressBarRef,
  thumbnail,
  tooltipPosition,
  tooltipRef,
  tooltipVisible,
}: ProgressBarProps): ReactNode {
  // Calculate clamped tooltip position to prevent it from going off-screen
  const getClampedTooltipPosition = () => {
    if (!progressBarRef.current || !tooltipRef.current) {
      return {
        left: `${String(tooltipPosition.percentage * 100)}%`,
        transform: 'translateX(-50%)',
      }
    }

    const progressRect = progressBarRef.current.getBoundingClientRect()
    const tooltipRect = tooltipRef.current.getBoundingClientRect()

    // Calculate the desired center position of the tooltip
    const desiredLeft =
      progressRect.left + tooltipPosition.percentage * progressRect.width

    // Calculate tooltip half-width
    const tooltipHalfWidth = tooltipRect.width / 2

    // Padding from screen edges
    const edgePadding = 8

    // Calculate clamped position
    const minLeft = edgePadding + tooltipHalfWidth
    const maxLeft = globalThis.innerWidth - edgePadding - tooltipHalfWidth
    const clampedLeft = Math.max(minLeft, Math.min(maxLeft, desiredLeft))

    // Calculate the offset needed
    const offset = clampedLeft - progressRect.left
    const offsetPercentage = (offset / progressRect.width) * 100

    // Calculate max width to prevent overflow
    const leftEdge = clampedLeft - tooltipHalfWidth
    const rightEdge = clampedLeft + tooltipHalfWidth

    let maxWidth: string | undefined
    if (leftEdge < edgePadding) {
      // Constrain from left edge
      maxWidth = `${String((clampedLeft - edgePadding) * 2)}px`
    } else if (rightEdge > globalThis.innerWidth - edgePadding) {
      // Constrain from right edge
      maxWidth = `${String((globalThis.innerWidth - edgePadding - clampedLeft) * 2)}px`
    }

    return {
      left: `${String(offsetPercentage)}%`,
      maxWidth,
      transform: 'translateX(-50%)',
    }
  }

  return (
    <div className="relative -mt-2 w-full py-2">
      <input
        aria-label="Seek"
        className={cn(
          'absolute inset-0 z-10 h-6 w-full cursor-pointer opacity-0',
        )}
        max={duration}
        min={0}
        onBlur={() => {
          onTooltipVisibilityChange(false)
        }}
        onChange={onProgressChange}
        onFocus={() => {
          onTooltipVisibilityChange(true)
        }}
        onMouseEnter={() => {
          onTooltipVisibilityChange(true)
        }}
        onMouseLeave={() => {
          onTooltipVisibilityChange(false)
        }}
        onMouseMove={onProgressMouseMove}
        ref={progressBarRef}
        step={1}
        type="range"
        value={currentTime}
      />
      <div className="relative h-1 bg-stone-900">
        {/* Buffer indicator */}
        <div
          className="absolute h-full bg-primary/20 transition-all"
          style={{
            width: `${String(duration > 0 ? (bufferedTime / duration) * 100 : 0)}%`,
          }}
        />
        {/* Current progress */}
        <div
          className="absolute h-full bg-primary transition-all"
          style={{
            width: `${String(duration > 0 ? (currentTime / duration) * 100 : 0)}%`,
          }}
        />
      </div>
      {/* Custom tooltip */}
      {tooltipVisible && (
        <div
          className="pointer-events-none absolute bottom-full z-50 mb-2"
          ref={tooltipRef}
          style={{
            ...getClampedTooltipPosition(),
            width: 'max-content',
          }}
        >
          {/* Thumbnail preview */}
          {thumbnail && (
            <div
              className={`
                mb-2 aspect-video max-h-40 overflow-hidden border border-primary
                bg-background shadow-md
              `}
            >
              <img
                alt=""
                aria-hidden="true"
                className={`h-full w-full object-contain`}
                src={thumbnail.imageUrl}
              />
            </div>
          )}
          {/* Time tooltip */}
          <div
            className={cn(
              thumbnail && 'absolute bottom-4 left-1/2 -translate-x-1/2',
              `
                rounded-md bg-primary px-3 py-1.5 text-xs
                text-primary-foreground shadow-md
              `,
            )}
          >
            {formatDuration(tooltipPosition.time)}
          </div>
        </div>
      )}
    </div>
  )
}

/**
 * Formats milliseconds to a human-readable duration string (H:MM:SS or M:SS)
 */
function formatDuration(ms: number): string {
  if (!Number.isFinite(ms) || ms < 0) {
    return '--:--'
  }

  const totalSeconds = Math.floor(ms / 1000)
  const hours = Math.floor(totalSeconds / 3600)
  const minutes = Math.floor((totalSeconds % 3600) / 60)
  const seconds = totalSeconds % 60

  if (hours > 0) {
    return `${String(hours)}:${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`
  }
  return `${String(minutes)}:${String(seconds).padStart(2, '0')}`
}
