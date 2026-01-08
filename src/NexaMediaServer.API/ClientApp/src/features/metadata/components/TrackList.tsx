import type { Item } from '@/shared/api/graphql/graphql'

import { Separator } from '@/shared/components/ui/separator'

import { TrackRow } from './TrackRow'

interface MediumGroup {
  mediumId: string
  mediumIndex: number
  mediumTitle: string
  tracks: TrackItem[]
}

type PersonItem = Pick<Item, 'id' | 'metadataType' | 'title'>

type TrackItem = Pick<
  Item,
  | 'id'
  | 'index'
  | 'length'
  | 'librarySectionId'
  | 'metadataType'
  | 'parentId'
  | 'parentIndex'
  | 'parentTitle'
  | 'thumbUri'
  | 'title'
  | 'viewOffset'
> & {
  persons?: null | PersonItem[]
}

type TrackListProps = Readonly<{
  /** The album ID for playlist-based playback. When provided, playing a track plays the entire album. */
  albumId?: string
  tracks: TrackItem[]
}>

/**
 * Renders a list of tracks grouped by medium (disc) with visual separators.
 * Single-disc albums don't show the disc header.
 */
export function TrackList({ albumId, tracks }: TrackListProps) {
  if (tracks.length === 0) {
    return (
      <div className="py-4 text-center text-sm text-muted-foreground">
        No tracks available
      </div>
    )
  }

  const groups = groupTracksByMedium(tracks)
  const isMultiDisc = groups.length > 1

  return (
    <div className="flex flex-col">
      {groups.map((group, groupIndex) => (
        <div key={group.mediumId}>
          {/* Add separator between disc groups */}
          {isMultiDisc && groupIndex > 0 && <Separator className="my-4" />}

          {/* Disc header - only show for multi-disc albums */}
          {isMultiDisc && (
            <h3
              className={`
                px-4 py-2 text-sm font-semibold tracking-wide
                text-muted-foreground uppercase
              `}
            >
              {group.mediumTitle}
            </h3>
          )}

          {/* Track rows */}
          <div className="flex flex-col">
            {group.tracks.map((track) => (
              <TrackRow albumId={albumId} key={track.id} track={track} />
            ))}
          </div>
        </div>
      ))}
    </div>
  )
}

/**
 * Groups tracks by their parent medium (disc).
 */
function groupTracksByMedium(tracks: TrackItem[]): MediumGroup[] {
  const groups = new Map<string, MediumGroup>()

  for (const track of tracks) {
    const mediumId = track.parentId
    const existingGroup = groups.get(mediumId)
    if (existingGroup) {
      existingGroup.tracks.push(track)
    } else {
      groups.set(mediumId, {
        mediumId,
        mediumIndex: track.parentIndex,
        mediumTitle: track.parentTitle ?? `Disc ${String(track.parentIndex)}`,
        tracks: [track],
      })
    }
  }

  // Sort groups by medium index
  return Array.from(groups.values()).sort(
    (a, b) => a.mediumIndex - b.mediumIndex,
  )
}
