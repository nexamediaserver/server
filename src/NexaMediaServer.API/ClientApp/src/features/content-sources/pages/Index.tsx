import { useQuery } from '@apollo/client/react'
import { Link, useNavigate, useRouterState } from '@tanstack/react-router'
import { useDocumentTitle } from '@uidotdev/usehooks'
import { useAtom } from 'jotai'
import { useCallback, useMemo } from 'react'

import { LibrarySectionQuery } from '@/features/content-sources/queries'
import {
  contentSourceIndexRoute,
  type ContentSourceIndexSearch,
  type ContentSourceViewMode,
} from '@/features/content-sources/routes'
import { libraryViewModesAtom } from '@/features/content-sources/store/atoms'
import { QueryErrorDisplay } from '@/shared/components/QueryErrorDisplay'
import { Tabs, TabsList, TabsTrigger } from '@/shared/components/ui/tabs'
import { useLayoutSlot } from '@/shared/hooks'

import { BrowseView } from '../components/BrowseView'
import { DiscoverView } from '../components/DiscoverView'
import { ItemCardScaleSlider } from '../components/ItemCardScaleSlider'
import { LibraryActionsMenu } from '../components/LibraryActionsMenu'

export function ContentSourceIndex() {
  const { contentSourceId } = contentSourceIndexRoute.useParams()
  const routerState = useRouterState()
  const rawSearch = routerState.location.search as ContentSourceIndexSearch
  const navigate = useNavigate({ from: '/section/$contentSourceId' })
  const [libraryViewModes, setLibraryViewModes] = useAtom(libraryViewModesAtom)

  // Derive view mode: URL param > localStorage preference > default
  const viewMode: ContentSourceViewMode =
    // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition -- False positive due to TypeScript not knowing an array index can be undefined
    rawSearch.view ?? libraryViewModes[contentSourceId] ?? 'discover'

  // Update both localStorage preference and URL when view changes
  const handleViewChange = useCallback(
    (value: ContentSourceViewMode) => {
      setLibraryViewModes((prev) => ({ ...prev, [contentSourceId]: value }))
      void navigate({
        search: { view: value },
      })
    },
    [contentSourceId, navigate, setLibraryViewModes],
  )

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
          {librarySectionData?.librarySection && (
            <LibraryActionsMenu
              librarySectionId={librarySectionData.librarySection.id}
            />
          )}
        </div>
        <Tabs
          onValueChange={(value) => {
            handleViewChange(value as ContentSourceViewMode)
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
    [contentSourceId, handleViewChange, librarySectionData, viewMode],
  )

  useLayoutSlot('header', headerContent)

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

  return (
    <BrowseView
      contentSourceId={contentSourceId}
      libraryType={librarySectionData?.librarySection?.type}
    />
  )
}
