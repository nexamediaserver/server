import type { ReactNode } from 'react'

import { useQuery } from '@apollo/client/react'
import { Link, useNavigate } from '@tanstack/react-router'
import { useDocumentTitle, useHover } from '@uidotdev/usehooks'
import { Duration } from 'luxon'
import { useMemo, useState } from 'react'
import IconEdit from '~icons/material-symbols/edit'
import IconMore from '~icons/material-symbols/more-horiz'
import IconPlay from '~icons/material-symbols/play-arrow'

import { ItemActionsMenu } from '@/features/content-sources/components/ItemActionsMenu'
import { HubRow, ItemDetailHubDefinitionsQuery } from '@/features/hubs'
import {
  ContentSourceQuery,
  MetadataItemQuery,
} from '@/features/metadata/queries'
import { metadataItemDetailRoute } from '@/features/metadata/routes'
import { useStartPlayback } from '@/features/player'
import { MetadataType } from '@/shared/api/graphql/graphql'
import { Image } from '@/shared/components/Image'
import { ItemProgress } from '@/shared/components/ItemProgress'
import { QueryErrorDisplay } from '@/shared/components/QueryErrorDisplay'
import { Button } from '@/shared/components/ui/button'
import { useErrorHandler, useLayoutSlot } from '@/shared/hooks'
import { cn } from '@/shared/lib/utils'

type DetailAspect = 'poster' | 'square' | 'wide'

interface DetailImageDimensions {
  aspect: DetailAspect
  aspectClass: string
  height: number
  width: number
}

/**
 * Get the aspect ratio and dimensions for the detail page image based on metadata type.
 * - 2:3 (poster): Movie, Show, Season, Person, Group
 * - Square (1:1): AlbumRelease, AlbumReleaseGroup, Track
 * - Wide (16:9): Episode, Clip, BehindTheScenes, Interview, Scene, ShortForm, etc.
 */
function getDetailImageDimensions(
  metadataType?: MetadataType,
): DetailImageDimensions {
  const baseWidth = 256

  switch (metadataType) {
    // Square aspect for music
    case MetadataType.AlbumRelease:
    case MetadataType.AlbumReleaseGroup:
    case MetadataType.Track:
      return {
        aspect: 'square',
        aspectClass: 'aspect-square',
        height: baseWidth,
        width: baseWidth,
      }

    // Wide aspect for episodes and extras
    case MetadataType.BehindTheScenes:
    case MetadataType.Clip:
    case MetadataType.DeletedScene:
    case MetadataType.Episode:
    case MetadataType.ExtraOther:
    case MetadataType.Featurette:
    case MetadataType.Interview:
    case MetadataType.Scene:
    case MetadataType.ShortForm:
    case MetadataType.Trailer:
      return {
        aspect: 'wide',
        aspectClass: 'aspect-video',
        height: Math.round(baseWidth * (9 / 16)),
        width: baseWidth,
      }

    // Poster aspect (2:3) for movies, shows, seasons, people, groups, and default
    case MetadataType.Group:
    case MetadataType.Movie:
    case MetadataType.Person:
    case MetadataType.Season:
    case MetadataType.Show:
    default:
      return {
        aspect: 'poster',
        aspectClass: 'aspect-[2/3]',
        height: Math.round(baseWidth * 1.5),
        width: baseWidth,
      }
  }
}

const formatRuntime = (lengthSeconds?: null | number): null | string => {
  if (!lengthSeconds || lengthSeconds <= 0) {
    return null
  }

  const locale = Intl.DateTimeFormat().resolvedOptions().locale
  const duration = Duration.fromObject({ seconds: lengthSeconds })
    .shiftTo('hours', 'minutes')
    .normalize()

  const hours = Math.trunc(duration.hours)
  const roundedMinutes = Math.round(duration.minutes)

  const totalHours = hours + Math.floor(roundedMinutes / 60)
  const minutes = roundedMinutes % 60

  const hourFormatter = new Intl.NumberFormat(locale, {
    maximumFractionDigits: 0,
    style: 'unit',
    unit: 'hour',
    unitDisplay: 'narrow',
  })
  const minuteFormatter = new Intl.NumberFormat(locale, {
    maximumFractionDigits: 0,
    style: 'unit',
    unit: 'minute',
    unitDisplay: 'narrow',
  })

  const parts: string[] = []
  if (totalHours > 0) {
    parts.push(hourFormatter.format(totalHours))
  }

  const displayMinutes =
    minutes > 0 || parts.length === 0 ? Math.max(1, minutes) : 0
  if (displayMinutes > 0) {
    parts.push(minuteFormatter.format(displayMinutes))
  }

  return parts.join(' ')
}

export function MetadataItemDetailPage(): ReactNode {
  const { contentSourceId, metadataItemId } =
    metadataItemDetailRoute.useParams()
  const navigate = useNavigate({
    from: '/section/$contentSourceId/details/$metadataItemId',
  })
  const [hoverRef, isHovered] = useHover<HTMLElement>()
  const { startPlaybackForItem, startPlaybackLoading } = useStartPlayback()
  const [showAllGenres, setShowAllGenres] = useState(false)
  const { handleError } = useErrorHandler({ context: 'MetadataItemDetailPage' })

  const { data, error, loading, refetch } = useQuery(MetadataItemQuery, {
    variables: { id: metadataItemId },
  })

  const { data: sectionData } = useQuery(ContentSourceQuery, {
    variables: { contentSourceId },
  })

  const { data: hubData } = useQuery(ItemDetailHubDefinitionsQuery, {
    skip: !metadataItemId,
    variables: { itemId: metadataItemId },
  })

  useDocumentTitle(
    data?.metadataItem ? `${data.metadataItem.title} | Nexa` : 'Nexa',
  )

  // Memoize header content to prevent useLayoutSlot from recreating on every render
  const headerContent = useMemo(
    () => (
      <div className="flex flex-row items-center gap-2">
        <Link params={{ contentSourceId }} to="/section/$contentSourceId">
          <h1 className="text-base font-semibold">
            {sectionData?.librarySection?.name}
          </h1>
        </Link>
      </div>
    ),
    [contentSourceId, sectionData?.librarySection?.name],
  )

  useLayoutSlot('header', headerContent)

  const releaseYear = data?.metadataItem?.year
  const displayYear =
    releaseYear != null && Number.isInteger(releaseYear) && releaseYear > 0
      ? releaseYear
      : null
  const runtime = useMemo(
    () => formatRuntime(data?.metadataItem?.length),
    [data?.metadataItem?.length],
  )

  const imageDimensions = useMemo(
    () => getDetailImageDimensions(data?.metadataItem?.metadataType),
    [data?.metadataItem?.metadataType],
  )

  const handlePlay = async () => {
    if (!data?.metadataItem) {
      return
    }

    try {
      await startPlaybackForItem(data.metadataItem)
    } catch (mutationError) {
      handleError(mutationError)
    }
  }

  if (error) {
    return (
      <QueryErrorDisplay
        error={error}
        onRetry={() => {
          void refetch()
        }}
        title="Error loading item"
      />
    )
  }

  if (!data?.metadataItem && !loading) {
    void navigate({
      params: { contentSourceId },
      to: '/section/$contentSourceId',
    })

    return null
  }

  return (
    <div
      className={`
        flex h-full w-full flex-col gap-4 overflow-x-hidden overflow-y-auto p-8
      `}
    >
      <div className="flex flex-row gap-8">
        <div
          className={`
            relative w-64 shrink-0 cursor-pointer overflow-hidden rounded-md
          `}
          ref={hoverRef}
        >
          <Image
            alt={data?.metadataItem?.title}
            className="w-64"
            height={imageDimensions.height}
            imageUri={data?.metadataItem?.thumbUri ?? undefined}
            imgClassName={cn(
              imageDimensions.aspectClass,
              'w-full object-cover',
            )}
            thumbHash={data?.metadataItem?.thumbHash ?? undefined}
            width={imageDimensions.width}
          />
          <ItemProgress
            className="absolute right-0 bottom-0 left-0 h-1 rounded-b-md"
            length={data?.metadataItem?.length}
            viewOffset={data?.metadataItem?.viewOffset}
          />
          <div
            aria-hidden="true"
            className={cn(
              `
                pointer-events-none absolute inset-0 flex items-center
                justify-center rounded-md border-2 border-primary bg-black/40
                opacity-0 transition-opacity duration-200 ease-in-out
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
              onClick={() => {
                void handlePlay()
              }}
              size="icon"
              variant="default"
            >
              <IconPlay className="size-8" />
            </Button>
          </div>
        </div>
        <div className="flex flex-col gap-6">
          <div className="flex flex-col">
            {data?.metadataItem?.originalTitle && (
              <h2 className="line-clamp-1 text-lg font-light">
                {data.metadataItem.originalTitle}
              </h2>
            )}
            <h1 className="line-clamp-1 text-4xl font-bold">
              {data?.metadataItem?.title}
            </h1>
          </div>
          <div className="flex flex-col gap-1">
            <div className="flex flex-row gap-4">
              {displayYear !== null && (
                <span className="text-sm text-muted-foreground">
                  {displayYear}
                </span>
              )}
              {runtime && (
                <span className="text-sm text-muted-foreground">{runtime}</span>
              )}
              {data?.metadataItem?.contentRating && (
                <span
                  className={`
                    rounded border border-white/40 px-1.5 py-0.5 text-xs
                    text-muted-foreground
                  `}
                >
                  {data.metadataItem.contentRating}
                </span>
              )}
            </div>
            {data?.metadataItem?.genres &&
              data.metadataItem.genres.length > 0 && (
                <div className="flex flex-row flex-wrap items-baseline">
                  {data.metadataItem.genres
                    .slice(
                      0,
                      showAllGenres || data.metadataItem.genres.length <= 3
                        ? undefined
                        : 2,
                    )
                    .map((genre, index, array) => (
                      <span key={genre}>
                        <Link params={{ contentSourceId, genre }}>{genre}</Link>
                        {index < array.length - 1 && ',\u00A0'}
                      </span>
                    ))}
                  {data.metadataItem.genres.length > 3 && (
                    <>
                      {!showAllGenres && (
                        <>
                          <span>,&nbsp;</span>
                          <button
                            className={`hover:underline`}
                            onClick={() => {
                              setShowAllGenres(!showAllGenres)
                            }}
                            type="button"
                          >
                            and more
                          </button>
                        </>
                      )}
                    </>
                  )}
                </div>
              )}
          </div>
          <div className="flex flex-row gap-4">
            {data?.metadataItem && (
              <Button
                disabled={startPlaybackLoading}
                onClick={() => {
                  void handlePlay()
                }}
              >
                <IconPlay />
                Play
              </Button>
            )}
            <Button variant="ghost">
              <IconEdit />
            </Button>
            <ItemActionsMenu
              isPromoted={data?.metadataItem?.isPromoted ?? false}
              itemId={data?.metadataItem?.id ?? ''}
              trigger={
                <Button variant="ghost">
                  <IconMore />
                </Button>
              }
            />
          </div>
        </div>
      </div>
      {hubData?.itemDetailHubDefinitions &&
        hubData.itemDetailHubDefinitions.length > 0 && (
          <div className="flex flex-col gap-4">
            {hubData.itemDetailHubDefinitions.map((definition) => (
              <HubRow
                definition={definition}
                key={definition.key}
                librarySectionId={contentSourceId}
                metadataItemId={metadataItemId}
              />
            ))}
          </div>
        )}
    </div>
  )
}
