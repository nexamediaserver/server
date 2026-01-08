// Components
export {
  MobileSearchSheet,
  type SearchResultData,
  SearchResultItem,
  SearchResultsList,
  SearchSidebarResults,
} from './components'

// Hooks
export {
  type SearchResult,
  useSearch,
  type UseSearchOptions,
  type UseSearchResult,
} from './hooks'

// Utilities
export {
  getMetadataTypeLabel,
  isPosterAspect,
  MIN_SEARCH_QUERY_LENGTH,
} from './lib/utils'

// Routes
export {
  createSearchRoutes,
  searchRoute,
  type SearchRouteSearch,
} from './routes'
