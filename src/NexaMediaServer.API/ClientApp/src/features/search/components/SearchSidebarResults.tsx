import { useQuery } from '@apollo/client/react'
import { Link } from '@tanstack/react-router'
import { useDebounce } from '@uidotdev/usehooks'
import IconArrowRight from '~icons/material-symbols/arrow-right-alt'
import IconClose from '~icons/material-symbols/close'

import { searchQueryDocument } from '@/app/graphql/search'
import { SearchPivot } from '@/shared/api/graphql/graphql'
import { Button } from '@/shared/components/ui/button'
import {
  SidebarGroup,
  SidebarGroupLabel,
  SidebarMenu,
  SidebarMenuButton,
} from '@/shared/components/ui/sidebar'

import { SearchResultsList } from './SearchResultsList'

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
  const isLoading = loading || (query !== debouncedQuery && query.length >= 2)

  return (
    <SidebarGroup className="flex h-full flex-col">
      <SidebarGroupLabel className="justify-between pr-0">
        <span>{query.length < 2 ? 'Search' : `Results for "${query}"`}</span>
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
        <SearchResultsList
          isLoading={isLoading}
          onNavigate={onClearSearch}
          query={query}
          results={results}
        />
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

// Re-export for backward compatibility
export { SearchResultItem } from './SearchResultItem'
