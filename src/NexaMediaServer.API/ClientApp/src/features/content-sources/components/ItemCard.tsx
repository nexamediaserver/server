import { Link } from '@tanstack/react-router'
import { useHover } from '@uidotdev/usehooks'
import { memo, useEffect, useRef, useState } from 'react'
import IconPlay from '~icons/material-symbols/play-arrow'

import type { Item } from '@/shared/api/graphql/graphql'

import { ITEM_CARD_MAX_WIDTH_PX } from '@/features/content-sources/lib/itemCardSizing'
import { usePlayback } from '@/features/player'
import { MetadataType } from '@/shared/api/graphql/graphql'
import { MetadataTypeIcon } from '@/shared/components/MetadataTypeIcon'
import { Button } from '@/shared/components/ui/button'
import { getImageTranscodeUrl } from '@/shared/lib/images'
import { cn } from '@/shared/lib/utils'

export const ItemCard = memo(function ItemCard({
  cardWidthPx = ITEM_CARD_MAX_WIDTH_PX,
  isPlaceholder = false,
  isScrolling = false,
  item,
  librarySectionId,
}: {
  cardWidthPx?: number
  isPlaceholder?: boolean
  isScrolling?: boolean
  item: Pick<
    Item,
    | 'directPlayUrl'
    | 'id'
    | 'metadataType'
    | 'thumbUri'
    | 'title'
    | 'trickplayUrl'
    | 'year'
  >
  librarySectionId: string
}) {
  const aspect = aspectClassForMediaType(item.metadataType)
  const { startPlayback } = usePlayback()

  // Track hover state for enhanced interactions
  const [hoverRef, isHovered] = useHover<HTMLAnchorElement>()

  // Use the transition hook to manage graceful image changes
  // Pass the raw thumbUri and let the hook handle transcoding
  const { currentImage, isTransitioning, previousImage } = useImageTransition(
    item.thumbUri ?? undefined,
  )

  if (isPlaceholder) {
    return (
      <div
        aria-hidden="true"
        className={cn(
          `group pointer-events-none block animate-pulse select-none`,
        )}
        style={{ width: cardWidthPx }}
      >
        <div
          className={cn('relative w-full overflow-hidden rounded-md', aspect)}
        >
          <div
            className={cn(
              'absolute inset-0 flex items-center justify-center',
              `dark:bg-stone-800`,
            )}
          />
        </div>
        <div className="h-10">
          <div
            className={cn(
              `h-4 w-3/4 rounded bg-stone-200`,
              `dark:bg-stone-700`,
            )}
          />
        </div>
      </div>
    )
  }

  return (
    <Link
      aria-label={item.title}
      className={cn(
        `
          group block
          focus:outline-none
          focus-visible:ring-2 focus-visible:ring-purple-500
        `,
      )}
      params={{ contentSourceId: librarySectionId, metadataItemId: item.id }}
      ref={hoverRef}
      style={{ width: cardWidthPx }}
      to={`/section/$contentSourceId/details/$metadataItemId`}
    >
      {/* Visual card (image placeholder) */}
      <div className={cn('relative w-full overflow-hidden rounded-md', aspect)}>
        {/* Previous image/placeholder - shown during transition */}
        {previousImage && (
          <img
            alt=""
            aria-hidden="true"
            className={cn(
              'absolute inset-0 h-full w-full object-cover',
              'transition-opacity duration-300',
              isTransitioning ? 'opacity-100' : 'opacity-0',
              aspect,
            )}
            src={previousImage}
          />
        )}
        {isTransitioning && !previousImage && !currentImage && (
          <div
            aria-hidden="true"
            className={cn(
              'absolute inset-0 flex items-center justify-center',
              'transition-opacity duration-300',
              'opacity-100',
              `dark:bg-stone-800`,
            )}
          >
            <span
              className={`
                text-sm font-medium text-stone-600
                dark:text-stone-300
              `}
            >
              <MetadataTypeIcon className="text-4xl" item={item} />
            </span>
          </div>
        )}

        {/* Current image/placeholder */}
        {currentImage ? (
          <img
            alt={item.title}
            className={cn(
              'absolute inset-0 h-full w-full object-cover',
              'transition-opacity duration-300',
              isTransitioning && previousImage ? 'opacity-0' : 'opacity-100',
              aspect,
            )}
            src={currentImage}
          />
        ) : (
          <div
            aria-hidden="true"
            className={cn(
              'absolute inset-0 flex items-center justify-center',
              'transition-opacity duration-300',
              isTransitioning && previousImage ? 'opacity-0' : 'opacity-100',
              `dark:bg-stone-800`,
            )}
          >
            <span
              className={`
                text-sm font-medium text-stone-600
                dark:text-stone-300
              `}
            >
              <MetadataTypeIcon className="text-4xl" item={item} />
            </span>
          </div>
        )}

        {/* Overlay shown on hover/focus when not scrolling */}
        {!isScrolling && (
          <div
            aria-hidden="true"
            className={cn(
              `
                pointer-events-none absolute inset-0 flex items-center
                justify-center rounded-md bg-black/40 opacity-0
                transition-opacity duration-200 ease-in-out
              `,
              // Use isHovered state for more reliable hover detection
              isHovered && 'opacity-100',
              `
                group-hover:opacity-100
                group-focus-visible:opacity-100
              `,
            )}
          >
            <Button
              className={`
                pointer-events-auto size-12 cursor-pointer rounded-full
              `}
              onClick={(event) => {
                event.preventDefault()
                event.stopPropagation()
                if (item.directPlayUrl) {
                  startPlayback(item.directPlayUrl, item)
                }
              }}
              size="icon"
              variant="default"
            >
              <IconPlay className="size-8" />
            </Button>
          </div>
        )}

        {/* Enhanced hover overlay with additional info */}
        {isHovered && !isScrolling && item.year > 0 && (
          <div
            className={cn(
              'pointer-events-none absolute inset-x-0 bottom-0',
              'bg-linear-to-t from-black/80 to-transparent',
              'p-3 transition-opacity duration-200',
            )}
          >
            <p className="text-xs font-medium text-white">{item.year}</p>
          </div>
        )}
      </div>

      {/* Title below the card */}
      <div className="h-10">
        <h3
          className={cn(`w-full truncate text-sm font-medium text-foreground`)}
        >
          {item.title}
        </h3>
        {item.year > 0 && (
          <p className={cn(`mt-1 text-xs text-foreground/70`)}>{item.year}</p>
        )}
      </div>
    </Link>
  )
})

// Decide aspect ratio based on item media type.
// - Poster-like (2:3) for most video/book/comic types
// - Square (1:1) for music (Album/Track)
function aspectClassForMediaType(type?: MetadataType) {
  switch (type) {
    case MetadataType.AlbumRelease:
    case MetadataType.AlbumReleaseGroup:
    case MetadataType.Track:
      return 'aspect-square'
    default:
      return 'aspect-[2/3]'
  }
}

// Hook to manage graceful image transitions
function useImageTransition(rawThumbUri: string | undefined) {
  const [currentImage, setCurrentImage] = useState<string | undefined>()
  const [previousImage, setPreviousImage] = useState<string | undefined>()
  const [isTransitioning, setIsTransitioning] = useState(false)
  const preloadImageRef = useRef<HTMLImageElement | null>(null)
  const currentImageRef = useRef<string | undefined>(undefined)
  const cleanupTimeoutRef = useRef<null | ReturnType<typeof setTimeout>>(null)

  useEffect(() => {
    currentImageRef.current = currentImage
  }, [currentImage])

  useEffect(() => {
    const clearPendingCleanup = () => {
      if (cleanupTimeoutRef.current) {
        clearTimeout(cleanupTimeoutRef.current)
        cleanupTimeoutRef.current = null
      }
    }

    const resetPreloadHandlers = () => {
      if (preloadImageRef.current) {
        preloadImageRef.current.onload = null
        preloadImageRef.current.onerror = null
      }
    }

    clearPendingCleanup()

    const finishTransition = () => {
      setIsTransitioning(false)
      clearPendingCleanup()
      cleanupTimeoutRef.current = setTimeout(() => {
        setPreviousImage(undefined)
      }, 300)
    }

    // Start transition - keep showing current image
    setIsTransitioning(true)
    setPreviousImage(currentImageRef.current)

    if (rawThumbUri) {
      const cancellationState = { cancelled: false }

      // Get the transcoded URL
      void (async () => {
        try {
          const transcodedUrl = await getImageTranscodeUrl(rawThumbUri, {
            height: 312,
            width: 208,
          })

          if (cancellationState.cancelled) {
            return
          }

          // Preload the new image
          const img = new Image()
          preloadImageRef.current = img

          img.onload = () => {
            // Double-check we're still on the same thumbUri
            if (cancellationState.cancelled) {
              return
            }
            setCurrentImage(transcodedUrl)
            finishTransition()
          }

          img.onerror = () => {
            if (cancellationState.cancelled) {
              return
            }
            setCurrentImage(undefined)
            finishTransition()
          }

          img.src = transcodedUrl
        } catch {
          if (cancellationState.cancelled) {
            return
          }
          setCurrentImage(undefined)
          finishTransition()
        }
      })()

      return () => {
        cancellationState.cancelled = true
        clearPendingCleanup()
        resetPreloadHandlers()
      }
    }

    // If new thumbUri is undefined, transition to placeholder
    setCurrentImage(undefined)
    finishTransition()

    return () => {
      clearPendingCleanup()
      resetPreloadHandlers()
    }
  }, [rawThumbUri])

  return { currentImage, isTransitioning, previousImage }
}
