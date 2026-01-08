import type { ReactNode } from 'react'

import IconZoomReset from '~icons/material-symbols/fit-screen'
import IconSkipNext from '~icons/material-symbols/skip-next-rounded'
import IconSkipPrevious from '~icons/material-symbols/skip-previous-rounded'
import IconZoomIn from '~icons/material-symbols/zoom-in'
import IconZoomOut from '~icons/material-symbols/zoom-out'

import { Button } from '@/shared/components/ui/button'

import { PlaylistControls } from './PlaylistControls'

interface ImageControlsProps {
  /** Whether there's a next item in the playlist */
  hasNext?: boolean
  /** Whether there's a previous item in the playlist */
  hasPrevious?: boolean
  /** Whether the player is maximized */
  isMaximized: boolean
  /** Whether repeat mode is enabled */
  isRepeat?: boolean
  /** Whether shuffle mode is enabled */
  isShuffle?: boolean
  /** Handler for navigating to the next image */
  onNext?: () => void
  /** Handler for navigating to the previous image */
  onPrevious?: () => void
  /** Handler for resetting zoom to original scale */
  onResetZoom: () => void
  /** Handler for toggling repeat mode */
  onToggleRepeat?: () => void
  /** Handler for toggling shuffle mode */
  onToggleShuffle?: () => void
  /** Handler for zooming in */
  onZoomIn: () => void
  /** Handler for zooming out */
  onZoomOut: () => void
}

/**
 * Control bar for the image viewer with centered controls.
 * Includes: Previous/Next navigation, Zoom controls, Shuffle/Repeat modes
 */
export function ImageControls({
  hasNext = false,
  hasPrevious = false,
  isMaximized,
  isRepeat = false,
  isShuffle = false,
  onNext,
  onPrevious,
  onResetZoom,
  onToggleRepeat,
  onToggleShuffle,
  onZoomIn,
  onZoomOut,
}: ImageControlsProps): ReactNode {
  return (
    <div className="mb-2.5 flex items-center gap-1">
      {/* Navigation controls */}
      {onPrevious && (
        <Button
          aria-label="Previous image"
          disabled={!hasPrevious}
          onClick={onPrevious}
          size="icon"
          variant="ghost"
        >
          <IconSkipPrevious className="h-5 w-5" />
        </Button>
      )}

      {/* Zoom controls (only when maximized) */}
      {isMaximized && (
        <>
          <Button
            aria-label="Zoom out"
            onClick={onZoomOut}
            size="icon"
            variant="ghost"
          >
            <IconZoomOut className="h-5 w-5" />
          </Button>
          <Button
            aria-label="Reset zoom"
            onClick={onResetZoom}
            size="icon"
            variant="ghost"
          >
            <IconZoomReset className="h-5 w-5" />
          </Button>
          <Button
            aria-label="Zoom in"
            onClick={onZoomIn}
            size="icon"
            variant="ghost"
          >
            <IconZoomIn className="h-5 w-5" />
          </Button>
        </>
      )}

      {onNext && (
        <Button
          aria-label="Next image"
          disabled={!hasNext}
          onClick={onNext}
          size="icon"
          variant="ghost"
        >
          <IconSkipNext className="h-5 w-5" />
        </Button>
      )}

      {/* Playlist controls (shuffle/repeat) */}
      {onToggleRepeat && onToggleShuffle && (
        <PlaylistControls
          isRepeat={isRepeat}
          isShuffle={isShuffle}
          onToggleRepeat={onToggleRepeat}
          onToggleShuffle={onToggleShuffle}
        />
      )}
    </div>
  )
}
