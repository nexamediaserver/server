import type { ReactNode } from 'react'

import { useVirtualizer } from '@tanstack/react-virtual'
import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import IconQueueMusic from '~icons/material-symbols/queue-music'

import type { PlaylistItemPayload } from '@/shared/api/graphql/graphql'

import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
} from '@/shared/components/ui/sheet'
import { useIsMobile } from '@/shared/hooks'
import { useKeyboardShortcuts } from '@/shared/hooks/useKeyboardShortcuts'
import { cn } from '@/shared/lib/utils'

import { usePlaylist } from '../../hooks/usePlaylist'
import { usePlaylistCache } from '../../hooks/usePlaylistCache'
import { usePlaylistNavigation } from '../../hooks/usePlaylistNavigation'
import { PlaylistItem } from './PlaylistItem'

export interface PlaylistDrawerProps {
  /** Callback when the drawer is closed */
  onOpenChange: (open: boolean) => void
  /** Whether the drawer is open */
  open: boolean
}

const PREFETCH_THRESHOLD = 20
const CHUNK_SIZE = 20
const ITEM_ESTIMATE_PX = 80
const OVERSCAN_COUNT = 3

/**
 * Responsive playlist drawer for navigating queued media items.
 * - Desktop: Right-side sheet
 * - Mobile: Bottom sheet with drag handle and safe area padding
 * - Features: Virtualization, keyboard navigation, auto-scroll to current item
 */
export function PlaylistDrawer({
  onOpenChange,
  open,
}: PlaylistDrawerProps): ReactNode {
  const isMobile = useIsMobile()
  const scrollContainerRef = useRef<HTMLDivElement>(null)
  const [keyboardSelectedIndex, setKeyboardSelectedIndex] = useState<
    null | number
  >(null)

  const { chunk, playlistGeneratorId, playlistIndex, refetch, totalCount } =
    usePlaylist({
      chunkSize: CHUNK_SIZE,
    })

  const { jumpTo } = usePlaylistNavigation()

  const { cachedItems, resetCache, updateCache } =
    usePlaylistCache(playlistGeneratorId)

  const chunkItems = useMemo(
    () =>
      chunk?.items.filter(
        (item): item is PlaylistItemPayload =>
          item !== null && item !== undefined,
      ) ?? [],
    [chunk?.items],
  )

  const lastRequestedStartRef = useRef<null | number>(null)
  const cachedItemsRef = useRef(cachedItems)
  const playlistGeneratorIdRef = useRef<null | string>(playlistGeneratorId)

  // Keep ref in sync for stable reads inside callbacks
  useEffect(() => {
    cachedItemsRef.current = cachedItems
  }, [cachedItems])

  // Update cache when chunk items actually change (not just reference)
  useEffect(() => {
    if (chunkItems.length === 0) return

    // Only update if items actually changed
    const hasNewItems = chunkItems.some(
      (item) => !cachedItemsRef.current.has(item.index),
    )
    if (!hasNewItems) return

    updateCache(chunkItems)
  }, [chunkItems, updateCache])

  // Track playlist generator ID changes
  useEffect(() => {
    const newId = playlistGeneratorId
    const previousId = playlistGeneratorIdRef.current

    if (previousId === newId) {
      return
    }

    playlistGeneratorIdRef.current = newId

    if (previousId && newId && previousId !== newId) {
      resetCache()
      cachedItemsRef.current = new Map()
      lastRequestedStartRef.current = null
    }
  }, [playlistGeneratorId, resetCache])

  // Virtualizer setup with stable callbacks
  const estimateSize = useCallback(() => ITEM_ESTIMATE_PX, [])
  const getScrollElement = useCallback(() => scrollContainerRef.current, [])
  const getItemKey = useCallback((index: number) => index, [])

  const rowVirtualizer = useVirtualizer({
    count: totalCount,
    estimateSize,
    getItemKey,
    getScrollElement,
    overscan: OVERSCAN_COUNT,
  })
  const rowVirtualizerRef = useRef(rowVirtualizer)

  // Keep virtualizer ref up to date
  useEffect(() => {
    rowVirtualizerRef.current = rowVirtualizer
  }, [rowVirtualizer])

  // Force virtualizer to recalculate when drawer opens
  // The Sheet animation can cause the scroll container to have invalid dimensions initially
  useEffect(() => {
    if (!open) return

    // Measure immediately and after animation completes
    rowVirtualizer.measure()

    const timeoutId = setTimeout(() => {
      rowVirtualizer.measure()
    }, 150) // Typical sheet animation duration

    return () => {
      clearTimeout(timeoutId)
    }
  }, [open, rowVirtualizer])

  // Auto-scroll to current item when drawer opens
  useEffect(() => {
    if (!open || playlistIndex < 0) return undefined

    const frame = requestAnimationFrame(() => {
      rowVirtualizerRef.current.scrollToIndex(playlistIndex, {
        align: 'center',
        behavior: 'auto',
      })
      setKeyboardSelectedIndex(null)
    })

    return () => {
      cancelAnimationFrame(frame)
    }
  }, [open, playlistIndex])

  // Prefetch chunks when scrolling - scan visible range for missing items
  const prefetchMissingItems = useCallback(() => {
    const playlistGeneratorId = playlistGeneratorIdRef.current
    if (!open || totalCount === 0 || !playlistGeneratorId) {
      console.log('[PlaylistDrawer] Prefetch skipped:', {
        open,
        playlistGeneratorId,
        totalCount,
      })
      return
    }

    const virtualItems = rowVirtualizerRef.current.getVirtualItems()
    if (virtualItems.length === 0) {
      console.log('[PlaylistDrawer] No virtual items')
      return
    }

    const firstVisible = virtualItems[0].index
    const lastVisible = virtualItems[virtualItems.length - 1].index

    // Scan visible range (including prefetch buffer) for missing items
    const scanStart = Math.max(0, firstVisible - PREFETCH_THRESHOLD)
    const scanEnd = Math.min(totalCount - 1, lastVisible + PREFETCH_THRESHOLD)

    console.log('[PlaylistDrawer] Scanning range:', {
      cacheSize: cachedItemsRef.current.size,
      firstVisible,
      lastVisible,
      scanEnd,
      scanStart,
    })

    let missingStart: null | number = null
    for (let i = scanStart; i <= scanEnd; i++) {
      if (!cachedItemsRef.current.has(i)) {
        missingStart = i
        break
      }
    }

    if (missingStart === null) {
      console.log('[PlaylistDrawer] No missing items in range')
      return
    }

    // Request the chunk that contains the first missing index
    const targetStart = Math.max(0, missingStart - (missingStart % CHUNK_SIZE))

    if (lastRequestedStartRef.current === targetStart) {
      console.log('[PlaylistDrawer] Already requested chunk at:', targetStart)
      return
    }
    lastRequestedStartRef.current = targetStart

    console.log('[PlaylistDrawer] Fetching chunk:', {
      limit: CHUNK_SIZE,
      missingStart,
      targetStart,
    })

    void refetch({
      input: {
        limit: CHUNK_SIZE,
        playlistGeneratorId,
        startIndex: targetStart,
      },
    })
  }, [open, refetch, totalCount])

  // Trigger prefetch when virtualizer range changes
  useEffect(() => {
    if (!open) return

    console.log(
      '[PlaylistDrawer] Virtualizer range changed, checking for missing items',
    )
    prefetchMissingItems()
  }, [open, prefetchMissingItems, rowVirtualizer.range])

  // Handle item selection
  const handleSelect = useCallback(
    (index: number) => {
      void jumpTo(index)
      onOpenChange(false) // Close drawer after jumping
    },
    [jumpTo, onOpenChange],
  )

  // Keyboard navigation
  const handleArrowDown = useCallback(() => {
    setKeyboardSelectedIndex((prev) => {
      const current = prev ?? playlistIndex
      const next = Math.min(current + 1, totalCount - 1)
      rowVirtualizerRef.current.scrollToIndex(next, { align: 'center' })
      return next
    })
  }, [playlistIndex, totalCount])

  const handleArrowUp = useCallback(() => {
    setKeyboardSelectedIndex((prev) => {
      const current = prev ?? playlistIndex
      const next = Math.max(current - 1, 0)
      rowVirtualizerRef.current.scrollToIndex(next, { align: 'center' })
      return next
    })
  }, [playlistIndex])

  const handleEnter = useCallback(() => {
    const selectedIndex = keyboardSelectedIndex ?? playlistIndex
    if (selectedIndex >= 0 && selectedIndex < totalCount) {
      handleSelect(selectedIndex)
    }
  }, [keyboardSelectedIndex, playlistIndex, totalCount, handleSelect])

  const handleEscape = useCallback(() => {
    onOpenChange(false)
  }, [onOpenChange])

  useKeyboardShortcuts(
    [
      {
        description: 'Select next item',
        handler: handleArrowDown,
        key: 'ArrowDown',
      },
      {
        description: 'Select previous item',
        handler: handleArrowUp,
        key: 'ArrowUp',
      },
      {
        description: 'Jump to selected item',
        handler: handleEnter,
        key: 'Enter',
      },
      {
        description: 'Close playlist',
        handler: handleEscape,
        key: 'Escape',
      },
    ],
    open,
  )

  // Reset keyboard selection when drawer closes
  const handleOpenChange = useCallback(
    (isOpen: boolean) => {
      if (!isOpen) {
        setKeyboardSelectedIndex(null)
      }
      onOpenChange(isOpen)
    },
    [onOpenChange],
  )

  return (
    <Sheet modal onOpenChange={handleOpenChange} open={open}>
      <SheetContent
        aria-describedby={undefined}
        className={cn(
          isMobile
            ? `
              flex max-h-[85vh] flex-col rounded-t-2xl
              pb-[max(1rem,env(safe-area-inset-bottom))]
            `
            : 'flex w-100 flex-col',
        )}
        side={isMobile ? 'bottom' : 'right'}
      >
        {/* Drag handle indicator (mobile only) */}
        {isMobile && (
          <div className="flex justify-center pt-3 pb-2">
            <div className="h-1 w-12 rounded-full bg-muted-foreground/30" />
          </div>
        )}

        <SheetHeader className={cn(isMobile ? 'px-4 pb-4' : 'pb-4')}>
          <SheetTitle>Playlist</SheetTitle>
          {totalCount > 0 && (
            <p className="text-sm text-muted-foreground">
              {playlistIndex + 1} of {totalCount}
            </p>
          )}
        </SheetHeader>

        {/* Scrollable content */}
        <div
          className={cn('flex-1 overflow-y-auto', isMobile ? 'px-4' : '')}
          ref={scrollContainerRef}
        >
          {totalCount === 0 ? (
            <div className="py-12 text-center text-muted-foreground">
              <IconQueueMusic className="mx-auto mb-4 size-12 opacity-50" />
              <p className="text-lg">No items in playlist</p>
              <p className="text-sm">Start playing media to build your queue</p>
            </div>
          ) : (
            <div
              className="relative w-full"
              style={{ height: `${String(rowVirtualizer.getTotalSize())}px` }}
            >
              {rowVirtualizer.getVirtualItems().map((virtualRow) => {
                const item = cachedItems.get(virtualRow.index)
                const isActive = virtualRow.index === playlistIndex
                const isKeyboardSelected =
                  virtualRow.index === keyboardSelectedIndex

                return (
                  <div
                    className={cn(
                      'absolute top-0 left-0 w-full',
                      isKeyboardSelected && 'ring-2 ring-primary ring-offset-2',
                    )}
                    key={virtualRow.key}
                    style={{
                      height: `${String(virtualRow.size)}px`,
                      transform: `translateY(${String(virtualRow.start)}px)`,
                    }}
                  >
                    {item ? (
                      <PlaylistItem
                        isActive={isActive}
                        item={item}
                        onSelect={handleSelect}
                      />
                    ) : (
                      <div className="flex h-full items-center gap-3 p-2">
                        <div
                          className={`
                            h-16 w-11 shrink-0 animate-pulse rounded bg-muted
                          `}
                        />
                        <div className="flex min-w-0 flex-1 flex-col gap-2">
                          <div
                            className={`
                              h-4 w-3/4 animate-pulse rounded bg-muted
                            `}
                          />
                          <div
                            className={`
                              h-3 w-1/2 animate-pulse rounded bg-muted
                            `}
                          />
                        </div>
                        <div
                          className={`
                            h-4 w-8 shrink-0 animate-pulse rounded bg-muted
                          `}
                        />
                      </div>
                    )}
                  </div>
                )
              })}
            </div>
          )}
        </div>
      </SheetContent>
    </Sheet>
  )
}
