import { useElementScrollRestoration } from '@tanstack/react-router'
import { useVirtualizer } from '@tanstack/react-virtual'
import {
  useIntersectionObserver,
  useMeasure,
  useWindowSize,
} from '@uidotdev/usehooks'
import { useAtomValue } from 'jotai'
import { memo, useCallback, useEffect, useMemo, useRef } from 'react'

import {
  getItemCardWidthPx,
  ITEM_CARD_MAX_WIDTH_PX,
} from '@/features/content-sources/lib/itemCardSizing'
import { type Item, MetadataType } from '@/shared/api/graphql/graphql'
import { itemCardWidthAtom } from '@/store'

import { ItemCard } from './ItemCard'

export interface ItemGridProps {
  gap?: number // px
  hasMore: boolean
  isFetching?: boolean
  items: Pick<Item, 'id' | 'metadataType' | 'thumbUri' | 'title' | 'year'>[]
  librarySectionId: string
  onLoadMore?: () => void
  prefetchRows?: number // how many rows from the end to trigger load more
  tileWidth?: number // px width when slider is at max token (52)
  totalCount: number
}

export const ItemGrid = memo(function ItemGrid({
  gap = 24,
  hasMore,
  isFetching = false,
  items,
  librarySectionId,
  onLoadMore,
  prefetchRows = 3,
  tileWidth = ITEM_CARD_MAX_WIDTH_PX,
  totalCount,
}: ItemGridProps) {
  const scrollElementRef = useRef<HTMLDivElement>(null)
  const [measureRef, { width: containerWidth }] = useMeasure<HTMLDivElement>()
  const windowSize = useWindowSize()
  const scrollRestorationId = useMemo(
    () => `item-grid-${librarySectionId}`,
    [librarySectionId],
  )
  const scrollEntry = useElementScrollRestoration({
    id: scrollRestorationId,
  })

  // Use IntersectionObserver for infinite scroll trigger
  const [sentinelRef, entry] = useIntersectionObserver<HTMLDivElement>({
    root: null,
    rootMargin: '200px', // Trigger 200px before the sentinel is visible
    threshold: 0,
  })

  // Combine refs for scroll element and measurement
  // Use useCallback to stabilize the ref callback
  const parentRef = useCallback(
    (node: HTMLDivElement | null) => {
      scrollElementRef.current = node
      measureRef(node)
    },
    [measureRef],
  )

  // Read the global card width token and derive pixel width by scaling from the tileWidth baseline
  const widthToken = useAtomValue(itemCardWidthAtom) // e.g., 32..52
  const cardWidthPx = useMemo(() => {
    return Math.max(
      1,
      getItemCardWidthPx(widthToken, { maxWidthPx: tileWidth }),
    )
  }, [tileWidth, widthToken])

  // Estimate row height based on poster aspect (2:3) plus title (h-10 = 2.5rem ~ 40px)
  const estimatedRowHeight = useMemo(() => {
    const titlePx = 40
    return Math.round(cardWidthPx * 1.5 + titlePx)
  }, [cardWidthPx])

  // Calculate columns based on measured container width, window size, and tile width
  const columns = useMemo(() => {
    const width = containerWidth ?? windowSize.width ?? window.innerWidth
    const paddingX = 64 // px-8 left and right on the grid row container
    const innerWidth = Math.max(0, width - paddingX)
    const per = cardWidthPx
    return Math.max(1, Math.floor((innerWidth + gap) / (per + gap)))
  }, [containerWidth, windowSize.width, cardWidthPx, gap])

  const rowsLength = useMemo(() => {
    return Math.ceil(totalCount / columns)
  }, [totalCount, columns])

  const rowVirtualizer = useVirtualizer({
    count: rowsLength,
    estimateSize: () => estimatedRowHeight,
    gap: gap,
    getScrollElement: () => scrollElementRef.current,
    initialOffset: scrollEntry?.scrollY ?? 0,
    overscan: 5,
  })

  const columnVirtualizer = useVirtualizer({
    count: columns,
    estimateSize: () => cardWidthPx,
    gap: gap,
    getScrollElement: () => scrollElementRef.current,
    horizontal: true,
    overscan: 5,
  })

  // Trigger load more when sentinel becomes visible
  // Use a ref to store the onLoadMore callback to prevent effect from running when it changes
  const onLoadMoreRef = useRef(onLoadMore)
  useEffect(() => {
    onLoadMoreRef.current = onLoadMore
  }, [onLoadMore])

  useEffect(() => {
    if (entry?.isIntersecting && hasMore && !isFetching) {
      onLoadMoreRef.current?.()
    }
  }, [entry?.isIntersecting, hasMore, isFetching])

  // Stable ref callback for row measurement and sentinel attachment
  const loadedRows = Math.ceil(items.length / columns)
  const triggerRowIndex = Math.max(0, loadedRows - 1 - prefetchRows)

  const rowRefCallback = useCallback(
    (virtualRowIndex: number) => (el: HTMLDivElement | null) => {
      // Let virtualizer measure the element
      rowVirtualizer.measureElement(el)
      // Attach sentinel to the trigger row for intersection observation
      if (virtualRowIndex === triggerRowIndex) {
        sentinelRef(el)
      }
    },
    [rowVirtualizer, sentinelRef, triggerRowIndex],
  )

  // Get virtual items - these update frequently during scroll
  const virtualRows = rowVirtualizer.getVirtualItems()
  const virtualColumns = columnVirtualizer.getVirtualItems()

  return (
    <div
      className="h-full w-full overflow-x-hidden overflow-y-auto py-8"
      data-scroll-restoration-id={scrollRestorationId}
      ref={parentRef}
    >
      <div
        className="relative mx-auto"
        style={{
          height: rowVirtualizer.getTotalSize().toString() + 'px',
          minWidth: '100%',
        }}
      >
        {virtualRows.map((virtualRow) => {
          return (
            <div
              className={`absolute top-0 left-0 grid w-full px-8`}
              data-index={virtualRow.key}
              key={virtualRow.key}
              ref={rowRefCallback(virtualRow.index)}
              style={{
                gap: `${gap.toString()}px`,
                gridTemplateColumns: `repeat(${columns.toString()}, ${cardWidthPx.toString()}px)`,
                transform: `translateY(${virtualRow.start.toString()}px)`,
                willChange: 'transform',
              }}
            >
              {virtualColumns.map(
                (
                  virtualColumn: ReturnType<
                    typeof columnVirtualizer.getVirtualItems
                  >[number],
                ) => {
                  const flatIndex =
                    virtualRow.index * columns + virtualColumn.index
                  const itemData = items.at(flatIndex)
                  const cardKey =
                    itemData?.id ?? `placeholder-${flatIndex.toString()}`

                  if (!itemData) {
                    // Render a placeholder card for not-yet-loaded items to avoid flashing
                    if (flatIndex < totalCount) {
                      return (
                        <ItemCard
                          cardWidthPx={cardWidthPx}
                          data-index={virtualColumn.key}
                          isPlaceholder
                          isScrolling={rowVirtualizer.isScrolling}
                          item={{
                            id: cardKey,
                            metadataType:
                              items[0]?.metadataType ?? MetadataType.Unknown,
                            thumbUri: undefined,
                            title: '',
                            year: 0,
                          }}
                          key={cardKey}
                          librarySectionId={librarySectionId}
                        />
                      )
                    }
                    return null
                  }

                  return (
                    <ItemCard
                      cardWidthPx={cardWidthPx}
                      data-index={virtualColumn.key}
                      isScrolling={rowVirtualizer.isScrolling}
                      item={itemData}
                      key={cardKey}
                      librarySectionId={librarySectionId}
                    />
                  )
                },
              )}
            </div>
          )
        })}
      </div>
    </div>
  )
})
