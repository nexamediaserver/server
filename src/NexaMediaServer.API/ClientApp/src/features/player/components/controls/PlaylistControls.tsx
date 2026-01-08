import type { ReactNode } from 'react'

import IconRepeat from '~icons/material-symbols/repeat'
import IconRepeatOne from '~icons/material-symbols/repeat-one'
import IconShuffle from '~icons/material-symbols/shuffle'

import { Button } from '@/shared/components/ui/button'
import { cn } from '@/shared/lib/utils'

interface PlaylistControlsProps {
  /** Whether repeat mode is enabled */
  isRepeat: boolean
  /** Whether shuffle mode is enabled */
  isShuffle: boolean
  /** Handler for toggling repeat mode */
  onToggleRepeat: () => void
  /** Handler for toggling shuffle mode */
  onToggleShuffle: () => void
  /** Whether to show a "repeat one" icon variant when repeat is on. Defaults to false. */
  showRepeatOne?: boolean
}

/**
 * Playlist mode control buttons (shuffle, repeat).
 */
export function PlaylistControls({
  isRepeat,
  isShuffle,
  onToggleRepeat,
  onToggleShuffle,
  showRepeatOne = false,
}: PlaylistControlsProps): ReactNode {
  return (
    <div className="flex items-center gap-1">
      <Button
        aria-label={isShuffle ? 'Disable shuffle' : 'Enable shuffle'}
        className={cn('transition-colors', isShuffle && 'text-primary')}
        onClick={onToggleShuffle}
        size="icon"
        variant="ghost"
      >
        <IconShuffle className="h-4 w-4" />
      </Button>
      <Button
        aria-label={isRepeat ? 'Disable repeat' : 'Enable repeat'}
        className={cn('transition-colors', isRepeat && 'text-primary')}
        onClick={onToggleRepeat}
        size="icon"
        variant="ghost"
      >
        {showRepeatOne && isRepeat ? (
          <IconRepeatOne className="h-4 w-4" />
        ) : (
          <IconRepeat className="h-4 w-4" />
        )}
      </Button>
    </div>
  )
}
