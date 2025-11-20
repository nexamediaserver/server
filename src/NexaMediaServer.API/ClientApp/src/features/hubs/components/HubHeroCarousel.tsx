import { useQuery } from '@apollo/client/react'
import { useNavigate } from '@tanstack/react-router'
import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import IconInfo from '~icons/material-symbols/info'
import IconChevronLeft from '~icons/material-symbols/keyboard-arrow-left'
import IconChevronRight from '~icons/material-symbols/keyboard-arrow-right'
import IconPlay from '~icons/material-symbols/play-arrow'

import { useStartPlayback } from '@/features/player'
import {
  HubContext,
  type HubDefinition,
  MetadataType,
} from '@/shared/api/graphql/graphql'
import { Image } from '@/shared/components/Image'
import { Button } from '@/shared/components/ui/button'
import { cn } from '@/shared/lib/utils'

import { HubItemsQuery } from '../queries'

/** Auto-advance interval in milliseconds (6 seconds) */
const AUTO_ADVANCE_INTERVAL_MS = 6000

type HubHeroCarouselProps = Readonly<{
  definition: Pick<
    HubDefinition,
    'filterValue' | 'key' | 'librarySectionId' | 'title' | 'type'
  >
  librarySectionId?: string
  metadataItemId?: string
}>

/**
 * Netflix-style hero carousel for the Promoted hub.
 * Features:
 * - Full-bleed backdrop images with gradient overlay
 * - Logo, title, tagline, and summary text
 * - Play and Info buttons
 * - Pagination dots with click navigation
 * - Auto-advance every 6 seconds (desktop), pauses on hover
 * - Mobile: simplified single-item view with swipe navigation
 */
export function HubHeroCarousel({
  definition,
  librarySectionId,
  metadataItemId,
}: HubHeroCarouselProps) {
  const navigate = useNavigate()
  const { startPlaybackForItem, startPlaybackLoading } = useStartPlayback()

  const [currentIndex, setCurrentIndex] = useState(0)
  const [isPaused, setIsPaused] = useState(false)
  const [touchStart, setTouchStart] = useState<null | number>(null)
  const [touchEnd, setTouchEnd] = useState<null | number>(null)
  const [containerSize, setContainerSize] = useState<null | {
    height: number
    width: number
  }>(null)

  const autoAdvanceRef = useRef<null | ReturnType<typeof setInterval>>(null)
  const containerRef = useRef<HTMLDivElement>(null)

  const context = getHubContext(librarySectionId, metadataItemId)

  const { data, loading } = useQuery(HubItemsQuery, {
    variables: {
      input: {
        context,
        filterValue: definition.filterValue ?? null,
        hubType: definition.type,
        librarySectionId: librarySectionId ?? definition.librarySectionId,
        metadataItemId: metadataItemId ?? null,
      },
    },
  })

  const items = useMemo(() => data?.hubItems ?? [], [data?.hubItems])

  // Reset index if items change
  useEffect(() => {
    if (items.length > 0 && currentIndex >= items.length) {
      setCurrentIndex(0)
    }
  }, [items.length, currentIndex])

  // Measure container size
  useEffect(() => {
    const updateSize = () => {
      if (containerRef.current) {
        const { height, width } = containerRef.current.getBoundingClientRect()

        // Only update if we have valid dimensions
        if (height > 0 && width > 0) {
          // Use 2x dimensions for retina displays
          setContainerSize({
            height: Math.round(height),
            width: Math.round(width),
          })
        }
      }
    }

    // Use requestAnimationFrame to ensure DOM is painted
    requestAnimationFrame(() => {
      updateSize()
    })

    globalThis.addEventListener('resize', updateSize)

    return () => {
      globalThis.removeEventListener('resize', updateSize)
    }
  }, [items.length])

  // Auto-advance logic (desktop only)
  useEffect(() => {
    // Only auto-advance if we have multiple items and not paused
    if (items.length <= 1 || isPaused) {
      return
    }

    // Check if we're on mobile (simplified: use media query)
    const isMobile = globalThis.matchMedia('(max-width: 768px)').matches
    if (isMobile) {
      return
    }

    autoAdvanceRef.current = setInterval(() => {
      setCurrentIndex((prev) => (prev + 1) % items.length)
    }, AUTO_ADVANCE_INTERVAL_MS)

    return () => {
      if (autoAdvanceRef.current) {
        clearInterval(autoAdvanceRef.current)
      }
    }
  }, [items.length, isPaused])

  // Navigation handlers
  const goToPrevious = useCallback(() => {
    setCurrentIndex((prev) => (prev - 1 + items.length) % items.length)
  }, [items.length])

  const goToNext = useCallback(() => {
    setCurrentIndex((prev) => (prev + 1) % items.length)
  }, [items.length])

  const goToIndex = useCallback((index: number) => {
    setCurrentIndex(index)
  }, [])

  // Touch handlers for mobile swipe
  const handleTouchStart = useCallback((e: React.TouchEvent) => {
    setTouchEnd(null)
    setTouchStart(e.targetTouches[0].clientX)
  }, [])

  const handleTouchMove = useCallback((e: React.TouchEvent) => {
    setTouchEnd(e.targetTouches[0].clientX)
  }, [])

  const handleTouchEnd = useCallback(() => {
    if (!touchStart || !touchEnd) return

    const distance = touchStart - touchEnd
    const minSwipeDistance = 50

    if (Math.abs(distance) > minSwipeDistance) {
      if (distance > 0) {
        goToNext()
      } else {
        goToPrevious()
      }
    }

    setTouchStart(null)
    setTouchEnd(null)
  }, [touchStart, touchEnd, goToNext, goToPrevious])

  // Handle play button click
  const handlePlay = useCallback(async () => {
    const item = items[currentIndex]
    // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition
    if (!item) return

    await startPlaybackForItem({
      id: item.id,
      metadataType: item.metadataType,
      title: item.title,
    })
  }, [items, currentIndex, startPlaybackForItem])

  // Handle info button click
  const handleInfo = useCallback(() => {
    const item = items[currentIndex]
    // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition
    if (!item) return

    void navigate({
      params: {
        contentSourceId: item.librarySectionId,
        metadataItemId: item.id,
      },
      to: '/section/$contentSourceId/details/$metadataItemId',
    })
  }, [items, currentIndex, navigate])

  // Don't render anything while loading or if no items
  if (loading || items.length === 0) {
    return null
  }

  const currentItem = items[currentIndex]
  // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition
  if (!currentItem) return null

  // Use artUri if available, fallback to thumbUri
  const backdropUri = currentItem.artUri ?? currentItem.thumbUri
  const backdropHash = currentItem.artHash ?? currentItem.thumbHash

  // Check if item is playable (movies, episodes, etc.)
  const isPlayable =
    currentItem.metadataType === MetadataType.Movie ||
    currentItem.metadataType === MetadataType.Episode ||
    currentItem.metadataType === MetadataType.Clip

  return (
    <section
      aria-label={definition.title}
      aria-roledescription="carousel"
      className="group relative -mx-8 -mt-8 min-w-0"
      onMouseEnter={() => {
        setIsPaused(true)
      }}
      onMouseLeave={() => {
        setIsPaused(false)
      }}
      onTouchEnd={handleTouchEnd}
      onTouchMove={handleTouchMove}
      onTouchStart={handleTouchStart}
    >
      {/* Hero container with backdrop */}
      <div
        className={`
          relative aspect-21/9 w-full overflow-hidden
          md:aspect-[2.4/1]
        `}
        ref={containerRef}
      >
        {/* Backdrop image */}
        {backdropUri && (
          <div className="absolute inset-0">
            <Image
              className="h-full w-full object-cover"
              height={containerSize?.height ?? 800}
              imageUri={backdropUri}
              lazy={false}
              thumbHash={backdropHash ?? undefined}
              width={containerSize?.width ?? 1920}
            />
          </div>
        )}

        {/* Gradient overlays for text readability */}
        <div
          className={`
            absolute inset-0 bg-linear-to-t from-background via-background/60
            to-transparent
          `}
        />
        <div
          className={`
            absolute inset-0 bg-linear-to-r from-background/80 via-transparent
            to-transparent
          `}
        />

        {/* Content overlay */}
        <div
          className={`
            absolute inset-0 flex flex-col justify-end p-6
            md:p-12
          `}
        >
          <div className="max-w-2xl space-y-4">
            {/* Logo if available, otherwise title */}
            {currentItem.logoUri ? (
              <Image
                className={`
                  max-h-24 w-auto max-w-full object-contain
                  md:max-h-32
                `}
                height={128}
                imageUri={currentItem.logoUri}
                thumbHash={currentItem.logoHash ?? undefined}
                width={400}
              />
            ) : (
              <h2
                className={`
                  text-3xl font-bold text-white drop-shadow-lg
                  md:text-5xl
                `}
              >
                {currentItem.title}
              </h2>
            )}

            {/* Tagline */}
            {currentItem.tagline && (
              <p
                className={`
                  text-lg font-medium text-white/90 drop-shadow
                  md:text-xl
                `}
              >
                {currentItem.tagline}
              </p>
            )}

            {/* Metadata: year, content rating */}
            <div
              className={`flex items-center gap-3 text-sm text-muted-foreground`}
            >
              {Boolean(currentItem.year) && <span>{currentItem.year}</span>}
              {currentItem.contentRating && (
                <span
                  className={`
                    rounded border border-white/40 px-1.5 py-0.5 text-xs
                  `}
                >
                  {currentItem.contentRating}
                </span>
              )}
            </div>

            {/* Summary (truncated) */}
            {currentItem.summary && (
              <p
                className={`
                  line-clamp-2 text-sm text-white/80
                  md:line-clamp-3 md:text-base
                `}
              >
                {currentItem.summary}
              </p>
            )}

            {/* Action buttons */}
            <div className="flex gap-3 pt-2">
              {isPlayable && (
                <Button
                  className="gap-2"
                  disabled={startPlaybackLoading}
                  onClick={() => void handlePlay()}
                  size="lg"
                >
                  <IconPlay className="h-5 w-5" />
                  Play
                </Button>
              )}
              <Button
                className="gap-2"
                onClick={handleInfo}
                size="lg"
                variant="secondary"
              >
                <IconInfo className="h-5 w-5" />
                More Info
              </Button>
            </div>
          </div>
        </div>

        {/* Navigation arrows (desktop only, visible on hover) */}
        {items.length > 1 && (
          <>
            <button
              aria-label="Previous slide"
              className={cn(
                'absolute top-1/2 left-4 hidden -translate-y-1/2 rounded-full',
                'bg-black/50 p-2 text-white opacity-0 transition-opacity',
                `
                  group-hover:opacity-100
                  hover:bg-black/70
                  md:block
                `,
              )}
              onClick={goToPrevious}
              type="button"
            >
              <IconChevronLeft className="h-8 w-8" />
            </button>
            <button
              aria-label="Next slide"
              className={cn(
                'absolute top-1/2 right-4 hidden -translate-y-1/2 rounded-full',
                'bg-black/50 p-2 text-white opacity-0 transition-opacity',
                `
                  group-hover:opacity-100
                  hover:bg-black/70
                  md:block
                `,
              )}
              onClick={goToNext}
              type="button"
            >
              <IconChevronRight className="h-8 w-8" />
            </button>
          </>
        )}

        {/* Pagination dots */}
        {items.length > 1 && (
          <div
            className={`absolute bottom-4 left-1/2 flex -translate-x-1/2 gap-2`}
          >
            {items.map((item, index) => (
              <button
                aria-label={`Go to slide ${String(index + 1)}`}
                className={cn(
                  'h-2 w-2 rounded-full transition-all',
                  index === currentIndex
                    ? 'w-6 bg-white'
                    : `
                      bg-white/50
                      hover:bg-white/80
                    `,
                )}
                key={item.id}
                onClick={() => {
                  goToIndex(index)
                }}
                type="button"
              />
            ))}
          </div>
        )}
      </div>
    </section>
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
