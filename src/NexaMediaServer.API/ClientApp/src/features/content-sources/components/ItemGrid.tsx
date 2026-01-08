import { useElementScrollRestoration } from '@tanstack/react-router'
import { useVirtualizer } from '@tanstack/react-virtual'
import { useMeasure, useWindowSize } from '@uidotdev/usehooks'
import { useAtomValue } from 'jotai'
import {
  forwardRef,
  memo,
  useCallback,
  useEffect,
  useImperativeHandle,
  useMemo,
  useRef,
} from 'react'

import {
  getItemCardWidthPx,
  ITEM_CARD_MAX_WIDTH_PX,
} from '@/features/content-sources/lib/itemCardSizing'
import { PAGE_SIZE } from '@/features/content-sources/queries'
import { MetadataType } from '@/shared/api/graphql/graphql'
import { isUnloadedItem } from '@/shared/lib/sparseArray'
import { itemCardWidthAtom } from '@/store'

import { ItemCard } from './ItemCard'

// Type guard to check if an item is loaded (not a placeholder)
const isLoadedItem = (
  item: ItemGridItem | null | undefined,
): item is ItemGridItem => {
  return item !== null && item !== undefined
}

export type HeightMode = 'auto' | 'fit' | 'full'

export interface ItemGridHandle {
  scrollToIndex: (itemIndex: number) => void
  scrollToTop: () => void
}

export interface ItemGridItem {
  id: string
  isPromoted: boolean
  length: number
  librarySectionId: string
  metadataType: MetadataType
  persons: {
    id: string
    metadataType: MetadataType
    title: string
  }[]
  primaryPerson?: null | {
    id: string
    metadataType: MetadataType
    title: string
  }
  thumbHash?: null | string
  thumbUri?: null | string
  title: string
  viewCount: number
  viewOffset: number
  year: number
}

export interface ItemGridProps {
  enableScroll?: boolean // Enable vertical scrolling (default: true)
  gap?: number // px
  hasMore: boolean
  heightMode?: HeightMode // Height behavior: 'full' (default), 'auto', or 'fit'
  isFetching?: boolean
  items: (ItemGridItem | null | undefined)[]
  librarySectionId: string
  onRequestRange?: (startIndex: number) => void // Request data starting at index
  padding?: Padding // Container padding in pixels (default: { x: 32, y: 32 })
  prefetchRows?: number // how many rows ahead to trigger prefetch
  tileWidth?: number // px width when slider is at max token (52)
  totalCount: number
}

export interface Padding {
  x: number
  y: number
}

export const ItemGrid = memo(
  forwardRef<ItemGridHandle, ItemGridProps>(function ItemGrid(
    {
      enableScroll = true,
      gap = 24,
      hasMore,
      heightMode = 'full',
      isFetching = false,
      items,
      librarySectionId,
      onRequestRange,
      padding = { x: 32, y: 32 },
      prefetchRows = 3,
      tileWidth = ITEM_CARD_MAX_WIDTH_PX,
      totalCount,
    },
    ref,
  ) {
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
      const width = containerWidth ?? windowSize.width ?? globalThis.innerWidth
      const paddingX = padding.x * 2 // left and right padding from props
      const innerWidth = Math.max(0, width - paddingX)
      const per = cardWidthPx
      return Math.max(1, Math.floor((innerWidth + gap) / (per + gap)))
    }, [containerWidth, windowSize.width, cardWidthPx, gap, padding.x])

    // Keep a ref to columns for use in imperative handle
    const columnsRef = useRef(columns)
    useEffect(() => {
      columnsRef.current = columns
    }, [columns])

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

    // Expose scrollToIndex and scrollToTop via ref
    useImperativeHandle(
      ref,
      () => ({
        scrollToIndex: (itemIndex: number) => {
          // Use columnsRef to get current column count
          const currentColumns = columnsRef.current
          const rowIndex = Math.floor(itemIndex / currentColumns)
          rowVirtualizer.scrollToIndex(rowIndex, { align: 'start' })
        },
        scrollToTop: () => {
          scrollElementRef.current?.scrollTo({ behavior: 'smooth', top: 0 })
        },
      }),
      [rowVirtualizer],
    )

    // Track visible range for data loading
    const onRequestRangeRef = useRef(onRequestRange)
    useEffect(() => {
      onRequestRangeRef.current = onRequestRange
    }, [onRequestRange])

    // Track the last requested range to avoid duplicate requests
    const lastRequestedRangeRef = useRef<null | number>(null)

    // Use a scroll handler to check if we need more data
    // This avoids the infinite loop from depending on virtualizer state in an effect
    const checkDataNeeds = useCallback(() => {
      if (isFetching || !onRequestRangeRef.current) return

      const virtualRows = rowVirtualizer.getVirtualItems()
      if (virtualRows.length === 0) return

      const visibleStartIndex = virtualRows[0].index * columns
      const lastRow = virtualRows.at(-1)
      const visibleEndIndex = lastRow
        ? (lastRow.index + 1) * columns
        : visibleStartIndex

      // Find first unloaded item in visible range (including prefetch rows)
      const prefetchEndIndex = Math.min(
        visibleEndIndex + columns * prefetchRows,
        totalCount,
      )

      for (
        let i = visibleStartIndex;
        i < prefetchEndIndex && i < items.length;
        i++
      ) {
        if (isUnloadedItem(items[i])) {
          const requestStart = Math.max(
            0,
            Math.floor(i / PAGE_SIZE) * PAGE_SIZE,
          )
          if (lastRequestedRangeRef.current !== requestStart) {
            lastRequestedRangeRef.current = requestStart
            onRequestRangeRef.current(requestStart)
          }
          return
        }
      }

      // Also request next chunk if approaching end of loaded data and hasMore
      if (hasMore && prefetchEndIndex >= items.length) {
        const requestStart = items.length
        if (lastRequestedRangeRef.current !== requestStart) {
          lastRequestedRangeRef.current = requestStart
          onRequestRangeRef.current(requestStart)
        }
      }
    }, [
      columns,
      hasMore,
      isFetching,
      items,
      prefetchRows,
      rowVirtualizer,
      totalCount,
    ])

    // Stable ref callback for row measurement
    const rowRefCallback = useCallback(
      (el: HTMLDivElement | null) => {
        // Let virtualizer measure the element
        rowVirtualizer.measureElement(el)
      },
      [rowVirtualizer],
    )

    // Get virtual items for rendering
    const virtualRows = rowVirtualizer.getVirtualItems()
    const virtualColumns = columnVirtualizer.getVirtualItems()

    // Handle scroll to check if we need more data
    const handleScroll = useCallback(() => {
      checkDataNeeds()
    }, [checkDataNeeds])

    const heightClass =
      heightMode === 'full'
        ? 'h-full'
        : heightMode === 'fit'
          ? 'h-fit'
          : 'h-auto'
    const overflowClass = enableScroll ? 'overflow-y-auto' : 'overflow-y-hidden'

    return (
      <div
        className={`
          ${heightClass}
          w-full overflow-x-hidden
          ${overflowClass}
        `}
        data-scroll-restoration-id={scrollRestorationId}
        onScroll={handleScroll}
        ref={parentRef}
        style={{
          paddingBottom: `${padding.y.toString()}px`,
          paddingTop: `${padding.y.toString()}px`,
        }}
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
                className={`absolute top-0 left-0 grid w-full`}
                data-index={virtualRow.key}
                key={virtualRow.key}
                ref={rowRefCallback}
                style={{
                  gap: `${gap.toString()}px`,
                  gridTemplateColumns: `repeat(${columns.toString()}, ${cardWidthPx.toString()}px)`,
                  paddingLeft: `${padding.x.toString()}px`,
                  paddingRight: `${padding.x.toString()}px`,
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
                    // Calculate the absolute index in the total dataset
                    const absoluteIndex =
                      virtualRow.index * columns + virtualColumn.index

                    // With sparse arrays, items are at their correct indices
                    const rawItem =
                      absoluteIndex < items.length
                        ? items[absoluteIndex]
                        : undefined
                    const itemData = isLoadedItem(rawItem) ? rawItem : undefined
                    const cardKey =
                      itemData?.id ?? `placeholder-${absoluteIndex.toString()}`

                    if (!itemData) {
                      // Render a placeholder card for not-yet-loaded items
                      if (absoluteIndex < totalCount) {
                        // Find a loaded item to get the metadataType
                        const sampleItem = items.find(isLoadedItem)
                        return (
                          <ItemCard
                            cardWidthPx={cardWidthPx}
                            data-index={virtualColumn.key}
                            isPlaceholder
                            isScrolling={rowVirtualizer.isScrolling}
                            item={{
                              id: cardKey,
                              isPromoted: false,
                              length: 0,
                              librarySectionId: librarySectionId,
                              metadataType:
                                sampleItem?.metadataType ??
                                MetadataType.Unknown,
                              persons: [],
                              primaryPerson: null,
                              thumbHash: null,
                              thumbUri: undefined,
                              title: '',
                              viewCount: 0,
                              viewOffset: 0,
                              year: 0,
                            }}
                            key={cardKey}
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
  }),
)
