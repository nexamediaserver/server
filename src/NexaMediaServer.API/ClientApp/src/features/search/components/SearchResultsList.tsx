import type { ReactNode } from 'react'

import { MIN_SEARCH_QUERY_LENGTH } from '@/features/search/lib/utils'
import { Skeleton } from '@/shared/components/ui/skeleton'

import type { SearchResultData } from './SearchResultItem'

import { SearchResultItem } from './SearchResultItem'

export interface SearchResultsListProps {
  /** Footer content (e.g., "View More" link) */
  footer?: ReactNode
  /** Whether search is loading or user is typing */
  isLoading: boolean
  /** Callback when a result is clicked */
  onNavigate?: () => void
  /** Current search query */
  query: string
  /** Search results to display */
  results: SearchResultData[]
}

/**
 * Renders a list of search results with loading skeletons and empty states.
 * Reusable between sidebar and mobile search implementations.
 */
export function SearchResultsList({
  footer,
  isLoading,
  onNavigate,
  query,
  results,
}: SearchResultsListProps) {
  // Show hint when query is too short
  if (query.length < MIN_SEARCH_QUERY_LENGTH) {
    return (
      <div className="px-3 py-8 text-center text-sm text-muted-foreground">
        Type at least {MIN_SEARCH_QUERY_LENGTH} characters to search...
      </div>
    )
  }

  // Show loading skeletons
  if (isLoading) {
    return (
      <div className="flex flex-col gap-1 px-1">
        {Array.from({ length: 5 }).map((_, i) => (
          <div className="flex items-center gap-3 p-2" key={i}>
            <Skeleton className="size-10 shrink-0 rounded" />
            <div className="flex flex-1 flex-col gap-1">
              <Skeleton className="h-4 w-3/4" />
              <Skeleton className="h-3 w-1/2" />
            </div>
          </div>
        ))}
      </div>
    )
  }

  // Show empty state
  if (results.length === 0) {
    return (
      <div className="px-3 py-8 text-center text-sm text-muted-foreground">
        No results found for &quot;{query}&quot;
      </div>
    )
  }

  // Show results
  return (
    <div className="flex flex-col">
      <div className="flex flex-col gap-1 px-1">
        {results.map((result) => (
          <SearchResultItem
            key={result.id}
            onNavigate={onNavigate}
            result={result}
          />
        ))}
      </div>
      {footer}
    </div>
  )
}
