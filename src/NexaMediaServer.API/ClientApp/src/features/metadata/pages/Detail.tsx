import type { ReactNode } from 'react'

import { useQuery } from '@apollo/client/react'
import { Link, useNavigate } from '@tanstack/react-router'
import { useDocumentTitle, useHover } from '@uidotdev/usehooks'
import { useMemo, useState } from 'react'
import IconEdit from '~icons/material-symbols/edit'
import IconMore from '~icons/material-symbols/more-horiz'
import IconPlay from '~icons/material-symbols/play-arrow'

import { ItemProgress } from '@/domain/components'
import { useIsAdmin } from '@/features/auth'
import { ItemActionsMenu } from '@/features/content-sources/components/ItemActionsMenu'
import { HubRow, ItemDetailHubDefinitionsQuery } from '@/features/hubs'
import { EditMetadataItemDialog } from '@/features/metadata/components/EditMetadataItemDialog'
import { DetailFieldsSection } from '@/features/metadata/components/fields'
import {
  ContentSourceQuery,
  MetadataItemQuery,
} from '@/features/metadata/queries'
import { metadataItemDetailRoute } from '@/features/metadata/routes'
import { useStartPlayback } from '@/features/player'
import { MetadataType } from '@/shared/api/graphql/graphql'
import { Image } from '@/shared/components/Image'
import { QueryErrorDisplay } from '@/shared/components/QueryErrorDisplay'
import { Button } from '@/shared/components/ui/button'
import {
  useContainerSize,
  useErrorHandler,
  useLayoutSlot,
} from '@/shared/hooks'
import { cn } from '@/shared/lib/utils'

type DetailAspect = 'poster' | 'square' | 'wide'

interface DetailImageDimensions {
  aspect: DetailAspect
  aspectClass: string
  height: number
  width: number
}

export function MetadataItemDetailPage(): ReactNode {
  const { contentSourceId, metadataItemId } =
    metadataItemDetailRoute.useParams()
  const navigate = useNavigate({
    from: '/section/$contentSourceId/details/$metadataItemId',
  })
  const [hoverRef, isHovered] = useHover<HTMLElement>()
  const { startPlaybackForItem, startPlaybackLoading } = useStartPlayback()
  const [editDialogOpen, setEditDialogOpen] = useState(false)
  const { handleError } = useErrorHandler({ context: 'MetadataItemDetailPage' })
  const isAdmin = useIsAdmin()
  const { containerRef: mobileBackdropRef, containerSize: mobileBackdropSize } =
    useContainerSize()

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

  const imageDimensions = useMemo(
    () => getDetailImageDimensions(data?.metadataItem?.metadataType),
    [data?.metadataItem?.metadataType],
  )

  // Determine if this is a music item (uses square aspect, no backdrop)
  const isMusicItem = useMemo(() => {
    const type = data?.metadataItem?.metadataType
    return (
      type === MetadataType.AlbumRelease ||
      type === MetadataType.AlbumReleaseGroup ||
      type === MetadataType.Track
    )
  }, [data?.metadataItem?.metadataType])

  // Mobile backdrop: use artUri with thumbUri fallback (except for music which uses thumb)
  const mobileBackdropUri = useMemo(() => {
    if (isMusicItem) {
      return data?.metadataItem?.thumbUri
    }
    return data?.metadataItem?.artUri ?? data?.metadataItem?.thumbUri
  }, [data?.metadataItem?.artUri, data?.metadataItem?.thumbUri, isMusicItem])

  const mobileBackdropHash = useMemo(() => {
    if (isMusicItem) {
      return data?.metadataItem?.thumbHash
    }
    return data?.metadataItem?.artHash ?? data?.metadataItem?.thumbHash
  }, [data?.metadataItem?.artHash, data?.metadataItem?.thumbHash, isMusicItem])

  const handlePlay = async () => {
    if (!data?.metadataItem) {
      return
    }

    const item = data.metadataItem
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

    try {
      await startPlaybackForItem({
        item,
        originatorId: playlistType === 'single' ? undefined : item.id,
        playlistType,
      })
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
        flex h-full w-full flex-col gap-4 overflow-x-hidden overflow-y-auto p-0
        md:p-8
      `}
    >
      {/* Mobile Layout */}
      <div
        className={`
          flex flex-col
          md:hidden
        `}
      >
        {/* Mobile Backdrop/Hero Section */}
        <div
          className={cn(
            'relative w-full overflow-hidden',
            isMusicItem ? 'aspect-square' : 'aspect-video',
          )}
          ref={mobileBackdropRef}
        >
          <Image
            alt={data?.metadataItem?.title}
            className="absolute inset-0 h-full w-full"
            height={mobileBackdropSize?.height ?? 400}
            imageUri={mobileBackdropUri ?? undefined}
            imgClassName="h-full w-full object-cover"
            thumbHash={mobileBackdropHash ?? undefined}
            width={mobileBackdropSize?.width ?? 400}
          />
          {/* Gradient overlay */}
          <div
            className={`
              pointer-events-none absolute inset-0 bg-linear-to-t
              from-background via-background/60 to-transparent
            `}
          />
        </div>

        {/* Progress bar below gradient */}
        <ItemProgress
          className="h-1"
          length={data?.metadataItem?.length}
          viewOffset={data?.metadataItem?.viewOffset}
        />

        {/* Mobile Fields Section */}
        {data?.metadataItem && (
          <div
            className={`
              z-10 -mt-18 px-4
              md:mt-0 md:pt-4
            `}
          >
            <DetailFieldsSection
              contentSourceId={contentSourceId}
              metadataItem={data.metadataItem}
              onEditClick={() => {
                setEditDialogOpen(true)
              }}
              onPlayClick={() => void handlePlay()}
              playDisabled={startPlaybackLoading}
            />
          </div>
        )}
      </div>

      {/* Desktop Layout */}
      <div
        className={`
          hidden
          md:flex md:flex-row md:gap-8
        `}
      >
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
        {data?.metadataItem && (
          <DetailFieldsSection
            contentSourceId={contentSourceId}
            metadataItem={data.metadataItem}
            onEditClick={() => {
              setEditDialogOpen(true)
            }}
            onPlayClick={() => void handlePlay()}
            playDisabled={startPlaybackLoading}
          />
        )}
      </div>

      {/* Hub Rows (both layouts) */}
      {hubData?.itemDetailHubDefinitions &&
        hubData.itemDetailHubDefinitions.length > 0 && (
          <div
            className={`
              flex flex-col gap-4 px-4
              md:px-0
            `}
          >
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

      {isAdmin && (
        <EditMetadataItemDialog
          itemId={metadataItemId}
          onClose={() => {
            setEditDialogOpen(false)
          }}
          onUpdated={() => void refetch()}
          open={editDialogOpen}
        />
      )}
    </div>
  )
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
