import { useQuery } from '@apollo/client/react'

import { ItemSlider } from '@/features/content-sources/components/ItemSlider'
import {
  HubContext,
  type HubDefinition,
  MetadataType,
} from '@/shared/api/graphql/graphql'

import { HubItemsQuery } from '../queries'

type HubItemRowProps = Readonly<{
  definition: Pick<
    HubDefinition,
    | 'contextId'
    | 'filterValue'
    | 'key'
    | 'librarySectionId'
    | 'metadataType'
    | 'title'
    | 'type'
  >
  librarySectionId?: string
  metadataItemId?: string
}>

/**
 * Renders a hub row with items fetched based on the hub definition.
 * Hides itself if no items are available.
 */
export function HubItemRow({
  definition,
  librarySectionId,
  metadataItemId,
}: HubItemRowProps) {
  const context = getHubContext(librarySectionId, metadataItemId)

  const { data, loading } = useQuery(HubItemsQuery, {
    variables: {
      input: {
        context,
        filterValue: definition.filterValue ?? null,
        hubType: definition.type,
        librarySectionId: librarySectionId ?? definition.librarySectionId,
        metadataItemId: metadataItemId ?? definition.contextId,
      },
    },
  })

  // Don't render anything while loading or if no items
  if (loading || !data?.hubItems.length) {
    return null
  }

  const items = data.hubItems

  // Map hub items to the format expected by ItemSlider
  const sliderItems = items.map((item) => ({
    id: item.id,
    length: item.length,
    librarySectionId: item.librarySectionId,
    metadataType: item.metadataType,
    thumbUri: item.thumbUri,
    title: item.title,
    viewOffset: item.viewOffset,
    year: item.year,
  }))

  return (
    <div className="min-w-0">
      <ItemSlider
        heading={definition.title}
        itemAspect={mapMetadataTypeToAspect(definition.metadataType)}
        items={sliderItems}
      />
    </div>
  )
}

function getHubContext(
  librarySectionId?: string,
  metadataItemId?: string,
): HubContext {
  if (metadataItemId) return HubContext.ItemDetail
  if (librarySectionId) return HubContext.LibraryDiscover
  return HubContext.Home
}

function mapMetadataTypeToAspect(
  metadataType: MetadataType,
): 'poster' | 'square' | 'wide' {
  switch (metadataType) {
    case MetadataType.AlbumRelease:
    case MetadataType.AlbumReleaseGroup:
    case MetadataType.Track:
      return 'square'
    case MetadataType.Clip:
    case MetadataType.Episode:
    case MetadataType.Trailer:
      return 'wide'
    case MetadataType.Movie:
    case MetadataType.Person:
    case MetadataType.Show:
    default:
      return 'poster'
  }
}
