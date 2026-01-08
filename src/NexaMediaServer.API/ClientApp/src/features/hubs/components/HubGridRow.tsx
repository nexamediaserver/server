import { useQuery } from '@apollo/client/react'
import { useCallback, useRef } from 'react'

import {
  ItemGrid,
  type ItemGridHandle,
} from '@/features/content-sources/components/ItemGrid'
import { PAGE_SIZE } from '@/features/content-sources/queries'
import { type HubDefinition } from '@/shared/api/graphql/graphql'

import { MetadataItemChildrenQuery } from '../queries'

type HubGridRowProps = Readonly<{
  definition: Pick<HubDefinition, 'contextId' | 'key' | 'title'>
  librarySectionId?: string
  metadataItemId?: string
}>

/**
 * Renders a hub row displaying a grid of child items (photos/pictures) with virtualization.
 * Fetches children of a metadata item (PhotoAlbum/PictureSet) with pagination support.
 */
export function HubGridRow({
  definition,
  librarySectionId,
  metadataItemId,
}: HubGridRowProps) {
  const itemGridRef = useRef<ItemGridHandle>(null)
  const requestedRangesRef = useRef<Set<number>>(new Set())

  const parentId = metadataItemId ?? definition.contextId ?? ''

  const { data, fetchMore, loading } = useQuery(MetadataItemChildrenQuery, {
    skip: !parentId,
    variables: {
      itemId: parentId,
      skip: 0,
      take: PAGE_SIZE,
    },
  })

  // Fetch data for a specific range
  const fetchRange = useCallback(
    (startIndex: number) => {
      // Round to page boundary
      const pageStart = Math.floor(startIndex / PAGE_SIZE) * PAGE_SIZE

      // Check if already requested
      if (requestedRangesRef.current.has(pageStart)) {
        return
      }
      requestedRangesRef.current.add(pageStart)

      void fetchMore({
        variables: {
          itemId: parentId,
          skip: pageStart,
          take: PAGE_SIZE,
        },
      })
    },
    [fetchMore, parentId],
  )

  // Handle request for data at a specific range (triggered by ItemGrid when scrolling)
  const handleRequestRange = useCallback(
    (startIndex: number) => {
      fetchRange(startIndex)
    },
    [fetchRange],
  )

  // Don't render anything while initially loading
  if (loading && !data?.metadataItem?.children?.items) {
    return (
      <div className="min-w-0">
        <h2 className="mb-4 text-xl font-semibold">{definition.title}</h2>
        <div
          className={`
            grid animate-pulse grid-cols-4 gap-4
            md:grid-cols-6
            lg:grid-cols-8
          `}
        >
          {Array.from({ length: 16 }).map((_, i) => (
            <div
              className="aspect-square rounded-md bg-muted"
              key={`skeleton-${i.toString()}`}
            />
          ))}
        </div>
      </div>
    )
  }

  // Don't render if no children data
  if (!data?.metadataItem?.children) {
    return null
  }

  const { items, pageInfo, totalCount } = data.metadataItem.children

  // Don't render if no items
  if (!items || items.length === 0 || totalCount === 0) {
    return null
  }

  // Use library section ID from parent item if not provided
  const sectionId = librarySectionId ?? data.metadataItem.librarySectionId

  return (
    <div className="flex min-w-0 flex-col gap-4">
      <h2 className="text-xl font-semibold">{definition.title}</h2>
      <ItemGrid
        enableScroll={false}
        gap={16}
        hasMore={pageInfo.hasNextPage}
        heightMode="auto"
        isFetching={loading}
        items={items}
        librarySectionId={sectionId}
        onRequestRange={handleRequestRange}
        padding={{ x: 0, y: 0 }}
        ref={itemGridRef}
        tileWidth={160}
        totalCount={totalCount}
      />
    </div>
  )
}
