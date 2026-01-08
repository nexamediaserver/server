import type { ReactNode } from 'react'

export interface MediaInfoProps {
  /** Album name (for audio) */
  album?: string
  /** Artist name (for audio) */
  artist?: string
  /** Current playback time in milliseconds */
  currentTime?: number
  /** Total duration in milliseconds */
  duration?: number
  /** Current item index in playlist (0-based) */
  playlistIndex?: number
  /** Total number of items in playlist */
  playlistTotalCount?: number
  /** Whether to show time display. Defaults to true. */
  showTime?: boolean
  /** Media title */
  title: string
  /** Layout variant: 'compact' for minimized bar, 'expanded' for audio player */
  variant?: 'compact' | 'expanded'
}

/**
 * Media information display (title, artist, album, and time).
 */
export function MediaInfo({
  album,
  artist,
  currentTime,
  duration,
  playlistIndex,
  playlistTotalCount,
  showTime = true,
  title,
  variant = 'compact',
}: MediaInfoProps): ReactNode {
  if (variant === 'expanded') {
    return (
      <div className="flex flex-col items-center text-center">
        <div className="max-w-md truncate text-xl font-semibold">{title}</div>
        {artist && (
          <div
            className={`mt-1 max-w-md truncate text-base text-muted-foreground`}
          >
            {artist}
          </div>
        )}
        {album && (
          <div
            className={`
              mt-0.5 max-w-md truncate text-sm text-muted-foreground/70
            `}
          >
            {album}
          </div>
        )}
        {showTime && currentTime !== undefined && duration !== undefined && (
          <div className="mt-2 text-sm text-muted-foreground">
            <span>{formatDuration(currentTime)}</span>
            <span className="mx-1">/</span>
            <span>{formatDuration(duration)}</span>
          </div>
        )}
      </div>
    )
  }

  return (
    <div className="mb-2.5 ml-3 text-nowrap">
      <div className="truncate text-sm font-medium">{title}</div>
      {(artist ?? album) && (
        <div className="truncate text-xs text-muted-foreground">
          {artist}
          {artist && album && ' • '}
          {album}
        </div>
      )}
      {showTime && currentTime !== undefined && duration !== undefined && (
        <div className="text-sm text-muted-foreground">
          <span>{formatDuration(currentTime)}</span>
          <span className="mx-1">/</span>
          <span>{formatDuration(duration)}</span>
        </div>
      )}
      {playlistTotalCount !== undefined &&
        playlistTotalCount > 0 &&
        playlistIndex !== undefined && (
          <div className="text-xs text-muted-foreground">
            {playlistIndex + 1} / {playlistTotalCount}
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
