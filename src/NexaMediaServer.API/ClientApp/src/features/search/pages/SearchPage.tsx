import { useQuery } from '@apollo/client/react'
import { Link, useNavigate, useRouterState } from '@tanstack/react-router'
import { useCallback, useEffect, useState } from 'react'
import IconSearch from '~icons/material-symbols/search'

import type { SearchQuery as SearchQueryType } from '@/shared/api/graphql/graphql'

import { searchQueryDocument } from '@/app/graphql/search'
import { type SearchRouteSearch } from '@/features/search/routes'
import { MetadataType, SearchPivot } from '@/shared/api/graphql/graphql'
import { Image } from '@/shared/components/Image'
import { MetadataTypeIcon } from '@/shared/components/MetadataTypeIcon'
import { Input } from '@/shared/components/ui/input'
import { Skeleton } from '@/shared/components/ui/skeleton'
import {
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
} from '@/shared/components/ui/tabs'
import { cn } from '@/shared/lib/utils'

interface SearchResultCardProps {
  readonly result: SearchResultData
}

type SearchResultData = SearchQueryType['search'][number]

interface SearchResultsGridProps {
  readonly loading?: boolean
  readonly results?: SearchResultData[]
}

const SEARCH_PAGE_LIMIT = 50

const pivotConfig = [
  { label: 'Top', value: SearchPivot.Top },
  { label: 'Movies', value: SearchPivot.Movie },
  { label: 'TV Shows', value: SearchPivot.Show },
  { label: 'Episodes', value: SearchPivot.Episode },
  { label: 'People', value: SearchPivot.People },
  { label: 'Albums', value: SearchPivot.Album },
  { label: 'Tracks', value: SearchPivot.Track },
] as const

export function SearchPage() {
  const navigate = useNavigate({ from: '/search' })
  const routerState = useRouterState()
  // Parse search params from location
  const rawSearch = routerState.location.search as SearchRouteSearch
  const queryParam: string = rawSearch.q ?? ''
  const pivotParam: string | undefined = rawSearch.pivot
  const [inputValue, setInputValue] = useState(queryParam)
  const activePivot =
    pivotParam && Object.values(SearchPivot).includes(pivotParam as SearchPivot)
      ? (pivotParam as SearchPivot)
      : SearchPivot.Top

  // Sync input with URL search param
  useEffect(() => {
    setInputValue(queryParam)
  }, [queryParam])

  const { data, loading } = useQuery(searchQueryDocument, {
    skip: queryParam.length < 2,
    variables: {
      limit: SEARCH_PAGE_LIMIT,
      pivot: activePivot,
      query: queryParam,
    },
  })

  const results = data?.search ?? []

  const handleSearch = useCallback(
    (newQuery: string) => {
      void navigate({
        replace: true,
        search: (prev: SearchRouteSearch) => ({
          ...prev,
          q: newQuery || undefined,
        }),
      })
    },
    [navigate],
  )

  const handlePivotChange = useCallback(
    (newPivot: string) => {
      void navigate({
        replace: true,
        search: (prev: SearchRouteSearch) => ({
          ...prev,
          pivot:
            newPivot === (SearchPivot.Top as string) ? undefined : newPivot,
        }),
      })
    },
    [navigate],
  )

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    handleSearch(inputValue)
  }

  return (
    <div className="flex h-full flex-col">
      {/* Search Header */}
      <div
        className={`
          border-b border-border bg-background/95 p-6 backdrop-blur
          supports-backdrop-filter:bg-background/60
        `}
      >
        <div className="mx-auto max-w-4xl">
          <form onSubmit={handleSubmit}>
            <div className="relative">
              <IconSearch
                className={`
                  absolute top-1/2 left-3 size-5 -translate-y-1/2
                  text-muted-foreground
                `}
              />
              <Input
                autoFocus
                className="h-12 pl-10 text-lg"
                onChange={(e) => {
                  setInputValue(e.target.value)
                  handleSearch(e.target.value)
                }}
                placeholder="Search for movies, shows, people..."
                value={inputValue}
              />
            </div>
          </form>
        </div>
      </div>

      {/* Results Area */}
      <div className="flex-1 overflow-y-auto p-6">
        <div className="mx-auto max-w-4xl">
          {queryParam.length >= 2 ? (
            <Tabs onValueChange={handlePivotChange} value={activePivot}>
              <TabsList className="mb-6">
                {pivotConfig.map((pivot) => (
                  <TabsTrigger key={pivot.value} value={pivot.value}>
                    {pivot.label}
                  </TabsTrigger>
                ))}
              </TabsList>

              {pivotConfig.map((pivot) => (
                <TabsContent key={pivot.value} value={pivot.value}>
                  <SearchResults loading={loading} results={results} />
                </TabsContent>
              ))}
            </Tabs>
          ) : (
            <div className="py-12 text-center text-muted-foreground">
              <IconSearch className="mx-auto mb-4 size-12 opacity-50" />
              <p className="text-lg">Search your library</p>
              <p className="text-sm">
                Enter at least 2 characters to start searching
              </p>
            </div>
          )}
        </div>
      </div>
    </div>
  )
}

function getMetadataTypeLabel(type: MetadataType): string {
  const labels: Partial<Record<MetadataType, string>> = {
    [MetadataType.AlbumRelease]: 'Album',
    [MetadataType.AlbumReleaseGroup]: 'Album',
    [MetadataType.Episode]: 'Episode',
    [MetadataType.Movie]: 'Movie',
    [MetadataType.Person]: 'Person',
    [MetadataType.Season]: 'Season',
    [MetadataType.Show]: 'TV Show',
    [MetadataType.Track]: 'Track',
  }
  return labels[type] ?? 'Item'
}

function isPosterAspect(type: MetadataType): boolean {
  return [
    MetadataType.Episode,
    MetadataType.Movie,
    MetadataType.Season,
    MetadataType.Show,
  ].includes(type)
}

function SearchResultCard({ result }: SearchResultCardProps) {
  const isPoster = isPosterAspect(result.metadataType)

  return (
    <Link
      className="group block space-y-2"
      params={{
        contentSourceId: String(result.librarySectionId),
        metadataItemId: result.id,
      }}
      to="/section/$contentSourceId/details/$metadataItemId"
    >
      <div
        className={cn(
          `
            relative overflow-hidden rounded-lg bg-muted transition-transform
            group-hover:scale-[1.02]
          `,
          isPoster ? 'aspect-2/3' : 'aspect-square',
        )}
      >
        {result.thumbUri ? (
          <Image
            className="size-full object-cover"
            height={isPoster ? 300 : 200}
            imageUri={result.thumbUri}
            width={200}
          />
        ) : (
          <div className="flex size-full items-center justify-center">
            <MetadataTypeIcon
              className="size-16 text-muted-foreground"
              item={{ metadataType: result.metadataType }}
            />
          </div>
        )}
      </div>
      <div className="space-y-1">
        <h3
          className={`
            line-clamp-2 text-sm leading-tight font-medium
            group-hover:text-primary
          `}
        >
          {result.title}
        </h3>
        <p className="text-xs text-muted-foreground">
          {getMetadataTypeLabel(result.metadataType)}
          {result.year ? ` • ${String(result.year)}` : ''}
        </p>
      </div>
    </Link>
  )
}

function SearchResults({
  loading,
  results,
}: {
  readonly loading: boolean
  readonly results: SearchResultData[]
}) {
  if (loading) {
    return <SearchResultsGrid loading />
  }

  if (results.length === 0) {
    return (
      <div className="py-12 text-center text-muted-foreground">
        <IconSearch className="mx-auto mb-4 size-12 opacity-50" />
        <p className="text-lg">No results found</p>
        <p className="text-sm">
          Try adjusting your search or selecting a different category
        </p>
      </div>
    )
  }

  return <SearchResultsGrid results={results} />
}

function SearchResultsGrid({ loading, results }: SearchResultsGridProps) {
  if (loading) {
    return (
      <div
        className={`
          grid grid-cols-2 gap-4
          sm:grid-cols-3
          md:grid-cols-4
          lg:grid-cols-5
        `}
      >
        {Array.from({ length: 10 }).map((_, i) => (
          <div className="space-y-2" key={`skeleton-${String(i)}`}>
            <Skeleton className="aspect-2/3 w-full rounded-lg" />
            <Skeleton className="h-4 w-3/4" />
            <Skeleton className="h-3 w-1/2" />
          </div>
        ))}
      </div>
    )
  }

  if (!results?.length) {
    return null
  }

  return (
    <div
      className={`
        grid grid-cols-2 gap-4
        sm:grid-cols-3
        md:grid-cols-4
        lg:grid-cols-5
      `}
    >
      {results.map((result) => (
        <SearchResultCard key={result.id} result={result} />
      ))}
    </div>
  )
}
