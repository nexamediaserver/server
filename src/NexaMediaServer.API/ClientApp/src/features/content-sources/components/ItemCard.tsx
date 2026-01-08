import { Link } from '@tanstack/react-router'
import {
  type Key,
  type RefObject,
  useCallback,
  useEffect,
  useRef,
  useState,
} from 'react'
import IconPlay from '~icons/material-symbols/play-arrow'

import { ItemProgress, MetadataTypeIcon } from '@/domain/components'
import { ITEM_CARD_MAX_WIDTH_PX } from '@/features/content-sources/lib/itemCardSizing'
import { useStartPlayback } from '@/features/player'
import { MetadataType } from '@/shared/api/graphql/graphql'
import { Image } from '@/shared/components/Image'
import { Button } from '@/shared/components/ui/button'
import { cn } from '@/shared/lib/utils'

import { ItemActionsMenu } from './ItemActionsMenu'
import { type ItemGridItem } from './ItemGrid'
import { PlayedIndicator } from './PlayedIndicator'

type AspectClass = 'aspect-[2/3]' | 'aspect-square' | 'aspect-video'

interface CardContentProps {
  readonly canPlay: boolean
  readonly isDropdownOpen: boolean
  readonly isHovered: boolean
  readonly isScrolling: boolean
  readonly item: ItemCardProps['item']
  readonly onDropdownOpenChange: (open: boolean) => void
  readonly resolvedAspect: AspectClass
  readonly resolvedHeightPx: number
  readonly resolvedWidthPx: number
  readonly startPlayback: () => void
}

type ItemCardAspect = 'poster' | 'square' | 'wide'

type ItemCardProps = Readonly<{
  aspect?: ItemCardAspect
  cardWidthPx?: number
  'data-index'?: Key
  disableLink?: boolean
  isPlaceholder?: boolean
  isScrolling?: boolean
  item: ItemGridItem
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
  renderWidthPx,
}: ItemCardProps) {
  const resolvedAspect = aspectClassForMediaType(aspect, item.metadataType)
  const heightMultiplier = aspectHeightMultiplier(resolvedAspect)
  const widthPx = renderWidthPx ?? cardWidthPx
  const containerRef = useRef<HTMLDivElement | null>(null)
  const measuredWidthPx = useMeasuredWidth(containerRef, widthPx)
  const effectiveWidthPx = measuredWidthPx
  const resolvedHeightPx = Math.max(
    1,
    Math.round(effectiveWidthPx * heightMultiplier),
  )
  const { startPlaybackForItem } = useStartPlayback()
  const canPlay = item.metadataType !== MetadataType.Unknown

  // Track hover state manually for better control with dropdown portals
  const [isHovered, setIsHovered] = useState(false)
  const [isDropdownOpen, setIsDropdownOpen] = useState(false)

  // Keep overlay visible while dropdown is opening/open
  const handleDropdownOpenChange = useCallback((open: boolean) => {
    setIsDropdownOpen(open)
    // Only clear hover state after a brief delay to ensure smooth transition
    if (!open) {
      setTimeout(() => {
        setIsHovered(false)
      }, 100)
    }
  }, [])

  const handleMouseEnter = useCallback(() => {
    setIsHovered(true)
  }, [])
  const handleMouseLeave = useCallback(() => {
    if (!isDropdownOpen) {
      setIsHovered(false)
    }
  }, [isDropdownOpen])

  const subtitleText = getSubtitleText(item)
  const handlePlay = () => {
    if (!canPlay) {
      return
    }
    const playlistType = (() => {
      switch (item.metadataType) {
        case MetadataType.AlbumRelease:
        case MetadataType.AlbumReleaseGroup:
          return 'album'
        case MetadataType.PhotoAlbum:
        case MetadataType.PictureSet:
          return 'container'
        case MetadataType.Season:
          return 'season'
        case MetadataType.Show:
          return 'show'
        default:
          return 'single'
      }
    })()

    void startPlaybackForItem({
      item,
      originatorId:
        playlistType === 'single' ? undefined : (item.id as string | undefined),
      playlistType,
    })
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
      isDropdownOpen={isDropdownOpen}
      isHovered={isHovered}
      isScrolling={isScrolling}
      item={item}
      onDropdownOpenChange={handleDropdownOpenChange}
      resolvedAspect={resolvedAspect}
      resolvedHeightPx={resolvedHeightPx}
      resolvedWidthPx={effectiveWidthPx}
      startPlayback={handlePlay}
    />
  )

  const titleBlock = (
    <div className="h-12 py-1">
      <Link
        params={{
          contentSourceId: item.librarySectionId,
          metadataItemId: item.id,
        }}
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
    return (
      <div ref={containerRef} style={{ width: widthPx }}>
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
          onMouseEnter={handleMouseEnter}
          onMouseLeave={handleMouseLeave}
          type="button"
        >
          {media}
        </button>
        {titleBlock}
      </div>
    )
  }

  return (
    <div ref={containerRef} style={{ width: widthPx }}>
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
        onMouseEnter={handleMouseEnter}
        onMouseLeave={handleMouseLeave}
        params={{
          contentSourceId: item.librarySectionId,
          metadataItemId: item.id,
        }}
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
    case MetadataType.ShortForm:
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
      return 9 / 16 // 16:9 ratio height multiplier
    default:
      return 1.5
  }
}

function CardContent({
  canPlay,
  isDropdownOpen,
  isHovered,
  isScrolling,
  item,
  onDropdownOpenChange,
  resolvedAspect,
  resolvedHeightPx,
  resolvedWidthPx,
  startPlayback,
}: Readonly<CardContentProps>) {
  const hasThumb = Boolean(item.thumbUri)
  const shouldShowOverlay = !isScrolling && (isHovered || isDropdownOpen)
  const isPlayed = item.viewCount > 0 || item.viewOffset > 0

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
        <Image
          alt={item.title}
          className={cn('h-full w-full rounded-md', resolvedAspect)}
          height={resolvedHeightPx}
          imageUri={item.thumbUri ?? undefined}
          imgClassName="h-full w-full object-cover"
          thumbHash={item.thumbHash ?? undefined}
          width={resolvedWidthPx}
        />

        {isPlayed && <PlayedIndicator />}

        {!hasThumb && (
          <div
            aria-hidden="true"
            className={cn(
              'absolute inset-0 flex items-center justify-center bg-stone-800',
              'transition-opacity duration-300',
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

        {/* Overlay shown on hover/focus when not scrolling */}
        {shouldShowOverlay && (
          <div
            aria-hidden="true"
            className={cn(
              `
                pointer-events-none absolute inset-0 flex items-center
                justify-center rounded-md border-2 border-primary bg-black/40
                transition-opacity duration-200 ease-in-out
              `,
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
                if (canPlay) {
                  startPlayback()
                }
              }}
              size="icon"
              variant="default"
            >
              <IconPlay className="size-8" />
            </Button>
            <div
              className={cn(
                'pointer-events-auto absolute right-2 bottom-2 z-10 flex gap-2',
              )}
            >
              <ItemActionsMenu
                isPromoted={item.isPromoted}
                itemId={item.id}
                onOpenChange={onDropdownOpenChange}
              />
            </div>
          </div>
        )}

        <ItemProgress
          className="absolute right-0 bottom-0 left-0 h-1 rounded-b-md"
          length={item.length}
          viewOffset={item.viewOffset}
        />
      </div>
    </>
  )
}

function getSubtitleText(
  item: Pick<Item, 'metadataType' | 'primaryPerson' | 'year'>,
): string | undefined {
  // For music albums, show primary person/group
  if (
    (item.metadataType === MetadataType.AlbumRelease ||
      item.metadataType === MetadataType.AlbumReleaseGroup) &&
    item.primaryPerson
  ) {
    return item.primaryPerson.title
  }

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
    case MetadataType.ShortForm:
      return 'Short'
    default:
      return item.year > 0 ? String(item.year) : undefined
  }
}

// Observe the rendered card width so transcode requests match real display size.
function useMeasuredWidth(
  ref: RefObject<HTMLElement | null>,
  fallbackWidth: number,
) {
  const [measuredWidth, setMeasuredWidth] = useState(fallbackWidth)

  useEffect(() => {
    const node = ref.current
    if (!node) {
      return
    }

    const updateWidth = () => {
      const nextWidth = node.getBoundingClientRect().width
      setMeasuredWidth(Math.max(0, Math.round(nextWidth)))
    }

    updateWidth()

    if (typeof ResizeObserver === 'undefined') {
      return
    }

    const observer = new ResizeObserver((entries) => {
      if (entries.length === 0) {
        return
      }
      setMeasuredWidth(Math.max(0, Math.round(entries[0].contentRect.width)))
    })

    observer.observe(node)

    return () => {
      observer.disconnect()
    }
  }, [fallbackWidth, ref])

  return measuredWidth
}
