import { useQuery } from '@apollo/client/react'
import { useDebounce } from '@uidotdev/usehooks'

import type { SearchQuery } from '@/shared/api/graphql/graphql'

import { searchQueryDocument } from '@/app/graphql/search'
import { SearchPivot } from '@/shared/api/graphql/graphql'

export type SearchResult = SearchQuery['search'][number]

export interface UseSearchOptions {
  /** Debounce delay in milliseconds (default: 200) */
  debounceMs?: number
  /** Maximum number of results to fetch (default: 8) */
  limit?: number
}

export interface UseSearchResult {
  /** The current debounced query value */
  debouncedQuery: string
  /** Whether search is currently loading */
  isLoading: boolean
  /** Whether user is still typing (query differs from debounced) */
  isTyping: boolean
  /** The current search query */
  query: string
  /** Search results array */
  results: SearchResult[]
  /** Update the search query */
  setQuery: (query: string) => void
}

const DEFAULT_DEBOUNCE_MS = 200
const DEFAULT_LIMIT = 8
const MIN_QUERY_LENGTH = 2

export function useSearch(
  query: string,
  setQuery: (query: string) => void,
  options?: UseSearchOptions,
): UseSearchResult {
  const { debounceMs = DEFAULT_DEBOUNCE_MS, limit = DEFAULT_LIMIT } =
    options ?? {}

  const debouncedQuery = useDebounce(query, debounceMs)

  const { data, loading } = useQuery(searchQueryDocument, {
    skip: !debouncedQuery || debouncedQuery.length < MIN_QUERY_LENGTH,
    variables: {
      limit,
      pivot: SearchPivot.Top,
      query: debouncedQuery,
    },
  })

  const results = data?.search ?? []
  const isTyping = query !== debouncedQuery && query.length >= MIN_QUERY_LENGTH

  return {
    debouncedQuery,
    isLoading: loading,
    isTyping,
    query,
    results,
    setQuery,
  }
}
