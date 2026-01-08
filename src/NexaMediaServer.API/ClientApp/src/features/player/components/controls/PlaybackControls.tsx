import type { ReactNode } from 'react'

import IconForward10 from '~icons/material-symbols/forward-10'
import IconPause from '~icons/material-symbols/pause'
import IconPlay from '~icons/material-symbols/play-arrow'
import IconReplay10 from '~icons/material-symbols/replay-10'
import IconNext from '~icons/material-symbols/skip-next'
import IconPrevious from '~icons/material-symbols/skip-previous'
import IconStop from '~icons/material-symbols/stop'

import { Button } from '@/shared/components/ui/button'

interface PlaybackControlsProps {
  /** Whether playback is currently playing */
  isPlaying: boolean
  /** Handler for fast forward 10 seconds */
  onFastForward10: () => void
  /** Handler for next track/item */
  onNext?: () => void
  /** Handler for previous track/item */
  onPrevious?: () => void
  /** Handler for rewind 10 seconds */
  onRewind10: () => void
  /** Handler for stop playback */
  onStop: () => void
  /** Handler for play/pause toggle */
  onTogglePlayPause: () => void
  /** Whether to show skip buttons (rewind/forward 10s). Defaults to true. */
  showSkipButtons?: boolean
}

/**
 * Playback control buttons (previous, rewind, play/pause, forward, next, stop).
 */
export function PlaybackControls({
  isPlaying,
  onFastForward10,
  onNext,
  onPrevious,
  onRewind10,
  onStop,
  onTogglePlayPause,
  showSkipButtons = true,
}: PlaybackControlsProps): ReactNode {
  return (
    <div className="mx-5 flex shrink items-center justify-center">
      <Button
        aria-label="Previous"
        disabled={!onPrevious}
        onClick={onPrevious}
        size="icon"
        variant="ghost"
      >
        <IconPrevious />
      </Button>
      {showSkipButtons && (
        <Button
          aria-label="Rewind 10 seconds"
          onClick={onRewind10}
          size="icon"
          variant="ghost"
        >
          <IconReplay10 />
        </Button>
      )}
      <Button
        aria-label="Play/Pause"
        onClick={onTogglePlayPause}
        size="icon"
        variant="ghost"
      >
        {isPlaying ? <IconPause /> : <IconPlay />}
      </Button>
      {showSkipButtons && (
        <Button
          aria-label="Fast Forward 10 seconds"
          onClick={onFastForward10}
          size="icon"
          variant="ghost"
        >
          <IconForward10 />
        </Button>
      )}
      <Button
        aria-label="Next"
        disabled={!onNext}
        onClick={onNext}
        size="icon"
        variant="ghost"
      >
        <IconNext />
      </Button>
      <Button aria-label="Stop" onClick={onStop} size="icon" variant="ghost">
        <IconStop />
      </Button>
    </div>
  )
}
