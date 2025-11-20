import { Link } from '@tanstack/react-router'
import { useHover } from '@uidotdev/usehooks'
import { type Key, useEffect, useRef, useState } from 'react'
import IconPlay from '~icons/material-symbols/play-arrow'

import type { Item } from '@/shared/api/graphql/graphql'

import { ITEM_CARD_MAX_WIDTH_PX } from '@/features/content-sources/lib/itemCardSizing'
import { usePlayback } from '@/features/player'
import { MetadataType } from '@/shared/api/graphql/graphql'
import { MetadataTypeIcon } from '@/shared/components/MetadataTypeIcon'
import { Button } from '@/shared/components/ui/button'
import { getImageTranscodeUrl } from '@/shared/lib/images'
import { cn } from '@/shared/lib/utils'

type AspectClass = 'aspect-[2/3]' | 'aspect-square' | 'aspect-video'

interface CardContentProps {
  readonly canPlay: boolean
  readonly currentImage: string | undefined
  readonly isHovered: boolean
  readonly isScrolling: boolean
  readonly isTransitioning: boolean
  readonly item: ItemCardProps['item']
  readonly previousImage: string | undefined
  readonly resolvedAspect: AspectClass
  readonly resolvedHeightPx: number | undefined
  readonly startPlayback: (url: string, item: ItemCardProps['item']) => void
}

type ItemCardAspect = 'poster' | 'square' | 'wide'

type ItemCardProps = Readonly<{
  aspect?: ItemCardAspect
  cardWidthPx?: number
  'data-index'?: Key
  disableLink?: boolean
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
  renderWidthPx?: number
}>

export function ItemCard({
  aspect,
  cardWidthPx = ITEM_CARD_MAX_WIDTH_PX,
  'data-index': dataIndex,
  disableLink = false,
  isPlaceholder = false,
  isScrolling = false,
  item,
  librarySectionId,
  renderWidthPx,
}: ItemCardProps) {
  const resolvedAspect = aspectClassForMediaType(aspect, item.metadataType)
  const resolvedHeightPx =
    resolvedAspect === 'aspect-video'
      ? Math.round(cardWidthPx * 1.5)
      : undefined
  const widthPx = renderWidthPx ?? cardWidthPx
  const { startPlayback } = usePlayback()
  const canPlay = Boolean(item.directPlayUrl)

  // Track hover state for enhanced interactions
  const [hoverRef, isHovered] = useHover<HTMLElement>()

  // Use the transition hook to manage graceful image changes
  // Pass the raw thumbUri and let the hook handle transcoding
  const imageIdentity = `${item.id}:${item.thumbUri ?? 'none'}`
  const { currentImage, isTransitioning, previousImage } = useImageTransition(
    item.thumbUri ?? undefined,
    imageIdentity,
    {
      aspectClass: resolvedAspect,
      cardWidthPx,
    },
  )
  const subtitleText = getSubtitleText(item)
  const handlePlay = () => {
    if (canPlay && item.directPlayUrl) {
      startPlayback(item.directPlayUrl, item)
    }
  }

  if (isPlaceholder) {
    return (
      <div
        aria-hidden="true"
        className={cn(
          `group pointer-events-none block animate-pulse select-none`,
        )}
        style={{ width: widthPx }}
      >
        <div
          className={cn(
            'relative w-full overflow-hidden rounded-md',
            resolvedAspect,
          )}
          style={{ height: resolvedHeightPx }}
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

  const media = (
    <CardContent
      canPlay={canPlay}
      currentImage={currentImage}
      isHovered={isHovered}
      isScrolling={isScrolling}
      isTransitioning={isTransitioning}
      item={item}
      previousImage={previousImage}
      resolvedAspect={resolvedAspect}
      resolvedHeightPx={resolvedHeightPx}
      startPlayback={handlePlay}
    />
  )

  const titleBlock = (
    <div className="h-12 py-1">
      <Link
        params={{ contentSourceId: librarySectionId, metadataItemId: item.id }}
        to={`/section/$contentSourceId/details/$metadataItemId`}
      >
        <h3
          className={cn(
            `
              w-full truncate text-sm font-medium text-foreground
              hover:text-primary hover:underline
            `,
          )}
        >
          {item.title}
        </h3>
      </Link>
      {subtitleText && (
        <p className={cn(`mt-1 text-xs text-foreground/70`)}>{subtitleText}</p>
      )}
    </div>
  )

  if (disableLink) {
    if (canPlay) {
      return (
        <div style={{ width: widthPx }}>
          <button
            aria-label={item.title}
            className={cn(
              'group block w-full',
              'cursor-pointer',
              'focus:outline-none',
              'focus-visible:ring-2 focus-visible:ring-purple-500',
            )}
            data-index={dataIndex}
            onClick={handlePlay}
            ref={hoverRef}
            type="button"
          >
            {media}
          </button>
          {titleBlock}
        </div>
      )
    }

    return (
      <div style={{ width: widthPx }}>
        <div
          aria-label={item.title}
          className={cn('group block')}
          data-index={dataIndex}
          ref={hoverRef}
        >
          {media}
        </div>
        {titleBlock}
      </div>
    )
  }

  return (
    <div style={{ width: widthPx }}>
      <Link
        aria-label={item.title}
        className={cn(
          `
            group block
            focus:outline-none
            focus-visible:ring-2 focus-visible:ring-purple-500
          `,
        )}
        data-index={dataIndex}
        params={{ contentSourceId: librarySectionId, metadataItemId: item.id }}
        ref={hoverRef}
        to={`/section/$contentSourceId/details/$metadataItemId`}
      >
        {media}
      </Link>
      {titleBlock}
    </div>
  )
}

// Decide aspect ratio based on item media type.
// - Poster-like (2:3) for most video/book/comic types
// - Square (1:1) for music (Album/Track)
function aspectClassForMediaType(
  aspectOverride: ItemCardAspect | undefined,
  type?: MetadataType,
): AspectClass {
  if (aspectOverride === 'wide') {
    return 'aspect-video'
  }

  if (aspectOverride === 'square') {
    return 'aspect-square'
  }

  if (aspectOverride === 'poster') {
    return 'aspect-[2/3]'
  }

  switch (type) {
    case MetadataType.AlbumRelease:
    case MetadataType.AlbumReleaseGroup:
    case MetadataType.Track:
      return 'aspect-square'
    case MetadataType.BehindTheScenes:
    case MetadataType.Clip:
    case MetadataType.DeletedScene:
    case MetadataType.ExtraOther:
    case MetadataType.Featurette:
    case MetadataType.Interview:
    case MetadataType.Scene:
    case MetadataType.Short:
      return 'aspect-video'
    default:
      return 'aspect-[2/3]'
  }
}

function aspectHeightMultiplier(aspectClass: AspectClass) {
  switch (aspectClass) {
    case 'aspect-square':
      return 1
    case 'aspect-video':
      return 1.5
    default:
      return 1.5
  }
}

function CardContent({
  canPlay,
  currentImage,
  isHovered,
  isScrolling,
  isTransitioning,
  item,
  previousImage,
  resolvedAspect,
  resolvedHeightPx,
  startPlayback,
}: Readonly<CardContentProps>) {
  return (
    <>
      {/* Visual card (image placeholder) */}
      <div
        className={cn(
          'relative w-full overflow-hidden rounded-md',
          resolvedAspect,
        )}
        style={{ height: resolvedHeightPx }}
      >
        {/* Previous image/placeholder - shown during transition */}
        {previousImage && (
          <img
            alt=""
            aria-hidden="true"
            className={cn(
              'absolute inset-0 h-full w-full object-cover',
              'transition-opacity duration-300',
              isTransitioning ? 'opacity-100' : 'opacity-0',
              resolvedAspect,
            )}
            decoding="async"
            key={previousImage}
            loading="lazy"
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
              resolvedAspect,
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
              resolvedAspect,
            )}
            decoding="async"
            key={currentImage}
            loading="lazy"
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
              resolvedAspect,
            )}
          >
            <span
              className={cn(
                `
                  text-sm font-medium text-stone-600
                  dark:text-stone-300
                `,
              )}
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
              disabled={!canPlay}
              onClick={(event) => {
                event.preventDefault()
                event.stopPropagation()
                if (canPlay && item.directPlayUrl) {
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
      </div>
    </>
  )
}

function getSubtitleText(
  item: Pick<Item, 'metadataType' | 'year'>,
): string | undefined {
  switch (item.metadataType) {
    case MetadataType.BehindTheScenes:
      return 'Behind the Scenes'
    case MetadataType.ExtraOther:
      return 'Extra'
    case MetadataType.Featurette:
      return 'Featurette'
    case MetadataType.Interview:
      return 'Interview'
    case MetadataType.Movie:
      return item.year > 0 ? String(item.year) : undefined
    case MetadataType.Scene:
      return 'Scene'
    case MetadataType.Short:
      return 'Short'
    default:
      return item.year > 0 ? String(item.year) : undefined
  }
}

// Hook to manage graceful image transitions
function useImageTransition(
  rawThumbUri: string | undefined,
  identityKey: string,
  options?: {
    aspectClass?: AspectClass
    cardWidthPx?: number
  },
) {
  const aspectClass = options?.aspectClass ?? 'aspect-[2/3]'
  const targetWidth = options?.cardWidthPx ?? 208
  const heightMultiplier = aspectHeightMultiplier(aspectClass)
  const targetHeight = Math.round(targetWidth * heightMultiplier)

  const [currentImage, setCurrentImage] = useState<string | undefined>()
  const [previousImage, setPreviousImage] = useState<string | undefined>()
  const [isTransitioning, setIsTransitioning] = useState(false)
  const preloadImageRef = useRef<HTMLImageElement | null>(null)
  const currentImageRef = useRef<string | undefined>(undefined)
  const identityRef = useRef<string>('')
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

    const isSameIdentity = identityRef.current === identityKey
    identityRef.current = identityKey

    const finishTransition = () => {
      setIsTransitioning(false)
      clearPendingCleanup()
      cleanupTimeoutRef.current = setTimeout(() => {
        setPreviousImage(undefined)
      }, 300)
    }

    // Start transition - keep showing current image
    setIsTransitioning(true)
    setPreviousImage(isSameIdentity ? currentImageRef.current : undefined)

    if (!isSameIdentity) {
      setCurrentImage(undefined)
    }

    if (rawThumbUri) {
      const cancellationState = { cancelled: false }

      // Get the transcoded URL
      void (async () => {
        try {
          const transcodedUrl = await getImageTranscodeUrl(rawThumbUri, {
            height: targetHeight,
            width: targetWidth,
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
        if (preloadImageRef.current) {
          preloadImageRef.current.src = ''
        }
      }
    }

    // If new thumbUri is undefined, transition to placeholder
    setCurrentImage(undefined)
    finishTransition()

    return () => {
      clearPendingCleanup()
      resetPreloadHandlers()
      if (preloadImageRef.current) {
        preloadImageRef.current.src = ''
      }
    }
  }, [identityKey, rawThumbUri])

  return { currentImage, isTransitioning, previousImage }
}
