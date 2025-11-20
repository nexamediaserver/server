import { useQuery } from '@apollo/client/react'
import { Link } from '@tanstack/react-router'
import { useDebounce } from '@uidotdev/usehooks'
import IconArrowRight from '~icons/material-symbols/arrow-right-alt'
import IconClose from '~icons/material-symbols/close'

import type { SearchQuery } from '@/shared/api/graphql/graphql'

import { searchQueryDocument } from '@/app/graphql/search'
import { MetadataType, SearchPivot } from '@/shared/api/graphql/graphql'
import { Image } from '@/shared/components/Image'
import { MetadataTypeIcon } from '@/shared/components/MetadataTypeIcon'
import { Button } from '@/shared/components/ui/button'
import {
  SidebarGroup,
  SidebarGroupLabel,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
} from '@/shared/components/ui/sidebar'
import { Skeleton } from '@/shared/components/ui/skeleton'
import { cn } from '@/shared/lib/utils'

type SearchResultData = SearchQuery['search'][number]

interface SearchResultItemProps {
  onNavigate: () => void
  result: SearchResultData
}

interface SearchSidebarResultsProps {
  onClearSearch: () => void
  query: string
}

const INSTANT_RESULTS_LIMIT = 8

export function SearchSidebarResults({
  onClearSearch,
  query,
}: SearchSidebarResultsProps) {
  const debouncedQuery = useDebounce(query, 200)

  const { data, loading } = useQuery(searchQueryDocument, {
    skip: !debouncedQuery || debouncedQuery.length < 2,
    variables: {
      limit: INSTANT_RESULTS_LIMIT,
      pivot: SearchPivot.Top,
      query: debouncedQuery,
    },
  })

  const results = data?.search ?? []
  const showSkeleton =
    loading || (query !== debouncedQuery && query.length >= 2)

  if (query.length < 2) {
    return (
      <SidebarGroup>
        <SidebarGroupLabel className="justify-between pr-0">
          <span>Search</span>
          <Button
            className="size-6"
            onClick={onClearSearch}
            size="icon"
            variant="ghost"
          >
            <IconClose className="size-4" />
          </Button>
        </SidebarGroupLabel>
        <div className="px-3 py-4 text-sm text-muted-foreground">
          Type at least 2 characters to search...
        </div>
      </SidebarGroup>
    )
  }

  return (
    <SidebarGroup className="flex h-full flex-col">
      <SidebarGroupLabel className="justify-between pr-0">
        <span>Results for &quot;{query}&quot;</span>
        <Button
          className="size-6"
          onClick={onClearSearch}
          size="icon"
          variant="ghost"
        >
          <IconClose className="size-4" />
        </Button>
      </SidebarGroupLabel>

      <SidebarMenu className="flex-1 overflow-y-auto">
        {showSkeleton ? (
          Array.from({ length: 5 }).map((_, i) => (
            <SidebarMenuItem key={i}>
              <div className="flex items-center gap-3 p-2">
                <Skeleton className="size-10 shrink-0 rounded" />
                <div className="flex flex-1 flex-col gap-1">
                  <Skeleton className="h-4 w-3/4" />
                  <Skeleton className="h-3 w-1/2" />
                </div>
              </div>
            </SidebarMenuItem>
          ))
        ) : results.length === 0 ? (
          <div className="px-3 py-4 text-sm text-muted-foreground">
            No results found
          </div>
        ) : (
          results.map((result) => (
            <SearchResultItem
              key={result.id}
              onNavigate={onClearSearch}
              result={result}
            />
          ))
        )}
      </SidebarMenu>

      {results.length > 0 && (
        <div className="mt-auto border-t border-border pt-2">
          <SidebarMenuButton asChild>
            <Link onClick={onClearSearch} search={{ q: query }} to="/search">
              <IconArrowRight />
              View More Results
            </Link>
          </SidebarMenuButton>
        </div>
      )}
    </SidebarGroup>
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

function SearchResultItem({ onNavigate, result }: SearchResultItemProps) {
  return (
    <SidebarMenuItem>
      <SidebarMenuButton asChild className="h-auto py-2">
        <Link
          onClick={onNavigate}
          params={{
            contentSourceId: String(result.librarySectionId),
            metadataItemId: result.id,
          }}
          to="/section/$contentSourceId/details/$metadataItemId"
        >
          <div className="flex items-center gap-3">
            {result.thumbUri ? (
              <Image
                className={cn(
                  'shrink-0 rounded bg-muted',
                  isPosterAspect(result.metadataType) ? 'h-12 w-8' : 'size-10',
                )}
                height={isPosterAspect(result.metadataType) ? 48 : 40}
                imageUri={result.thumbUri}
                width={isPosterAspect(result.metadataType) ? 32 : 40}
              />
            ) : (
              <div
                className={cn(
                  'flex shrink-0 items-center justify-center rounded bg-muted',
                  isPosterAspect(result.metadataType) ? 'h-12 w-8' : 'size-10',
                )}
              >
                <MetadataTypeIcon
                  className="size-5 text-muted-foreground"
                  item={{ metadataType: result.metadataType }}
                />
              </div>
            )}
            <div className="flex min-w-0 flex-col">
              <span className="truncate text-sm font-medium">
                {result.title}
              </span>
              <span className="truncate text-xs text-muted-foreground">
                {getMetadataTypeLabel(result.metadataType)}
                {result.year ? ` • ${String(result.year)}` : ''}
              </span>
            </div>
          </div>
        </Link>
      </SidebarMenuButton>
    </SidebarMenuItem>
  )
}
