import { Link } from '@tanstack/react-router'
import { Duration } from 'luxon'
import { useState } from 'react'
import IconPlay from '~icons/material-symbols/play-arrow'

import type { Item } from '@/shared/api/graphql/graphql'

import { useStartPlayback } from '@/features/player/hooks/useStartPlayback'
import { Button } from '@/shared/components/ui/button'
import { cn } from '@/shared/lib/utils'

type PersonItem = Pick<Item, 'id' | 'metadataType' | 'title'>

type TrackItem = Pick<
  Item,
  | 'id'
  | 'index'
  | 'length'
  | 'librarySectionId'
  | 'metadataType'
  | 'thumbUri'
  | 'title'
  | 'viewOffset'
> & {
  persons?: null | PersonItem[]
}

type TrackRowProps = Readonly<{
  /** The album ID for playlist-based playback. When provided, playing a track plays the entire album. */
  albumId?: string
  track: TrackItem
  trackNumber?: number
}>

/**
 * Renders a single track row with play button, track number, title, and duration.
 */
export function TrackRow({ albumId, track, trackNumber }: TrackRowProps) {
  const { startPlaybackForItem, startPlaybackLoading } = useStartPlayback()
  const [isHovered, setIsHovered] = useState(false)

  const displayNumber = trackNumber ?? track.index
  const hasProgress = track.viewOffset > 0 && track.length > 0
  const progressPercentage = hasProgress
    ? Math.min((track.viewOffset / track.length) * 100, 100)
    : 0

  const handlePlay = () => {
    if (startPlaybackLoading) return
    void startPlaybackForItem({
      item: {
        id: track.id,
        metadataType: track.metadataType,
        thumbUri: track.thumbUri,
        title: track.title,
      },
      originatorId: albumId,
      playlistType: albumId ? 'album' : 'single',
    })
  }

  return (
    <div
      className={cn(
        `
          group relative flex items-center gap-4 rounded-md px-4 py-2
          transition-colors
        `,
        `
          cursor-pointer
          hover:bg-accent
        `,
      )}
      onClick={handlePlay}
      onKeyDown={(e) => {
        if (e.key === 'Enter' || e.key === ' ') {
          e.preventDefault()
          handlePlay()
        }
      }}
      onMouseEnter={() => {
        setIsHovered(true)
      }}
      onMouseLeave={() => {
        setIsHovered(false)
      }}
      role="button"
      tabIndex={0}
    >
      {/* Track number / Play button */}
      <div className="w-8 shrink-0 text-center">
        {isHovered || startPlaybackLoading ? (
          <Button
            className="size-8 p-0"
            disabled={startPlaybackLoading}
            onClick={(e) => {
              e.stopPropagation()
              handlePlay()
            }}
            size="icon"
            variant="ghost"
          >
            <IconPlay className="size-5" />
          </Button>
        ) : (
          <span className="text-sm text-muted-foreground tabular-nums">
            {displayNumber}
          </span>
        )}
      </div>

      {/* Track title and artists */}
      <div className="min-w-0 flex-1">
        <p className="truncate text-sm font-medium">{track.title}</p>
        {track.persons && track.persons.length > 0 && (
          <p className="truncate text-xs text-muted-foreground">
            {track.persons.slice(0, 3).map((person, index, array) => (
              <span key={person.id}>
                {track.librarySectionId ? (
                  <Link
                    className="hover:underline"
                    onClick={(e) => {
                      e.stopPropagation()
                    }}
                    params={{
                      contentSourceId: track.librarySectionId,
                      metadataItemId: person.id,
                    }}
                    to="/section/$contentSourceId/details/$metadataItemId"
                  >
                    {person.title}
                  </Link>
                ) : (
                  person.title
                )}
                {index < array.length - 1 && ', '}
              </span>
            ))}
            {track.persons.length > 3 && ', ...'}
          </p>
        )}
      </div>

      {/* Duration */}
      <div className="shrink-0 text-sm text-muted-foreground tabular-nums">
        {formatDuration(track.length)}
      </div>

      {/* Progress bar for partially played tracks */}
      {hasProgress && (
        <div
          className={`
            absolute right-4 bottom-0 left-4 h-0.5 overflow-hidden rounded-full
            bg-muted
          `}
        >
          <div
            className="h-full bg-primary transition-all"
            style={{ width: `${String(progressPercentage)}%` }}
          />
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

  const duration = Duration.fromMillis(ms)
  const hours = Math.floor(duration.as('hours'))

  if (hours > 0) {
    return duration.toFormat('h:mm:ss')
  }
  return duration.toFormat('m:ss')
}
