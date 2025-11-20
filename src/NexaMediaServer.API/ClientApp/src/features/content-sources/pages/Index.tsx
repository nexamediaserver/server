import { useQuery } from '@apollo/client/react'
import { Link } from '@tanstack/react-router'
import { useDocumentTitle } from '@uidotdev/usehooks'
import { useCallback, useMemo } from 'react'

import { contentSourceIndexRoute } from '@/features/content-sources/routes'
import { graphql } from '@/shared/api/graphql'
import { Badge } from '@/shared/components/ui/badge'
import { useLayoutSlot } from '@/shared/hooks'

import { ItemCardScaleSlider } from '../components/ItemCardScaleSlider'
import { ItemGrid } from '../components/ItemGrid'

const LibrarySectionChildrenQuery = graphql(`
  query ContentSourceAll(
    $contentSourceId: ID!
    $first: Int
    $after: String
    $last: Int
    $before: String
  ) {
    librarySection(id: $contentSourceId) {
      id
      name
      children(
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

  const {
    data,
    fetchMore,
    loading: fetching,
  } = useQuery(LibrarySectionChildrenQuery, {
    skip: !contentSourceId,
    variables: { contentSourceId, first: PAGE_SIZE },
  })

  useDocumentTitle(
    data?.librarySection ? `${data.librarySection.name} | Nexa` : 'Nexa',
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
              {data?.librarySection?.name}
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
      data?.librarySection?.name,
      data?.librarySection?.children?.totalCount,
    ],
  )

  useLayoutSlot('header', headerContent)

  if (!data?.librarySection?.children?.nodes) {
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
