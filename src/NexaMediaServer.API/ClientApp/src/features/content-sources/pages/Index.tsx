import { useQuery } from '@apollo/client/react'
import { Link, useNavigate } from '@tanstack/react-router'
import { useDocumentTitle } from '@uidotdev/usehooks'
import { useCallback, useMemo } from 'react'

import { contentSourceIndexRoute } from '@/features/content-sources/routes'
import { graphql } from '@/shared/api/graphql'
import { MetadataType } from '@/shared/api/graphql/graphql'
import { Badge } from '@/shared/components/ui/badge'
import { useLayoutSlot } from '@/shared/hooks'

import { ItemCardScaleSlider } from '../components/ItemCardScaleSlider'
import { ItemGrid } from '../components/ItemGrid'

const LibrarySectionQuery = graphql(`
  query LibrarySection($contentSourceId: ID!) {
    librarySection(id: $contentSourceId) {
      id
      name
      type
    }
  }
`)

const LibrarySectionChildrenQuery = graphql(`
  query LibrarySectionChildren(
    $contentSourceId: ID!
    $metadataType: MetadataType!
    $first: Int
    $after: String
    $last: Int
    $before: String
  ) {
    librarySection(id: $contentSourceId) {
      id
      children(
        metadataType: $metadataType
        first: $first
        after: $after
        last: $last
        before: $before
        order: { title: ASC }
      ) {
        nodes {
          id
          title
          year
          metadataType
          thumbUri
          directPlayUrl
          trickplayUrl
        }
        pageInfo {
          endCursor
          startCursor
          hasNextPage
          hasPreviousPage
        }
        totalCount
      }
    }
  }
`)

const PAGE_SIZE = 100

export function ContentSourceIndex() {
  const { contentSourceId } = contentSourceIndexRoute.useParams()
  const navigate = useNavigate({ from: '/section/$contentSourceId' })

  const {
    data: librarySectionData,
    error: librarySectionError,
    loading: loadingLibrarySection,
  } = useQuery(LibrarySectionQuery, {
    skip: !contentSourceId,
    variables: { contentSourceId },
  })

  const {
    data,
    error: librarySectionChildrenError,
    fetchMore,
    loading: fetching,
  } = useQuery(LibrarySectionChildrenQuery, {
    skip: !contentSourceId,
    variables: {
      contentSourceId,
      first: PAGE_SIZE,
      metadataType: MetadataType.Movie,
    },
  })

  useDocumentTitle(
    librarySectionData?.librarySection
      ? `${librarySectionData.librarySection.name} | Nexa`
      : 'Nexa',
  )

  const loadMore = useCallback(() => {
    if (!data?.librarySection?.children?.pageInfo.hasNextPage) {
      return
    }

    const endCursor = data.librarySection.children.pageInfo.endCursor

    void fetchMore({
      variables: {
        after: endCursor,
        contentSourceId,
        first: PAGE_SIZE,
      },
    })
  }, [contentSourceId, data?.librarySection?.children?.pageInfo, fetchMore])

  // Memoize header content to prevent useLayoutSlot from recreating on every render
  const headerContent = useMemo(
    () => (
      <>
        <div className="flex flex-row items-center gap-2">
          <Link params={{ contentSourceId }} to="/section/$contentSourceId">
            <h1 className="text-base font-semibold">
              {librarySectionData?.librarySection?.name}
            </h1>
          </Link>
          <Badge variant={'outline'}>
            {data?.librarySection?.children?.totalCount ?? 0}
          </Badge>
        </div>
        <ItemCardScaleSlider />
      </>
    ),
    [
      contentSourceId,
      librarySectionData?.librarySection?.name,
      data?.librarySection?.children?.totalCount,
    ],
  )
  const subHeaderContent = useMemo(
    () => (
      <div className="text-sm text-muted-foreground">
        Browse all items in this content source.
      </div>
    ),
    [],
  )

  useLayoutSlot('header', headerContent)
  useLayoutSlot('subheader', subHeaderContent)

  if ((!librarySectionData && !loadingLibrarySection) || librarySectionError) {
    if (librarySectionError) {
      console.error(librarySectionError)
    }

    void navigate({ to: '/' })

    return null
  }

  if (!data?.librarySection?.children?.nodes || librarySectionChildrenError) {
    return null
  }

  return (
    <ItemGrid
      gap={24}
      hasMore={data.librarySection.children.pageInfo.hasNextPage}
      isFetching={fetching}
      items={data.librarySection.children.nodes}
      librarySectionId={data.librarySection.id}
      onLoadMore={loadMore}
      tileWidth={208}
      totalCount={data.librarySection.children.totalCount}
    />
  )
}
