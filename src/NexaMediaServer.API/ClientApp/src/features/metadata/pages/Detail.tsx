import type { ReactNode } from 'react'

import { useQuery } from '@apollo/client/react'
import { Link } from '@tanstack/react-router'
import { useDocumentTitle } from '@uidotdev/usehooks'
import { useEffect, useMemo, useState } from 'react'
import MsEdit from '~icons/material-symbols/edit'
import MsMore from '~icons/material-symbols/more-horiz'
import MsPlay from '~icons/material-symbols/play-arrow'

import { metadataItemDetailRoute } from '@/features/metadata/routes'
import { usePlayback } from '@/features/player'
import { graphql } from '@/shared/api/graphql'
import { Button } from '@/shared/components/ui/button'
import { useLayoutSlot } from '@/shared/hooks'
import { getImageTranscodeUrl } from '@/shared/lib/images'

const MetadataItemQuery = graphql(`
  query Media($id: ID!) {
    metadataItem(id: $id) {
      id
      title
      originalTitle
      thumbUri
      metadataType
      directPlayUrl
      trickplayUrl
    }
  }
`)

const LibrarySectionChildrenQuery = graphql(`
  query ContentSource($contentSourceId: ID!) {
    librarySection(id: $contentSourceId) {
      id
      name
    }
  }
`)

export function MetadataItemDetailPage(): ReactNode {
  const { contentSourceId, metadataItemId } =
    metadataItemDetailRoute.useParams()
  const { startPlayback } = usePlayback()

  const { data } = useQuery(MetadataItemQuery, {
    variables: { id: metadataItemId },
  })

  const { data: sectionData } = useQuery(LibrarySectionChildrenQuery, {
    variables: { contentSourceId },
  })

  useDocumentTitle(
    data?.metadataItem ? `${data.metadataItem.title} | Nexa` : 'Nexa',
  )

  const [imageUrl, setImageUrl] = useState<string>()

  useEffect(() => {
    if (data?.metadataItem?.thumbUri) {
      void getImageTranscodeUrl(data.metadataItem.thumbUri, {
        height: 384,
        quality: 90,
        width: 256,
      }).then(setImageUrl)
    }
  }, [data?.metadataItem?.thumbUri])

  // Memoize header content to prevent useLayoutSlot from recreating on every render
  const headerContent = useMemo(
    () => (
      <>
        <div className="flex flex-row items-center gap-2">
          <Link params={{ contentSourceId }} to="/section/$contentSourceId">
            <h1 className="text-base font-semibold">
              {sectionData?.librarySection?.name}
            </h1>
          </Link>
        </div>
      </>
    ),
    [contentSourceId, sectionData?.librarySection?.name],
  )

  useLayoutSlot('header', headerContent)

  if (!data?.metadataItem) {
    return <div>Metadata item not found</div>
  }

  return (
    <div className="flex flex-row gap-8 p-8">
      {imageUrl ? (
        <img
          alt={data.metadataItem.title}
          className="aspect-2/3 w-64 rounded object-cover"
          src={imageUrl}
        />
      ) : (
        <div className="aspect-2/3 w-64 rounded bg-gray-200" />
      )}
      <div className="flex flex-col gap-8">
        <h1 className="text-4xl font-bold">{data.metadataItem.title}</h1>
        <div className="flex flex-row gap-4">
          {data.metadataItem.directPlayUrl && (
            <Button
              onClick={() => {
                if (data.metadataItem?.directPlayUrl) {
                  startPlayback(
                    data.metadataItem.directPlayUrl,
                    data.metadataItem,
                  )
                }
              }}
            >
              <MsPlay />
              Play
            </Button>
          )}
          <Button variant="ghost">
            <MsEdit />
          </Button>
          <Button variant="ghost">
            <MsMore />
          </Button>
        </div>
      </div>
    </div>
  )
}
