import type { ReactNode } from 'react'

import type { PlaylistItemPayload } from '@/shared/api/graphql/graphql'

import { Image } from '@/shared/components/Image'
import { cn } from '@/shared/lib/utils'

export interface PlaylistItemProps {
  /** Whether this item is currently playing */
  isActive: boolean
  /** The playlist item data */
  item: PlaylistItemPayload
  /** Handler when item is clicked */
  onSelect: (index: number) => void
}

/**
 * Individual playlist item with thumbnail, title, subtitle, and active indicator.
 * Displays metadata-type-appropriate aspect ratio and styling.
 */
export function PlaylistItem({
  isActive,
  item,
  onSelect,
}: PlaylistItemProps): ReactNode {
  const aspectRatio = getAspectRatio(item.metadataType)
  const { height, width } = getThumbnailSize(item.metadataType)
  const subtitle = formatSubtitle(item)

  return (
    <button
      aria-label={`Play ${item.title}`}
      className={cn(
        `
          group flex w-full items-center gap-3 rounded-md p-2 text-left
          transition-colors
          hover:bg-accent
        `,
        isActive && 'bg-accent/50',
      )}
      onClick={() => {
        onSelect(item.index)
      }}
      type="button"
    >
      {/* Thumbnail */}
      <div
        className={cn(
          'relative h-16 shrink-0 overflow-hidden rounded',
          aspectRatio,
        )}
      >
        {item.thumbUri ? (
          <Image
            className="h-full w-full"
            height={height}
            imageUri={item.thumbUri}
            objectFit="cover"
            width={width}
          />
        ) : (
          <div
            className={`flex h-full w-full items-center justify-center bg-muted`}
          >
            <span className="text-xs text-muted-foreground">
              {item.index + 1}
            </span>
          </div>
        )}
        {/* Active indicator overlay */}
        {isActive && (
          <div
            className={`absolute inset-0 border-2 border-primary bg-primary/10`}
          />
        )}
      </div>

      {/* Text content */}
      <div className="flex min-w-0 flex-1 flex-col">
        <div
          className={cn(
            'truncate text-sm font-medium',
            isActive && 'text-primary',
          )}
        >
          {item.title}
        </div>
        {subtitle && (
          <div className="truncate text-xs text-muted-foreground">
            {subtitle}
          </div>
        )}
      </div>

      {/* Index number */}
      <div className="shrink-0 text-sm text-muted-foreground">
        {item.index + 1}
      </div>
    </button>
  )
}

/**
 * Format subtitle combining parent title and subtitle with separator.
 */
function formatSubtitle(item: PlaylistItemPayload): string {
  const parts: string[] = []
  if (item.parentTitle) parts.push(item.parentTitle)
  if (item.subtitle) parts.push(item.subtitle)
  return parts.join(' • ')
}

/**
 * Get aspect ratio class based on metadata type.
 * - Track/Album: 1:1 (square)
 * - Episode/Clip: 16:9 (wide)
 * - Movie/Show/Photo: 2:3 (poster)
 */
function getAspectRatio(metadataType: string): string {
  switch (metadataType) {
    case 'AlbumRelease':
    case 'AlbumReleaseGroup':
    case 'Track':
      return 'aspect-square'
    case 'Clip':
    case 'Episode':
    case 'Trailer':
      return 'aspect-video'
    case 'Movie':
    case 'Photo':
    case 'Show':
    default:
      return 'aspect-[2/3]'
  }
}

/**
 * Get thumbnail dimensions based on metadata type.
 * Matches the actual display size: h-16 (64px) with appropriate aspect ratio width.
 */
function getThumbnailSize(metadataType: string): {
  height: number
  width: number
} {
  const height = 64 // h-16 = 64px

  switch (metadataType) {
    case 'AlbumRelease':
    case 'AlbumReleaseGroup':
    case 'Track':
      // aspect-square (1:1)
      return { height, width: 64 }
    case 'Clip':
    case 'Episode':
    case 'Trailer':
      // aspect-video (16:9)
      return { height, width: Math.round((height * 16) / 9) }
    case 'Movie':
    case 'Photo':
    case 'Show':
    default:
      // aspect-[2/3]
      return { height, width: Math.round((height * 2) / 3) }
  }
}
