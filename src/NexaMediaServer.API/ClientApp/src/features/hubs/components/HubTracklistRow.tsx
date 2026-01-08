import { useQuery } from '@apollo/client/react'

import { TrackList } from '@/features/metadata/components/TrackList'
import { HubContext, type HubDefinition } from '@/shared/api/graphql/graphql'

import { HubItemsQuery } from '../queries'

type HubTracklistRowProps = Readonly<{
  definition: Pick<
    HubDefinition,
    'contextId' | 'filterValue' | 'key' | 'librarySectionId' | 'title' | 'type'
  >
  librarySectionId?: string
  metadataItemId?: string
}>

/**
 * Renders a hub row displaying a tracklist for albums.
 * Fetches tracks via the Tracks hub type and displays them grouped by medium.
 */
export function HubTracklistRow({
  definition,
  librarySectionId,
  metadataItemId,
}: HubTracklistRowProps) {
  const { data, loading } = useQuery(HubItemsQuery, {
    variables: {
      input: {
        context: HubContext.ItemDetail,
        filterValue: definition.filterValue ?? null,
        hubType: definition.type,
        librarySectionId: librarySectionId ?? definition.librarySectionId,
        metadataItemId: metadataItemId ?? definition.contextId,
      },
    },
  })

  // Don't render anything while loading
  if (loading) {
    return (
      <div className="min-w-0">
        <h2 className="mb-4 text-xl font-semibold">{definition.title}</h2>
        <div className="animate-pulse space-y-2">
          {Array.from({ length: 5 }).map((_, i) => (
            <div
              className="h-10 rounded-md bg-muted"
              key={`skeleton-${i.toString()}`}
            />
          ))}
        </div>
      </div>
    )
  }

  // Don't render if no items
  if (!data?.hubItems.length) {
    return null
  }

  // Map hub items to track format expected by TrackList
  const tracks = data.hubItems.map((item) => ({
    id: item.id,
    index: item.index,
    length: item.length,
    librarySectionId: item.librarySectionId,
    metadataType: item.metadataType,
    parentId: item.parent?.id ?? '',
    parentIndex: item.parent?.index ?? 0,
    parentTitle: item.parent?.title ?? '',
    persons: item.persons,
    thumbUri: item.thumbUri,
    title: item.title,
    viewOffset: item.viewOffset,
  }))

  return (
    <div className="min-w-0">
      <h2 className="mb-4 text-xl font-semibold">{definition.title}</h2>
      <TrackList
        albumId={metadataItemId ?? definition.contextId}
        tracks={tracks}
      />
    </div>
  )
}
