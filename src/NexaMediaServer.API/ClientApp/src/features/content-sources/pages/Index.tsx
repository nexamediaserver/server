import { useQuery } from '@apollo/client/react'
import { Link, useNavigate, useRouterState } from '@tanstack/react-router'
import { useDocumentTitle } from '@uidotdev/usehooks'
import { useMemo } from 'react'

import { LibrarySectionQuery } from '@/features/content-sources/queries'
import {
  contentSourceIndexRoute,
  type ContentSourceIndexSearch,
  type ContentSourceViewMode,
} from '@/features/content-sources/routes'
import { QueryErrorDisplay } from '@/shared/components/QueryErrorDisplay'
import { Tabs, TabsList, TabsTrigger } from '@/shared/components/ui/tabs'
import { useLayoutSlot } from '@/shared/hooks'

import { BrowseView } from '../components/BrowseView'
import { DiscoverView } from '../components/DiscoverView'
import { ItemCardScaleSlider } from '../components/ItemCardScaleSlider'

export function ContentSourceIndex() {
  const { contentSourceId } = contentSourceIndexRoute.useParams()
  const routerState = useRouterState()
  const rawSearch = routerState.location.search as ContentSourceIndexSearch
  const navigate = useNavigate({ from: '/section/$contentSourceId' })
  const viewMode: ContentSourceViewMode = rawSearch.view ?? 'discover'

  const {
    data: librarySectionData,
    error: librarySectionError,
    loading: loadingLibrarySection,
  } = useQuery(LibrarySectionQuery, {
    skip: !contentSourceId,
    variables: { contentSourceId },
  })

  useDocumentTitle(
    librarySectionData?.librarySection
      ? `${librarySectionData.librarySection.name} | Nexa`
      : 'Nexa',
  )

  // Memoize header content to prevent useLayoutSlot from recreating on every render
  const headerContent = useMemo(
    () => (
      <div className="grid w-full grid-cols-[1fr_auto_1fr] items-center">
        <div className="flex flex-row items-center gap-2">
          <Link params={{ contentSourceId }} to="/section/$contentSourceId">
            <h1 className="text-base font-semibold">
              {librarySectionData?.librarySection?.name}
            </h1>
          </Link>
        </div>
        <Tabs
          onValueChange={(value) => {
            void navigate({
              search: { view: value as ContentSourceViewMode },
            })
          }}
          value={viewMode}
        >
          <TabsList>
            <TabsTrigger value="discover">Discover</TabsTrigger>
            <TabsTrigger value="browse">Browse</TabsTrigger>
          </TabsList>
        </Tabs>
        <div className="flex items-center justify-end">
          {viewMode === 'browse' ? <ItemCardScaleSlider /> : null}
        </div>
      </div>
    ),
    [
      contentSourceId,
      librarySectionData?.librarySection?.name,
      navigate,
      viewMode,
    ],
  )
  const subHeaderContent = useMemo(
    () =>
      viewMode === 'browse' ? (
        <div className="text-sm text-muted-foreground">
          Browse all items in this content source.
        </div>
      ) : null,
    [viewMode],
  )

  useLayoutSlot('header', headerContent)
  useLayoutSlot('subheader', subHeaderContent)

  if (librarySectionError) {
    return (
      <QueryErrorDisplay
        error={librarySectionError}
        onRetry={() => {
          void navigate({ to: '/' })
        }}
        title="Error loading library"
      />
    )
  }

  if (!librarySectionData && !loadingLibrarySection) {
    void navigate({ to: '/' })
    return null
  }

  if (viewMode === 'discover') {
    return <DiscoverView librarySectionId={contentSourceId} />
  }

  return <BrowseView contentSourceId={contentSourceId} />
}
