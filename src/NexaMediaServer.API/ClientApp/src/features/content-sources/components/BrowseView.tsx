import { useQuery } from '@apollo/client/react'
import { useCallback, useMemo, useRef, useState } from 'react'

import { getRootMetadataType } from '@/features/content-sources/lib/libraryTypeMapping'
import {
  LibrarySectionBrowseOptionsQuery,
  LibrarySectionChildrenQuery,
  LibrarySectionLetterIndexQuery,
  PAGE_SIZE,
} from '@/features/content-sources/queries'
import {
  type ItemSortInput,
  LibraryType,
  MetadataType,
  SortEnumType,
} from '@/shared/api/graphql/graphql'
import { QueryErrorDisplay } from '@/shared/components/QueryErrorDisplay'
import { Badge } from '@/shared/components/ui/badge'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/shared/components/ui/select'
import { useLayoutSlot } from '@/shared/hooks/useLayout'
import { isUnloadedItem } from '@/shared/lib/sparseArray'

import { ItemGrid, type ItemGridHandle } from './ItemGrid'
import { JumpBar } from './JumpBar'

type BrowseViewProps = Readonly<{
  contentSourceId: string
  libraryType?: LibraryType
}>

export function BrowseView({ contentSourceId, libraryType }: BrowseViewProps) {
  const itemGridRef = useRef<ItemGridHandle>(null)
  const [activeLetter, setActiveLetter] = useState<string | undefined>()
  // Track which ranges we've requested to avoid duplicates
  const requestedRangesRef = useRef<Set<number>>(new Set())

  // State for selected browse options
  const [selectedItemTypeIndex, setSelectedItemTypeIndex] = useState(0)
  const [selectedSortKey, setSelectedSortKey] = useState('title')

  // Fetch available browse options
  const { data: browseOptionsData } = useQuery(
    LibrarySectionBrowseOptionsQuery,
    {
      skip: !contentSourceId,
      variables: { contentSourceId },
    },
  )

  const availableRootItemTypes = useMemo(
    () => browseOptionsData?.librarySection?.availableRootItemTypes ?? [],
    [browseOptionsData?.librarySection?.availableRootItemTypes],
  )

  const availableSortFields = useMemo(
    () => browseOptionsData?.librarySection?.availableSortFields ?? [],
    [browseOptionsData?.librarySection?.availableSortFields],
  )

  // Get the selected metadata types
  const selectedMetadataTypes = useMemo(() => {
    if (
      availableRootItemTypes.length > 0 &&
      selectedItemTypeIndex < availableRootItemTypes.length
    ) {
      return availableRootItemTypes[selectedItemTypeIndex].metadataTypes
    }
    // Fallback to default based on library type
    if (!libraryType) {
      return [MetadataType.Movie]
    }
    return [getRootMetadataType(libraryType)]
  }, [availableRootItemTypes, selectedItemTypeIndex, libraryType])

  // Build the sort input based on selected sort key
  const sortInput = useMemo((): ItemSortInput => {
    switch (selectedSortKey) {
      case 'contentRating':
        return { contentRating: SortEnumType.Asc }
      case 'dateAdded':
        return { dateAdded: SortEnumType.Desc }
      case 'duration':
        return { duration: SortEnumType.Asc }
      case 'index':
        return { index: SortEnumType.Asc }
      case 'releaseDate':
        return { releaseDate: SortEnumType.Desc }
      case 'title':
        return { title: SortEnumType.Asc }
      case 'year':
        return { year: SortEnumType.Desc }
      default:
        return { title: SortEnumType.Asc }
    }
  }, [selectedSortKey])

  // Show letter index only when sorting by title
  const showLetterIndex = selectedSortKey === 'title'

  // Reset pagination when selection changes
  const handleItemTypeChange = useCallback((value: string) => {
    setSelectedItemTypeIndex(Number(value))
    requestedRangesRef.current.clear()
    setActiveLetter(undefined)
  }, [])

  const handleSortChange = useCallback((value: string) => {
    setSelectedSortKey(value)
    requestedRangesRef.current.clear()
    setActiveLetter(undefined)
  }, [])

  // Fetch letter index for jump bar (only when sorting by title)
  const { data: letterIndexData } = useQuery(LibrarySectionLetterIndexQuery, {
    skip:
      !contentSourceId ||
      !showLetterIndex ||
      selectedMetadataTypes.length === 0,
    variables: {
      contentSourceId,
      metadataTypes: selectedMetadataTypes,
    },
  })

  const {
    data,
    error: librarySectionChildrenError,
    fetchMore,
    loading: fetching,
  } = useQuery(LibrarySectionChildrenQuery, {
    skip: !contentSourceId || selectedMetadataTypes.length === 0,
    variables: {
      contentSourceId,
      metadataTypes: selectedMetadataTypes,
      order: [sortInput],
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
          metadataTypes: selectedMetadataTypes,
          order: [sortInput],
          skip: pageStart,
          take: PAGE_SIZE,
        },
      })
    },
    [contentSourceId, fetchMore, selectedMetadataTypes, sortInput],
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

  // Extract totalCount for cleaner dependency tracking
  const totalCount = data?.librarySection?.children?.totalCount

  const subHeaderContent = useMemo(
    () => (
      <div className="flex items-center gap-4">
        {/* Item type selector - only show if more than one option */}
        {availableRootItemTypes.length > 1 && (
          <Select
            onValueChange={handleItemTypeChange}
            value={String(selectedItemTypeIndex)}
          >
            <SelectTrigger className="w-[140px]">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {availableRootItemTypes.map((itemType, index) => (
                <SelectItem key={index} value={String(index)}>
                  {itemType.displayName}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        )}

        {/* Sort field selector */}
        {availableSortFields.length > 0 && (
          <Select onValueChange={handleSortChange} value={selectedSortKey}>
            <SelectTrigger className="w-[160px]">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {availableSortFields.map((sortField) => (
                <SelectItem key={sortField.key} value={sortField.key}>
                  {sortField.displayName}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        )}

        {/* Item count badge */}
        {totalCount != null && <Badge variant="secondary">{totalCount}</Badge>}
      </div>
    ),
    [
      availableRootItemTypes,
      availableSortFields,
      handleItemTypeChange,
      handleSortChange,
      selectedItemTypeIndex,
      selectedSortKey,
      totalCount,
    ],
  )

  useLayoutSlot('subheader', subHeaderContent)

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
      {showLetterIndex && letterIndex.length > 0 && (
        <JumpBar
          activeLetter={activeLetter}
          letterIndex={letterIndex}
          onLetterSelect={handleLetterSelect}
        />
      )}
    </>
  )
}
