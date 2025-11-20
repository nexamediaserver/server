import { useQuery } from '@apollo/client/react'
import { useCallback, useMemo, useRef, useState } from 'react'

import {
  LibrarySectionChildrenQuery,
  LibrarySectionLetterIndexQuery,
  PAGE_SIZE,
} from '@/features/content-sources/queries'
import { MetadataType } from '@/shared/api/graphql/graphql'
import { QueryErrorDisplay } from '@/shared/components/QueryErrorDisplay'
import { isUnloadedItem } from '@/shared/lib/sparseArray'

import { ItemGrid, type ItemGridHandle } from './ItemGrid'
import { JumpBar } from './JumpBar'
type BrowseViewProps = Readonly<{
  contentSourceId: string
}>

export function BrowseView({ contentSourceId }: BrowseViewProps) {
  const itemGridRef = useRef<ItemGridHandle>(null)
  const [activeLetter, setActiveLetter] = useState<string | undefined>()
  // Track which ranges we've requested to avoid duplicates
  const requestedRangesRef = useRef<Set<number>>(new Set())

  // Fetch letter index for jump bar
  const { data: letterIndexData } = useQuery(LibrarySectionLetterIndexQuery, {
    skip: !contentSourceId,
    variables: {
      contentSourceId,
      metadataType: MetadataType.Movie,
    },
  })

  const {
    data,
    error: librarySectionChildrenError,
    fetchMore,
    loading: fetching,
  } = useQuery(LibrarySectionChildrenQuery, {
    skip: !contentSourceId,
    variables: {
      contentSourceId,
      metadataType: MetadataType.Movie,
      skip: 0,
      take: PAGE_SIZE,
    },
  })

  // Helper to check if we have loaded data at a given index
  const hasDataAtIndex = useCallback(
    (index: number) => {
      const items = data?.librarySection?.children?.items
      if (!items || index >= items.length) return false
      return !isUnloadedItem(items[index])
    },
    [data?.librarySection?.children?.items],
  )

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
          contentSourceId,
          metadataType: MetadataType.Movie,
          skip: pageStart,
          take: PAGE_SIZE,
        },
      })
    },
    [contentSourceId, fetchMore],
  )

  // Handle jump bar letter selection
  const handleLetterSelect = useCallback(
    (letter: string, offset: number) => {
      setActiveLetter(letter)

      // Calculate the page containing the target offset
      const targetPage = Math.floor(offset / PAGE_SIZE) * PAGE_SIZE

      // Fetch data for this offset if not already loaded
      if (!hasDataAtIndex(offset)) {
        fetchRange(offset)
      }

      // Also fetch the previous page so items before the jump target are visible
      const previousPage = targetPage - PAGE_SIZE
      if (previousPage >= 0 && !hasDataAtIndex(previousPage)) {
        fetchRange(previousPage)
      }

      // Scroll to the item at the given offset
      itemGridRef.current?.scrollToIndex(offset)
    },
    [fetchRange, hasDataAtIndex],
  )

  // Handle request for data at a specific range (triggered by ItemGrid when scrolling)
  const handleRequestRange = useCallback(
    (startIndex: number) => {
      fetchRange(startIndex)
    },
    [fetchRange],
  )

  // Memoize letter index for JumpBar
  const letterIndex = useMemo(
    () => letterIndexData?.librarySection?.letterIndex ?? [],
    [letterIndexData?.librarySection?.letterIndex],
  )

  if (librarySectionChildrenError) {
    return (
      <QueryErrorDisplay
        error={librarySectionChildrenError}
        title="Error loading items"
      />
    )
  }

  if (!data?.librarySection?.children?.items) {
    return null
  }

  return (
    <>
      <ItemGrid
        gap={24}
        hasMore={data.librarySection.children.pageInfo.hasNextPage}
        isFetching={fetching}
        items={data.librarySection.children.items}
        librarySectionId={data.librarySection.id}
        onRequestRange={handleRequestRange}
        ref={itemGridRef}
        tileWidth={208}
        totalCount={data.librarySection.children.totalCount}
      />
      {letterIndex.length > 0 && (
        <JumpBar
          activeLetter={activeLetter}
          letterIndex={letterIndex}
          onLetterSelect={handleLetterSelect}
        />
      )}
    </>
  )
}
