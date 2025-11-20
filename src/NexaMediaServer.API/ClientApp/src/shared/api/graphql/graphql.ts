/* eslint-disable */
import type { TypedDocumentNode as DocumentNode } from '@graphql-typed-document-node/core'
export type Maybe<T> = T | null
export type InputMaybe<T> = T | null | undefined
export type Exact<T extends { [key: string]: unknown }> = {
  [K in keyof T]: T[K]
}
export type MakeOptional<T, K extends keyof T> = Omit<T, K> & {
  [SubKey in K]?: Maybe<T[SubKey]>
}
export type MakeMaybe<T, K extends keyof T> = Omit<T, K> & {
  [SubKey in K]: Maybe<T[SubKey]>
}
export type MakeEmpty<
  T extends { [key: string]: unknown },
  K extends keyof T,
> = { [_ in K]?: never }
export type Incremental<T> =
  | T
  | {
      [P in keyof T]?: P extends ' $fragmentName' | '__typename' ? T[P] : never
    }
/** All built-in and custom scalars, mapped to their actual values */
export type Scalars = {
  ID: { input: string; output: string }
  String: { input: string; output: string }
  Boolean: { input: boolean; output: boolean }
  Int: { input: number; output: number }
  Float: { input: number; output: number }
  /** The `DateTime` scalar represents an ISO-8601 compliant date time type. */
  DateTime: { input: any; output: any }
  /** The `LocalDate` scalar type represents an ISO date string, represented as UTF-8 character sequences YYYY-MM-DD. The scalar follows the specification defined in RFC3339 */
  LocalDate: { input: any; output: any }
  /** The `Upload` scalar type represents a file upload. */
  Upload: { input: any; output: any }
}

/** Represents the input required to create a library section. */
export type AddLibrarySectionInput = {
  /** Gets or sets the library name. */
  name: Scalars['String']['input']
  /** Gets or sets the root paths associated with the library. */
  rootPaths: Array<Scalars['String']['input']>
  /** Gets or sets the initial settings for the library section. */
  settings?: InputMaybe<LibrarySectionSettingsInput>
  /** Gets or sets the library type. */
  type: LibraryType
}

/** Represents the mutation payload returned after creating a library section. */
export type AddLibrarySectionPayload = {
  __typename?: 'AddLibrarySectionPayload'
  /** Gets the created library section. */
  librarySection: LibrarySection
  query: Query
  /** Gets the identifier of the queued scan. */
  scanId: Scalars['Int']['output']
}

/** Defines when a policy shall be executed. */
export enum ApplyPolicy {
  /** After the resolver was executed. */
  AfterResolver = 'AFTER_RESOLVER',
  /** Before the resolver was executed. */
  BeforeResolver = 'BEFORE_RESOLVER',
  /** The policy is applied in the validation step before the execution. */
  Validation = 'VALIDATION',
}

export type BooleanOperationFilterInput = {
  eq?: InputMaybe<Scalars['Boolean']['input']>
  neq?: InputMaybe<Scalars['Boolean']['input']>
}

/** A connection to a list of items. */
export type ChildrenConnection = {
  __typename?: 'ChildrenConnection'
  /** A list of edges. */
  edges?: Maybe<Array<ChildrenEdge>>
  /** A flattened list of the nodes. */
  nodes?: Maybe<Array<Item>>
  /** Information to aid in pagination. */
  pageInfo: PageInfo
  /** Identifies the total count of items in the connection. */
  totalCount: Scalars['Int']['output']
}

/** An edge in a connection. */
export type ChildrenEdge = {
  __typename?: 'ChildrenEdge'
  /** A cursor for use in pagination. */
  cursor: Scalars['String']['output']
  /** The item at the end of the edge. */
  node: Item
}

/** GraphQL type representing a directory listing response. */
export type DirectoryListing = {
  __typename?: 'DirectoryListing'
  /** Gets the canonical path that was listed. */
  currentPath: Scalars['String']['output']
  /** Gets the child entries. */
  entries: Array<FileSystemEntry>
  /** Gets the parent path if available. */
  parentPath?: Maybe<Scalars['String']['output']>
}

/** Defines how episodes should be sorted within a season/series for display and selection. */
export enum EpisodeSortOrder {
  /** Sort by original air date ascending. */
  AirDate = 'AIR_DATE',
  /** Sort by production order when available; falls back to air date. */
  Production = 'PRODUCTION',
  /** Sort by season and episode number (SxxExx), ascending. */
  SeasonEpisode = 'SEASON_EPISODE',
}

export type EpisodeSortOrderOperationFilterInput = {
  eq?: InputMaybe<EpisodeSortOrder>
  in?: InputMaybe<Array<EpisodeSortOrder>>
  neq?: InputMaybe<EpisodeSortOrder>
  nin?: InputMaybe<Array<EpisodeSortOrder>>
}

/** GraphQL type describing an entry in a directory listing. */
export type FileSystemEntry = {
  __typename?: 'FileSystemEntry'
  /** Gets a value indicating whether the entry is a directory. */
  isDirectory: Scalars['Boolean']['output']
  /** Gets a value indicating whether the entry is a file. */
  isFile: Scalars['Boolean']['output']
  /** Gets a value indicating whether the entry can be selected in the UI. */
  isSelectable: Scalars['Boolean']['output']
  /** Gets a value indicating whether the entry is a symbolic link. */
  isSymbolicLink: Scalars['Boolean']['output']
  /** Gets the entry name. */
  name: Scalars['String']['output']
  /** Gets the raw server path to the entry. */
  path: Scalars['String']['output']
}

/** GraphQL type representing an available filesystem root. */
export type FileSystemRoot = {
  __typename?: 'FileSystemRoot'
  /** Gets the identifier for the root entry. */
  id: Scalars['String']['output']
  /** Gets a value indicating whether the root is read-only. */
  isReadOnly: Scalars['Boolean']['output']
  /** Gets the kind of root. */
  kind: FileSystemRootKind
  /** Gets the display label. */
  label: Scalars['String']['output']
  /** Gets the raw path accessible by the server OS. */
  path: Scalars['String']['output']
}

/** Categorizes filesystem root types. */
export enum FileSystemRootKind {
  /** A logical drive (Windows) or volume. */
  Drive = 'DRIVE',
  /** A mounted filesystem. */
  Mount = 'MOUNT',
  /** The primary OS root (e.g., / or C:\). */
  Root = 'ROOT',
}

export type IdOperationFilterInput = {
  eq?: InputMaybe<Scalars['ID']['input']>
  in?: InputMaybe<Array<InputMaybe<Scalars['ID']['input']>>>
  neq?: InputMaybe<Scalars['ID']['input']>
  nin?: InputMaybe<Array<InputMaybe<Scalars['ID']['input']>>>
}

export type IntOperationFilterInput = {
  eq?: InputMaybe<Scalars['Int']['input']>
  gt?: InputMaybe<Scalars['Int']['input']>
  gte?: InputMaybe<Scalars['Int']['input']>
  in?: InputMaybe<Array<InputMaybe<Scalars['Int']['input']>>>
  lt?: InputMaybe<Scalars['Int']['input']>
  lte?: InputMaybe<Scalars['Int']['input']>
  neq?: InputMaybe<Scalars['Int']['input']>
  ngt?: InputMaybe<Scalars['Int']['input']>
  ngte?: InputMaybe<Scalars['Int']['input']>
  nin?: InputMaybe<Array<InputMaybe<Scalars['Int']['input']>>>
  nlt?: InputMaybe<Scalars['Int']['input']>
  nlte?: InputMaybe<Scalars['Int']['input']>
}

/** Representation of a metadata item for pagination queries. */
export type Item = Node & {
  __typename?: 'Item'
  /** Gets the backdrop URL of the metadata item. */
  artUri?: Maybe<Scalars['String']['output']>
  /** Gets the number of child items in the metadata item. */
  childCount: Scalars['Int']['output']
  /** Gets the content rating of the metadata item. */
  contentRating: Scalars['String']['output']
  /**
   * Gets the direct play URL for streaming the first media part of this item.
   * Returns null if no media parts are available.
   */
  directPlayUrl?: Maybe<Scalars['String']['output']>
  /** Gets the grandparent identifier of the metadata item. */
  grandparentId: Scalars['ID']['output']
  /** Gets the global Relay-compatible identifier of the metadata item. */
  id: Scalars['ID']['output']
  /** Gets the index of the metadata item. */
  index: Scalars['Int']['output']
  /** Gets the number of leaf items in the metadata item. */
  leafCount: Scalars['Int']['output']
  /** Gets the length of the metadata item in milliseconds. */
  length: Scalars['Int']['output']
  /** Gets the logo URL of the metadata item. */
  logoUri?: Maybe<Scalars['String']['output']>
  /** Gets the type of the metadata item. */
  metadataType: MetadataType
  /** Gets the original title of the metadata item. */
  originalTitle: Scalars['String']['output']
  /** Gets the date the metadata item was originally available. */
  originallyAvailableAt?: Maybe<Scalars['LocalDate']['output']>
  /** Gets the parent identifier of the metadata item. */
  parentId: Scalars['ID']['output']
  /** Gets the parent title of the metadata item. */
  parentTitle: Scalars['String']['output']
  /**
   * Resolves the user rating for the metadata item.
   *
   *
   * **Returns:**
   * The rating value or 0 when unset.
   */
  rating: Scalars['Float']['output']
  /** Gets the summary description of the metadata item. */
  summary: Scalars['String']['output']
  /** Gets the tagline of the metadata item. */
  tagline: Scalars['String']['output']
  /** Gets the theme URL of the metadata item. */
  themeUrl?: Maybe<Scalars['String']['output']>
  /** Gets the thumbnail URL of the metadata item. */
  thumbUri?: Maybe<Scalars['String']['output']>
  /** Gets the title of the metadata item. */
  title: Scalars['String']['output']
  /** Gets the sortable title of the metadata item. */
  titleSort: Scalars['String']['output']
  /**
   * Gets the trickplay thumbnail track URL (WebVTT format) for video scrubbing.
   * Returns null if no trickplay data is available.
   */
  trickplayUrl?: Maybe<Scalars['String']['output']>
  /**
   * Resolves the number of times the current user has viewed the metadata item.
   *
   *
   * **Returns:**
   * The number of completed views for the current user.
   */
  viewCount: Scalars['Int']['output']
  /**
   * Resolves the resume offset for the current user.
   *
   *
   * **Returns:**
   * The playback offset in milliseconds.
   */
  viewOffset: Scalars['Int']['output']
  /** Gets the year the metadata item was released. */
  year: Scalars['Int']['output']
}

/** Representation of a metadata item for pagination queries. */
export type ItemFilterInput = {
  and?: InputMaybe<Array<ItemFilterInput>>
  /** Gets the backdrop URL of the metadata item. */
  artUri?: InputMaybe<StringOperationFilterInput>
  /** Gets the number of child items in the metadata item. */
  childCount?: InputMaybe<IntOperationFilterInput>
  /** Gets the content rating of the metadata item. */
  contentRating?: InputMaybe<StringOperationFilterInput>
  /**
   * Gets the direct play URL for streaming the first media part of this item.
   * Returns null if no media parts are available.
   */
  directPlayUrl?: InputMaybe<StringOperationFilterInput>
  /** Gets the grandparent identifier of the metadata item. */
  grandparentId?: InputMaybe<IdOperationFilterInput>
  /** Gets the global Relay-compatible identifier of the metadata item. */
  id?: InputMaybe<IdOperationFilterInput>
  /** Gets the index of the metadata item. */
  index?: InputMaybe<IntOperationFilterInput>
  /** Gets the number of leaf items in the metadata item. */
  leafCount?: InputMaybe<IntOperationFilterInput>
  /** Gets the length of the metadata item in milliseconds. */
  length?: InputMaybe<IntOperationFilterInput>
  /** Gets the logo URL of the metadata item. */
  logoUri?: InputMaybe<StringOperationFilterInput>
  /** Gets the type of the metadata item. */
  metadataType?: InputMaybe<MetadataTypeOperationFilterInput>
  or?: InputMaybe<Array<ItemFilterInput>>
  /** Gets the original title of the metadata item. */
  originalTitle?: InputMaybe<StringOperationFilterInput>
  /** Gets the date the metadata item was originally available. */
  originallyAvailableAt?: InputMaybe<LocalDateOperationFilterInput>
  /** Gets the parent identifier of the metadata item. */
  parentId?: InputMaybe<IdOperationFilterInput>
  /** Gets the parent title of the metadata item. */
  parentTitle?: InputMaybe<StringOperationFilterInput>
  /** Gets the summary description of the metadata item. */
  summary?: InputMaybe<StringOperationFilterInput>
  /** Gets the tagline of the metadata item. */
  tagline?: InputMaybe<StringOperationFilterInput>
  /** Gets the theme URL of the metadata item. */
  themeUrl?: InputMaybe<StringOperationFilterInput>
  /** Gets the thumbnail URL of the metadata item. */
  thumbUri?: InputMaybe<StringOperationFilterInput>
  /** Gets the title of the metadata item. */
  title?: InputMaybe<StringOperationFilterInput>
  /** Gets the sortable title of the metadata item. */
  titleSort?: InputMaybe<StringOperationFilterInput>
  /**
   * Gets the trickplay thumbnail track URL (WebVTT format) for video scrubbing.
   * Returns null if no trickplay data is available.
   */
  trickplayUrl?: InputMaybe<StringOperationFilterInput>
  /** Gets the year the metadata item was released. */
  year?: InputMaybe<IntOperationFilterInput>
}

/** Representation of a metadata item for pagination queries. */
export type ItemSortInput = {
  /** Gets the sortable title of the metadata item. */
  title?: InputMaybe<SortEnumType>
  /** Gets the year the metadata item was released. */
  year?: InputMaybe<SortEnumType>
}

/** Represents a job notification event sent to clients via GraphQL subscriptions. */
export type JobNotification = {
  __typename?: 'JobNotification'
  /** Gets or sets the number of completed items. */
  completedItems: Scalars['Int']['output']
  /** Gets or sets a human-readable description of the job. */
  description: Scalars['String']['output']
  /** Gets or sets the unique identifier for this job instance. */
  id: Scalars['String']['output']
  /** Gets or sets a value indicating whether the job is still running. */
  isActive: Scalars['Boolean']['output']
  /** Gets or sets the library section ID if the job is related to a specific library. */
  librarySectionId?: Maybe<Scalars['Int']['output']>
  /** Gets or sets the library section name if available. */
  librarySectionName?: Maybe<Scalars['String']['output']>
  /** Gets or sets the current progress percentage (0-100). */
  progressPercentage: Scalars['Float']['output']
  /** Gets or sets the timestamp when this notification was generated. */
  timestamp: Scalars['DateTime']['output']
  /** Gets or sets the total number of items to process. */
  totalItems: Scalars['Int']['output']
  /** Gets or sets the type of job. */
  type: JobType
}

/** Represents the type of background job being executed. */
export enum JobType {
  /** File analysis job. */
  FileAnalysis = 'FILE_ANALYSIS',
  /** Image generation job. */
  ImageGeneration = 'IMAGE_GENERATION',
  /** Library scanning job. */
  LibraryScan = 'LIBRARY_SCAN',
  /** Metadata refresh job for one or more items. */
  MetadataRefresh = 'METADATA_REFRESH',
  /** Trickplay (BIF) generation job. */
  TrickplayGeneration = 'TRICKPLAY_GENERATION',
}

export type KeyValuePairOfStringAndDictionaryOfStringAndString = {
  __typename?: 'KeyValuePairOfStringAndDictionaryOfStringAndString'
  key: Scalars['String']['output']
  value: Array<KeyValuePairOfStringAndString>
}

export type KeyValuePairOfStringAndDictionaryOfStringAndStringFilterInput = {
  and?: InputMaybe<
    Array<KeyValuePairOfStringAndDictionaryOfStringAndStringFilterInput>
  >
  key?: InputMaybe<StringOperationFilterInput>
  or?: InputMaybe<
    Array<KeyValuePairOfStringAndDictionaryOfStringAndStringFilterInput>
  >
  value?: InputMaybe<ListFilterInputTypeOfKeyValuePairOfStringAndStringFilterInput>
}

export type KeyValuePairOfStringAndDictionaryOfStringAndStringInput = {
  key: Scalars['String']['input']
  value: Array<KeyValuePairOfStringAndStringInput>
}

export type KeyValuePairOfStringAndString = {
  __typename?: 'KeyValuePairOfStringAndString'
  key: Scalars['String']['output']
  value: Scalars['String']['output']
}

export type KeyValuePairOfStringAndStringFilterInput = {
  and?: InputMaybe<Array<KeyValuePairOfStringAndStringFilterInput>>
  key?: InputMaybe<StringOperationFilterInput>
  or?: InputMaybe<Array<KeyValuePairOfStringAndStringFilterInput>>
  value?: InputMaybe<StringOperationFilterInput>
}

export type KeyValuePairOfStringAndStringInput = {
  key: Scalars['String']['input']
  value: Scalars['String']['input']
}

/** Representation of a library section for GraphQL queries. */
export type LibrarySection = Node & {
  __typename?: 'LibrarySection'
  /**
   * Gets a Relay-paginated list of top-level (root) metadata items (those without a parent) for this library section.
   *
   *
   * **Returns:**
   * An in-memory queryable used by HotChocolate to create a connection.
   */
  children?: Maybe<ChildrenConnection>
  /** Gets the global Relay-compatible identifier of the library section. */
  id: Scalars['ID']['output']
  /** Gets the list of root locations for the library section. */
  locations: Array<Scalars['String']['output']>
  /** Gets the display name of the library section. */
  name: Scalars['String']['output']
  /** Gets the settings for this library section. */
  settings: LibrarySectionSettings
  /** Gets the sortable name of the library section. */
  sortName: Scalars['String']['output']
  /** Gets the type of the library section. */
  type: LibraryType
}

/** Representation of a library section for GraphQL queries. */
export type LibrarySectionChildrenArgs = {
  after?: InputMaybe<Scalars['String']['input']>
  before?: InputMaybe<Scalars['String']['input']>
  first?: InputMaybe<Scalars['Int']['input']>
  last?: InputMaybe<Scalars['Int']['input']>
  metadataType: MetadataType
  order?: InputMaybe<Array<ItemSortInput>>
  where?: InputMaybe<ItemFilterInput>
}

/** Representation of a library section for GraphQL queries. */
export type LibrarySectionFilterInput = {
  and?: InputMaybe<Array<LibrarySectionFilterInput>>
  /** Gets the global Relay-compatible identifier of the library section. */
  id?: InputMaybe<IdOperationFilterInput>
  /** Gets the list of root locations for the library section. */
  locations?: InputMaybe<ListStringOperationFilterInput>
  /** Gets the display name of the library section. */
  name?: InputMaybe<StringOperationFilterInput>
  or?: InputMaybe<Array<LibrarySectionFilterInput>>
  /** Gets the settings for this library section. */
  settings?: InputMaybe<LibrarySectionSettingsFilterInput>
  /** Gets the sortable name of the library section. */
  sortName?: InputMaybe<StringOperationFilterInput>
  /** Gets the type of the library section. */
  type?: InputMaybe<LibraryTypeOperationFilterInput>
}

/** GraphQL representation of per-library settings. */
export type LibrarySectionSettings = {
  __typename?: 'LibrarySectionSettings'
  /** Gets the episode sort order preference for episodic content. */
  episodeSortOrder: EpisodeSortOrder
  /** Gets a value indicating whether to hide seasons for single-season series. */
  hideSeasonsForSingleSeasonSeries: Scalars['Boolean']['output']
  /** Gets the ordered list of metadata agent identifiers to use. */
  metadataAgentOrder: Array<Scalars['String']['output']>
  /** Gets the map of metadata agent specific settings: agentId -> (key -> value). */
  metadataAgentSettings: Array<KeyValuePairOfStringAndDictionaryOfStringAndString>
  /** Gets the preferred audio languages (ordered). */
  preferredAudioLanguages: Array<Scalars['String']['output']>
  /** Gets the preferred metadata language (BCP-47), e.g. "en", "de-DE". */
  preferredMetadataLanguage: Scalars['String']['output']
  /** Gets the preferred subtitle languages (ordered). */
  preferredSubtitleLanguages: Array<Scalars['String']['output']>
}

/** GraphQL representation of per-library settings. */
export type LibrarySectionSettingsFilterInput = {
  and?: InputMaybe<Array<LibrarySectionSettingsFilterInput>>
  /** Gets the episode sort order preference for episodic content. */
  episodeSortOrder?: InputMaybe<EpisodeSortOrderOperationFilterInput>
  /** Gets a value indicating whether to hide seasons for single-season series. */
  hideSeasonsForSingleSeasonSeries?: InputMaybe<BooleanOperationFilterInput>
  /** Gets the ordered list of metadata agent identifiers to use. */
  metadataAgentOrder?: InputMaybe<ListStringOperationFilterInput>
  /** Gets the map of metadata agent specific settings: agentId -> (key -> value). */
  metadataAgentSettings?: InputMaybe<ListFilterInputTypeOfKeyValuePairOfStringAndDictionaryOfStringAndStringFilterInput>
  or?: InputMaybe<Array<LibrarySectionSettingsFilterInput>>
  /** Gets the preferred audio languages (ordered). */
  preferredAudioLanguages?: InputMaybe<ListStringOperationFilterInput>
  /** Gets the preferred metadata language (BCP-47), e.g. "en", "de-DE". */
  preferredMetadataLanguage?: InputMaybe<StringOperationFilterInput>
  /** Gets the preferred subtitle languages (ordered). */
  preferredSubtitleLanguages?: InputMaybe<ListStringOperationFilterInput>
}

/** GraphQL representation of per-library settings. */
export type LibrarySectionSettingsInput = {
  /** Gets the episode sort order preference for episodic content. */
  episodeSortOrder: EpisodeSortOrder
  /** Gets a value indicating whether to hide seasons for single-season series. */
  hideSeasonsForSingleSeasonSeries: Scalars['Boolean']['input']
  /** Gets the ordered list of metadata agent identifiers to use. */
  metadataAgentOrder: Array<Scalars['String']['input']>
  /** Gets the map of metadata agent specific settings: agentId -> (key -> value). */
  metadataAgentSettings: Array<KeyValuePairOfStringAndDictionaryOfStringAndStringInput>
  /** Gets the preferred audio languages (ordered). */
  preferredAudioLanguages: Array<Scalars['String']['input']>
  /** Gets the preferred metadata language (BCP-47), e.g. "en", "de-DE". */
  preferredMetadataLanguage: Scalars['String']['input']
  /** Gets the preferred subtitle languages (ordered). */
  preferredSubtitleLanguages: Array<Scalars['String']['input']>
}

/** GraphQL representation of per-library settings. */
export type LibrarySectionSettingsSortInput = {
  /** Gets the episode sort order preference for episodic content. */
  episodeSortOrder?: InputMaybe<SortEnumType>
  /** Gets a value indicating whether to hide seasons for single-season series. */
  hideSeasonsForSingleSeasonSeries?: InputMaybe<SortEnumType>
  /** Gets the preferred metadata language (BCP-47), e.g. "en", "de-DE". */
  preferredMetadataLanguage?: InputMaybe<SortEnumType>
}

/** Representation of a library section for GraphQL queries. */
export type LibrarySectionSortInput = {
  /** Gets the global Relay-compatible identifier of the library section. */
  id?: InputMaybe<SortEnumType>
  /** Gets the display name of the library section. */
  name?: InputMaybe<SortEnumType>
  /** Gets the settings for this library section. */
  settings?: InputMaybe<LibrarySectionSettingsSortInput>
  /** Gets the sortable name of the library section. */
  sortName?: InputMaybe<SortEnumType>
  /** Gets the type of the library section. */
  type?: InputMaybe<SortEnumType>
}

/** A connection to a list of items. */
export type LibrarySectionsConnection = {
  __typename?: 'LibrarySectionsConnection'
  /** A list of edges. */
  edges?: Maybe<Array<LibrarySectionsEdge>>
  /** A flattened list of the nodes. */
  nodes?: Maybe<Array<LibrarySection>>
  /** Information to aid in pagination. */
  pageInfo: PageInfo
  /** Identifies the total count of items in the connection. */
  totalCount: Scalars['Int']['output']
}

/** An edge in a connection. */
export type LibrarySectionsEdge = {
  __typename?: 'LibrarySectionsEdge'
  /** A cursor for use in pagination. */
  cursor: Scalars['String']['output']
  /** The item at the end of the edge. */
  node: LibrarySection
}

/** Represents the type of media library. */
export enum LibraryType {
  /** Audiobooks library containing narrated book content. */
  Audiobooks = 'AUDIOBOOKS',
  /** Books library containing novels, non-fiction, and written literature. */
  Books = 'BOOKS',
  /** Comics library containing comic books and graphic novels. */
  Comics = 'COMICS',
  /** Games library containing video games across all platforms. */
  Games = 'GAMES',
  /** Home Videos library for personal video recordings. */
  HomeVideos = 'HOME_VIDEOS',
  /** Magazines library containing periodical publications. */
  Magazines = 'MAGAZINES',
  /** Manga library containing Japanese manga series. */
  Manga = 'MANGA',
  /** Movies library containing feature films, short films, and documentaries. */
  Movies = 'MOVIES',
  /** Music library containing albums, tracks, and recordings. */
  Music = 'MUSIC',
  /** Music Videos library containing standalone music video content. */
  MusicVideos = 'MUSIC_VIDEOS',
  /** Photos library containing personal photographs and albums. */
  Photos = 'PHOTOS',
  /** Pictures library containing digital art, wallpapers, and images. */
  Pictures = 'PICTURES',
  /** Podcasts library containing podcast series and episodes. */
  Podcasts = 'PODCASTS',
  /** TV Shows library containing series, seasons, and episodes. */
  TvShows = 'TV_SHOWS',
}

export type LibraryTypeOperationFilterInput = {
  eq?: InputMaybe<LibraryType>
  in?: InputMaybe<Array<LibraryType>>
  neq?: InputMaybe<LibraryType>
  nin?: InputMaybe<Array<LibraryType>>
}

export type ListFilterInputTypeOfKeyValuePairOfStringAndDictionaryOfStringAndStringFilterInput =
  {
    all?: InputMaybe<KeyValuePairOfStringAndDictionaryOfStringAndStringFilterInput>
    any?: InputMaybe<Scalars['Boolean']['input']>
    none?: InputMaybe<KeyValuePairOfStringAndDictionaryOfStringAndStringFilterInput>
    some?: InputMaybe<KeyValuePairOfStringAndDictionaryOfStringAndStringFilterInput>
  }

export type ListFilterInputTypeOfKeyValuePairOfStringAndStringFilterInput = {
  all?: InputMaybe<KeyValuePairOfStringAndStringFilterInput>
  any?: InputMaybe<Scalars['Boolean']['input']>
  none?: InputMaybe<KeyValuePairOfStringAndStringFilterInput>
  some?: InputMaybe<KeyValuePairOfStringAndStringFilterInput>
}

export type ListStringOperationFilterInput = {
  all?: InputMaybe<StringOperationFilterInput>
  any?: InputMaybe<Scalars['Boolean']['input']>
  none?: InputMaybe<StringOperationFilterInput>
  some?: InputMaybe<StringOperationFilterInput>
}

export type LocalDateOperationFilterInput = {
  eq?: InputMaybe<Scalars['LocalDate']['input']>
  gt?: InputMaybe<Scalars['LocalDate']['input']>
  gte?: InputMaybe<Scalars['LocalDate']['input']>
  in?: InputMaybe<Array<InputMaybe<Scalars['LocalDate']['input']>>>
  lt?: InputMaybe<Scalars['LocalDate']['input']>
  lte?: InputMaybe<Scalars['LocalDate']['input']>
  neq?: InputMaybe<Scalars['LocalDate']['input']>
  ngt?: InputMaybe<Scalars['LocalDate']['input']>
  ngte?: InputMaybe<Scalars['LocalDate']['input']>
  nin?: InputMaybe<Array<InputMaybe<Scalars['LocalDate']['input']>>>
  nlt?: InputMaybe<Scalars['LocalDate']['input']>
  nlte?: InputMaybe<Scalars['LocalDate']['input']>
}

/** A connection to a list of items. */
export type MetadataItemsConnection = {
  __typename?: 'MetadataItemsConnection'
  /** A list of edges. */
  edges?: Maybe<Array<MetadataItemsEdge>>
  /** A flattened list of the nodes. */
  nodes?: Maybe<Array<Item>>
  /** Information to aid in pagination. */
  pageInfo: PageInfo
}

/** An edge in a connection. */
export type MetadataItemsEdge = {
  __typename?: 'MetadataItemsEdge'
  /** A cursor for use in pagination. */
  cursor: Scalars['String']['output']
  /** The item at the end of the edge. */
  node: Item
}

/** Enumeration of supported metadata types. */
export enum MetadataType {
  /** The metadata represents a audio album release. */
  AlbumRelease = 'ALBUM_RELEASE',
  /** The metadata represents a grouping of album releases, such as a studio album or compilation. */
  AlbumReleaseGroup = 'ALBUM_RELEASE_GROUP',
  /** The metadata represents a audio work. */
  AudioWork = 'AUDIO_WORK',
  /**
   * The metadata represents an ordered set of books, such as a manga series, a periodical,
   * or a comic book series.
   */
  BookSeries = 'BOOK_SERIES',
  /** Clip metadata type. */
  Clip = 'CLIP',
  /** Collection metadata type. */
  Collection = 'COLLECTION',
  /**
   * The metadata represents a concrete publication of a book, such as a specific edition
   * or format.
   */
  Edition = 'EDITION',
  /**
   * The metadata represents a grouping of book editions, such as a single book released
   * in multiple formats (hardcover, paperback, eBook, audiobook).
   */
  EditionGroup = 'EDITION_GROUP',
  /** The metadata represents an item within a book edition, such as a chapter or volume. */
  EditionItem = 'EDITION_ITEM',
  /** The metadata represents an episode of a TV show. */
  Episode = 'EPISODE',
  /** The metadata represents a single video game. */
  Game = 'GAME',
  /** The metadata represents a video game franchise. */
  GameFranchise = 'GAME_FRANCHISE',
  /** The metadata represents a game release. */
  GameRelease = 'GAME_RELEASE',
  /** The metadata represents a video game series. */
  GameSeries = 'GAME_SERIES',
  /** The metadata represents a group of people, such as a band, a troupe, or a cast. */
  Group = 'GROUP',
  /** The metadata represents a literary work as a whole. */
  LiteraryWork = 'LITERARY_WORK',
  /** The metadata represents a part of a literary work, such as a chapter or section. */
  LiteraryWorkPart = 'LITERARY_WORK_PART',
  /** The metadata represents a movie, either feature-length or short film. */
  Movie = 'MOVIE',
  /** Optimized version metadata type. */
  OptimizedVersion = 'OPTIMIZED_VERSION',
  /** The metadata represents an individual person. */
  Person = 'PERSON',
  /** The metadata represents a photo. */
  Photo = 'PHOTO',
  /** The metadata represents a photo album. */
  PhotoAlbum = 'PHOTO_ALBUM',
  /** The metadata represents a picture. */
  Picture = 'PICTURE',
  /** The metadata represents a picture set. */
  PictureSet = 'PICTURE_SET',
  /** Playlist metadata type. */
  Playlist = 'PLAYLIST',
  /** Playlists folder metadata type. */
  PlaylistsFolder = 'PLAYLISTS_FOLDER',
  /** The metadata represents a audio recording. */
  Recording = 'RECORDING',
  /** The metadata represents a single season of a TV show. */
  Season = 'SEASON',
  /** The metadata represents a TV show. */
  Show = 'SHOW',
  /** The metadata represents a audio track. */
  Track = 'TRACK',
  /** Trailer metadata type. */
  Trailer = 'TRAILER',
  /** Unknown or unspecified metadata type. */
  Unknown = 'UNKNOWN',
  /** User playlist item metadata type. */
  UserPlaylistItem = 'USER_PLAYLIST_ITEM',
}

export type MetadataTypeOperationFilterInput = {
  eq?: InputMaybe<MetadataType>
  in?: InputMaybe<Array<MetadataType>>
  neq?: InputMaybe<MetadataType>
  nin?: InputMaybe<Array<MetadataType>>
}

export type Mutation = {
  __typename?: 'Mutation'
  /**
   * Adds a new library section and schedules an initial scan.
   *
   *
   * **Returns:**
   * The created library section and scan metadata.
   */
  addLibrarySection: AddLibrarySectionPayload
}

export type MutationAddLibrarySectionArgs = {
  input: AddLibrarySectionInput
}

/** The node interface is implemented by entities that have a global unique identifier. */
export type Node = {
  id: Scalars['ID']['output']
}

/** Information about pagination in a connection. */
export type PageInfo = {
  __typename?: 'PageInfo'
  /** When paginating forwards, the cursor to continue. */
  endCursor?: Maybe<Scalars['String']['output']>
  /** Indicates whether more edges exist following the set defined by the clients arguments. */
  hasNextPage: Scalars['Boolean']['output']
  /** Indicates whether more edges exist prior the set defined by the clients arguments. */
  hasPreviousPage: Scalars['Boolean']['output']
  /** When paginating backwards, the cursor to continue. */
  startCursor?: Maybe<Scalars['String']['output']>
}

export type Query = {
  __typename?: 'Query'
  /**
   * Browses a directory path, returning child entries, while ensuring access restrictions.
   *
   *
   * **Returns:**
   * The directory listing for the requested path.
   */
  browseDirectory: DirectoryListing
  /**
   * Lists filesystem roots (drives, mounts) that can be browsed for library creation.
   *
   *
   * **Returns:**
   * A collection of filesystem roots.
   */
  fileSystemRoots: Array<FileSystemRoot>
  /**
   * Gets a library section by its global Relay ID.
   *
   *
   * **Returns:**
   * A single LibrarySection.
   */
  librarySection?: Maybe<LibrarySection>
  /**
   * Gets a paginated list of library sections.
   *
   *
   * **Returns:**
   * A connection of LibrarySections.
   */
  librarySections?: Maybe<LibrarySectionsConnection>
  /**
   * Gets a metadata item.
   *
   *
   * **Returns:**
   * A metadata item instance.
   */
  metadataItem?: Maybe<Item>
  /**
   * Gets a collection of metadata items.
   *
   *
   * **Returns:**
   * A collection of metadata items.
   */
  metadataItems?: Maybe<MetadataItemsConnection>
  /** Fetches an object given its ID. */
  node?: Maybe<Node>
  /** Lookup nodes by a list of IDs. */
  nodes: Array<Maybe<Node>>
  /**
   * Gets basic server information like version and environment.
   *
   *
   * **Returns:**
   * The server info object.
   */
  serverInfo: ServerInfo
}

export type QueryBrowseDirectoryArgs = {
  path: Scalars['String']['input']
}

export type QueryLibrarySectionArgs = {
  id: Scalars['ID']['input']
}

export type QueryLibrarySectionsArgs = {
  after?: InputMaybe<Scalars['String']['input']>
  before?: InputMaybe<Scalars['String']['input']>
  first?: InputMaybe<Scalars['Int']['input']>
  last?: InputMaybe<Scalars['Int']['input']>
  order?: InputMaybe<Array<LibrarySectionSortInput>>
  where?: InputMaybe<LibrarySectionFilterInput>
}

export type QueryMetadataItemArgs = {
  id: Scalars['ID']['input']
}

export type QueryMetadataItemsArgs = {
  after?: InputMaybe<Scalars['String']['input']>
  before?: InputMaybe<Scalars['String']['input']>
  first?: InputMaybe<Scalars['Int']['input']>
  last?: InputMaybe<Scalars['Int']['input']>
  order?: InputMaybe<Array<ItemSortInput>>
  where?: InputMaybe<ItemFilterInput>
}

export type QueryNodeArgs = {
  id: Scalars['ID']['input']
}

export type QueryNodesArgs = {
  ids: Array<Scalars['ID']['input']>
}

/** Represents server runtime information for client display. */
export type ServerInfo = {
  __typename?: 'ServerInfo'
  /** Gets a value indicating whether the server is running in Development environment. */
  isDevelopment: Scalars['Boolean']['output']
  /** Gets the semantic version string of the running server build. */
  versionString: Scalars['String']['output']
}

export enum SortEnumType {
  Asc = 'ASC',
  Desc = 'DESC',
}

export type StringOperationFilterInput = {
  and?: InputMaybe<Array<StringOperationFilterInput>>
  contains?: InputMaybe<Scalars['String']['input']>
  endsWith?: InputMaybe<Scalars['String']['input']>
  eq?: InputMaybe<Scalars['String']['input']>
  in?: InputMaybe<Array<InputMaybe<Scalars['String']['input']>>>
  ncontains?: InputMaybe<Scalars['String']['input']>
  nendsWith?: InputMaybe<Scalars['String']['input']>
  neq?: InputMaybe<Scalars['String']['input']>
  nin?: InputMaybe<Array<InputMaybe<Scalars['String']['input']>>>
  nstartsWith?: InputMaybe<Scalars['String']['input']>
  or?: InputMaybe<Array<StringOperationFilterInput>>
  startsWith?: InputMaybe<Scalars['String']['input']>
}

/** Defines GraphQL subscription operations for the API. */
export type Subscription = {
  __typename?: 'Subscription'
  /**
   * Streams job notifications for background tasks such as library scans and metadata refresh.
   * Clients receive real-time updates about job progress and completion.
   *
   *
   * **Returns:**
   * The job notification with progress information.
   */
  onJobNotification: JobNotification
  /**
   * Streams metadata items as they are updated. Clients receive the full mapped metadata item.
   *
   *
   * **Returns:**
   * The updated metadata item mapped to the API type, or null if not found.
   */
  onMetadataItemUpdated?: Maybe<Item>
}

export type LibrarySectionsListQueryVariables = Exact<{
  first?: InputMaybe<Scalars['Int']['input']>
  after?: InputMaybe<Scalars['String']['input']>
  last?: InputMaybe<Scalars['Int']['input']>
  before?: InputMaybe<Scalars['String']['input']>
}>

export type LibrarySectionsListQuery = {
  __typename?: 'Query'
  librarySections?: {
    __typename?: 'LibrarySectionsConnection'
    nodes?: Array<{
      __typename?: 'LibrarySection'
      id: string
      name: string
      type: LibraryType
    }> | null
    pageInfo: {
      __typename?: 'PageInfo'
      hasNextPage: boolean
      hasPreviousPage: boolean
      startCursor?: string | null
      endCursor?: string | null
    }
  } | null
}

export type OnMetadataItemUpdatedSubscriptionVariables = Exact<{
  [key: string]: never
}>

export type OnMetadataItemUpdatedSubscription = {
  __typename?: 'Subscription'
  onMetadataItemUpdated?: {
    __typename?: 'Item'
    id: string
    title: string
    originalTitle: string
    year: number
    metadataType: MetadataType
    thumbUri?: string | null
  } | null
}

export type OnJobNotificationSubscriptionVariables = Exact<{
  [key: string]: never
}>

export type OnJobNotificationSubscription = {
  __typename?: 'Subscription'
  onJobNotification: {
    __typename?: 'JobNotification'
    id: string
    type: JobType
    librarySectionId?: number | null
    librarySectionName?: string | null
    description: string
    progressPercentage: number
    completedItems: number
    totalItems: number
    isActive: boolean
    timestamp: any
  }
}

export type ServerInfoQueryVariables = Exact<{ [key: string]: never }>

export type ServerInfoQuery = {
  __typename?: 'Query'
  serverInfo: {
    __typename?: 'ServerInfo'
    versionString: string
    isDevelopment: boolean
  }
}

export type AddLibrarySectionMutationVariables = Exact<{
  input: AddLibrarySectionInput
}>

export type AddLibrarySectionMutation = {
  __typename?: 'Mutation'
  addLibrarySection: {
    __typename?: 'AddLibrarySectionPayload'
    librarySection: {
      __typename?: 'LibrarySection'
      id: string
      name: string
      type: LibraryType
    }
  }
}

export type FileSystemRootsQueryVariables = Exact<{ [key: string]: never }>

export type FileSystemRootsQuery = {
  __typename?: 'Query'
  fileSystemRoots: Array<{
    __typename?: 'FileSystemRoot'
    id: string
    label: string
    path: string
    kind: FileSystemRootKind
    isReadOnly: boolean
  }>
}

export type BrowseDirectoryQueryVariables = Exact<{
  path: Scalars['String']['input']
}>

export type BrowseDirectoryQuery = {
  __typename?: 'Query'
  browseDirectory: {
    __typename?: 'DirectoryListing'
    currentPath: string
    parentPath?: string | null
    entries: Array<{
      __typename?: 'FileSystemEntry'
      name: string
      path: string
      isDirectory: boolean
      isFile: boolean
      isSymbolicLink: boolean
      isSelectable: boolean
    }>
  }
}

export type LibrarySectionQueryVariables = Exact<{
  contentSourceId: Scalars['ID']['input']
}>

export type LibrarySectionQuery = {
  __typename?: 'Query'
  librarySection?: {
    __typename?: 'LibrarySection'
    id: string
    name: string
    type: LibraryType
  } | null
}

export type LibrarySectionChildrenQueryVariables = Exact<{
  contentSourceId: Scalars['ID']['input']
  metadataType: MetadataType
  first?: InputMaybe<Scalars['Int']['input']>
  after?: InputMaybe<Scalars['String']['input']>
  last?: InputMaybe<Scalars['Int']['input']>
  before?: InputMaybe<Scalars['String']['input']>
}>

export type LibrarySectionChildrenQuery = {
  __typename?: 'Query'
  librarySection?: {
    __typename?: 'LibrarySection'
    id: string
    children?: {
      __typename?: 'ChildrenConnection'
      totalCount: number
      nodes?: Array<{
        __typename?: 'Item'
        id: string
        title: string
        year: number
        metadataType: MetadataType
        thumbUri?: string | null
        directPlayUrl?: string | null
        trickplayUrl?: string | null
      }> | null
      pageInfo: {
        __typename?: 'PageInfo'
        endCursor?: string | null
        startCursor?: string | null
        hasNextPage: boolean
        hasPreviousPage: boolean
      }
    } | null
  } | null
}

export type MediaQueryVariables = Exact<{
  id: Scalars['ID']['input']
}>

export type MediaQuery = {
  __typename?: 'Query'
  metadataItem?: {
    __typename?: 'Item'
    id: string
    title: string
    originalTitle: string
    thumbUri?: string | null
    metadataType: MetadataType
    directPlayUrl?: string | null
    trickplayUrl?: string | null
  } | null
}

export type ContentSourceQueryVariables = Exact<{
  contentSourceId: Scalars['ID']['input']
}>

export type ContentSourceQuery = {
  __typename?: 'Query'
  librarySection?: {
    __typename?: 'LibrarySection'
    id: string
    name: string
  } | null
}

export const LibrarySectionsListDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'query',
      name: { kind: 'Name', value: 'LibrarySectionsList' },
      variableDefinitions: [
        {
          kind: 'VariableDefinition',
          variable: {
            kind: 'Variable',
            name: { kind: 'Name', value: 'first' },
          },
          type: { kind: 'NamedType', name: { kind: 'Name', value: 'Int' } },
        },
        {
          kind: 'VariableDefinition',
          variable: {
            kind: 'Variable',
            name: { kind: 'Name', value: 'after' },
          },
          type: { kind: 'NamedType', name: { kind: 'Name', value: 'String' } },
        },
        {
          kind: 'VariableDefinition',
          variable: { kind: 'Variable', name: { kind: 'Name', value: 'last' } },
          type: { kind: 'NamedType', name: { kind: 'Name', value: 'Int' } },
        },
        {
          kind: 'VariableDefinition',
          variable: {
            kind: 'Variable',
            name: { kind: 'Name', value: 'before' },
          },
          type: { kind: 'NamedType', name: { kind: 'Name', value: 'String' } },
        },
      ],
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          {
            kind: 'Field',
            name: { kind: 'Name', value: 'librarySections' },
            arguments: [
              {
                kind: 'Argument',
                name: { kind: 'Name', value: 'first' },
                value: {
                  kind: 'Variable',
                  name: { kind: 'Name', value: 'first' },
                },
              },
              {
                kind: 'Argument',
                name: { kind: 'Name', value: 'after' },
                value: {
                  kind: 'Variable',
                  name: { kind: 'Name', value: 'after' },
                },
              },
              {
                kind: 'Argument',
                name: { kind: 'Name', value: 'last' },
                value: {
                  kind: 'Variable',
                  name: { kind: 'Name', value: 'last' },
                },
              },
              {
                kind: 'Argument',
                name: { kind: 'Name', value: 'before' },
                value: {
                  kind: 'Variable',
                  name: { kind: 'Name', value: 'before' },
                },
              },
            ],
            selectionSet: {
              kind: 'SelectionSet',
              selections: [
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'nodes' },
                  selectionSet: {
                    kind: 'SelectionSet',
                    selections: [
                      { kind: 'Field', name: { kind: 'Name', value: 'id' } },
                      { kind: 'Field', name: { kind: 'Name', value: 'name' } },
                      { kind: 'Field', name: { kind: 'Name', value: 'type' } },
                    ],
                  },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'pageInfo' },
                  selectionSet: {
                    kind: 'SelectionSet',
                    selections: [
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'hasNextPage' },
                      },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'hasPreviousPage' },
                      },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'startCursor' },
                      },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'endCursor' },
                      },
                    ],
                  },
                },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  LibrarySectionsListQuery,
  LibrarySectionsListQueryVariables
>
export const OnMetadataItemUpdatedDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'subscription',
      name: { kind: 'Name', value: 'OnMetadataItemUpdated' },
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          {
            kind: 'Field',
            name: { kind: 'Name', value: 'onMetadataItemUpdated' },
            selectionSet: {
              kind: 'SelectionSet',
              selections: [
                { kind: 'Field', name: { kind: 'Name', value: 'id' } },
                { kind: 'Field', name: { kind: 'Name', value: 'title' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'originalTitle' },
                },
                { kind: 'Field', name: { kind: 'Name', value: 'year' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'metadataType' },
                },
                { kind: 'Field', name: { kind: 'Name', value: 'thumbUri' } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  OnMetadataItemUpdatedSubscription,
  OnMetadataItemUpdatedSubscriptionVariables
>
export const OnJobNotificationDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'subscription',
      name: { kind: 'Name', value: 'OnJobNotification' },
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          {
            kind: 'Field',
            name: { kind: 'Name', value: 'onJobNotification' },
            selectionSet: {
              kind: 'SelectionSet',
              selections: [
                { kind: 'Field', name: { kind: 'Name', value: 'id' } },
                { kind: 'Field', name: { kind: 'Name', value: 'type' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'librarySectionId' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'librarySectionName' },
                },
                { kind: 'Field', name: { kind: 'Name', value: 'description' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'progressPercentage' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'completedItems' },
                },
                { kind: 'Field', name: { kind: 'Name', value: 'totalItems' } },
                { kind: 'Field', name: { kind: 'Name', value: 'isActive' } },
                { kind: 'Field', name: { kind: 'Name', value: 'timestamp' } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  OnJobNotificationSubscription,
  OnJobNotificationSubscriptionVariables
>
export const ServerInfoDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'query',
      name: { kind: 'Name', value: 'ServerInfo' },
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          {
            kind: 'Field',
            name: { kind: 'Name', value: 'serverInfo' },
            selectionSet: {
              kind: 'SelectionSet',
              selections: [
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'versionString' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'isDevelopment' },
                },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<ServerInfoQuery, ServerInfoQueryVariables>
export const AddLibrarySectionDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'mutation',
      name: { kind: 'Name', value: 'AddLibrarySection' },
      variableDefinitions: [
        {
          kind: 'VariableDefinition',
          variable: {
            kind: 'Variable',
            name: { kind: 'Name', value: 'input' },
          },
          type: {
            kind: 'NonNullType',
            type: {
              kind: 'NamedType',
              name: { kind: 'Name', value: 'AddLibrarySectionInput' },
            },
          },
        },
      ],
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          {
            kind: 'Field',
            name: { kind: 'Name', value: 'addLibrarySection' },
            arguments: [
              {
                kind: 'Argument',
                name: { kind: 'Name', value: 'input' },
                value: {
                  kind: 'Variable',
                  name: { kind: 'Name', value: 'input' },
                },
              },
            ],
            selectionSet: {
              kind: 'SelectionSet',
              selections: [
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'librarySection' },
                  selectionSet: {
                    kind: 'SelectionSet',
                    selections: [
                      { kind: 'Field', name: { kind: 'Name', value: 'id' } },
                      { kind: 'Field', name: { kind: 'Name', value: 'name' } },
                      { kind: 'Field', name: { kind: 'Name', value: 'type' } },
                    ],
                  },
                },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  AddLibrarySectionMutation,
  AddLibrarySectionMutationVariables
>
export const FileSystemRootsDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'query',
      name: { kind: 'Name', value: 'FileSystemRoots' },
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          {
            kind: 'Field',
            name: { kind: 'Name', value: 'fileSystemRoots' },
            selectionSet: {
              kind: 'SelectionSet',
              selections: [
                { kind: 'Field', name: { kind: 'Name', value: 'id' } },
                { kind: 'Field', name: { kind: 'Name', value: 'label' } },
                { kind: 'Field', name: { kind: 'Name', value: 'path' } },
                { kind: 'Field', name: { kind: 'Name', value: 'kind' } },
                { kind: 'Field', name: { kind: 'Name', value: 'isReadOnly' } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  FileSystemRootsQuery,
  FileSystemRootsQueryVariables
>
export const BrowseDirectoryDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'query',
      name: { kind: 'Name', value: 'BrowseDirectory' },
      variableDefinitions: [
        {
          kind: 'VariableDefinition',
          variable: { kind: 'Variable', name: { kind: 'Name', value: 'path' } },
          type: {
            kind: 'NonNullType',
            type: {
              kind: 'NamedType',
              name: { kind: 'Name', value: 'String' },
            },
          },
        },
      ],
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          {
            kind: 'Field',
            name: { kind: 'Name', value: 'browseDirectory' },
            arguments: [
              {
                kind: 'Argument',
                name: { kind: 'Name', value: 'path' },
                value: {
                  kind: 'Variable',
                  name: { kind: 'Name', value: 'path' },
                },
              },
            ],
            selectionSet: {
              kind: 'SelectionSet',
              selections: [
                { kind: 'Field', name: { kind: 'Name', value: 'currentPath' } },
                { kind: 'Field', name: { kind: 'Name', value: 'parentPath' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'entries' },
                  selectionSet: {
                    kind: 'SelectionSet',
                    selections: [
                      { kind: 'Field', name: { kind: 'Name', value: 'name' } },
                      { kind: 'Field', name: { kind: 'Name', value: 'path' } },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'isDirectory' },
                      },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'isFile' },
                      },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'isSymbolicLink' },
                      },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'isSelectable' },
                      },
                    ],
                  },
                },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  BrowseDirectoryQuery,
  BrowseDirectoryQueryVariables
>
export const LibrarySectionDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'query',
      name: { kind: 'Name', value: 'LibrarySection' },
      variableDefinitions: [
        {
          kind: 'VariableDefinition',
          variable: {
            kind: 'Variable',
            name: { kind: 'Name', value: 'contentSourceId' },
          },
          type: {
            kind: 'NonNullType',
            type: { kind: 'NamedType', name: { kind: 'Name', value: 'ID' } },
          },
        },
      ],
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          {
            kind: 'Field',
            name: { kind: 'Name', value: 'librarySection' },
            arguments: [
              {
                kind: 'Argument',
                name: { kind: 'Name', value: 'id' },
                value: {
                  kind: 'Variable',
                  name: { kind: 'Name', value: 'contentSourceId' },
                },
              },
            ],
            selectionSet: {
              kind: 'SelectionSet',
              selections: [
                { kind: 'Field', name: { kind: 'Name', value: 'id' } },
                { kind: 'Field', name: { kind: 'Name', value: 'name' } },
                { kind: 'Field', name: { kind: 'Name', value: 'type' } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<LibrarySectionQuery, LibrarySectionQueryVariables>
export const LibrarySectionChildrenDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'query',
      name: { kind: 'Name', value: 'LibrarySectionChildren' },
      variableDefinitions: [
        {
          kind: 'VariableDefinition',
          variable: {
            kind: 'Variable',
            name: { kind: 'Name', value: 'contentSourceId' },
          },
          type: {
            kind: 'NonNullType',
            type: { kind: 'NamedType', name: { kind: 'Name', value: 'ID' } },
          },
        },
        {
          kind: 'VariableDefinition',
          variable: {
            kind: 'Variable',
            name: { kind: 'Name', value: 'metadataType' },
          },
          type: {
            kind: 'NonNullType',
            type: {
              kind: 'NamedType',
              name: { kind: 'Name', value: 'MetadataType' },
            },
          },
        },
        {
          kind: 'VariableDefinition',
          variable: {
            kind: 'Variable',
            name: { kind: 'Name', value: 'first' },
          },
          type: { kind: 'NamedType', name: { kind: 'Name', value: 'Int' } },
        },
        {
          kind: 'VariableDefinition',
          variable: {
            kind: 'Variable',
            name: { kind: 'Name', value: 'after' },
          },
          type: { kind: 'NamedType', name: { kind: 'Name', value: 'String' } },
        },
        {
          kind: 'VariableDefinition',
          variable: { kind: 'Variable', name: { kind: 'Name', value: 'last' } },
          type: { kind: 'NamedType', name: { kind: 'Name', value: 'Int' } },
        },
        {
          kind: 'VariableDefinition',
          variable: {
            kind: 'Variable',
            name: { kind: 'Name', value: 'before' },
          },
          type: { kind: 'NamedType', name: { kind: 'Name', value: 'String' } },
        },
      ],
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          {
            kind: 'Field',
            name: { kind: 'Name', value: 'librarySection' },
            arguments: [
              {
                kind: 'Argument',
                name: { kind: 'Name', value: 'id' },
                value: {
                  kind: 'Variable',
                  name: { kind: 'Name', value: 'contentSourceId' },
                },
              },
            ],
            selectionSet: {
              kind: 'SelectionSet',
              selections: [
                { kind: 'Field', name: { kind: 'Name', value: 'id' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'children' },
                  arguments: [
                    {
                      kind: 'Argument',
                      name: { kind: 'Name', value: 'metadataType' },
                      value: {
                        kind: 'Variable',
                        name: { kind: 'Name', value: 'metadataType' },
                      },
                    },
                    {
                      kind: 'Argument',
                      name: { kind: 'Name', value: 'first' },
                      value: {
                        kind: 'Variable',
                        name: { kind: 'Name', value: 'first' },
                      },
                    },
                    {
                      kind: 'Argument',
                      name: { kind: 'Name', value: 'after' },
                      value: {
                        kind: 'Variable',
                        name: { kind: 'Name', value: 'after' },
                      },
                    },
                    {
                      kind: 'Argument',
                      name: { kind: 'Name', value: 'last' },
                      value: {
                        kind: 'Variable',
                        name: { kind: 'Name', value: 'last' },
                      },
                    },
                    {
                      kind: 'Argument',
                      name: { kind: 'Name', value: 'before' },
                      value: {
                        kind: 'Variable',
                        name: { kind: 'Name', value: 'before' },
                      },
                    },
                    {
                      kind: 'Argument',
                      name: { kind: 'Name', value: 'order' },
                      value: {
                        kind: 'ObjectValue',
                        fields: [
                          {
                            kind: 'ObjectField',
                            name: { kind: 'Name', value: 'title' },
                            value: { kind: 'EnumValue', value: 'ASC' },
                          },
                        ],
                      },
                    },
                  ],
                  selectionSet: {
                    kind: 'SelectionSet',
                    selections: [
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'nodes' },
                        selectionSet: {
                          kind: 'SelectionSet',
                          selections: [
                            {
                              kind: 'Field',
                              name: { kind: 'Name', value: 'id' },
                            },
                            {
                              kind: 'Field',
                              name: { kind: 'Name', value: 'title' },
                            },
                            {
                              kind: 'Field',
                              name: { kind: 'Name', value: 'year' },
                            },
                            {
                              kind: 'Field',
                              name: { kind: 'Name', value: 'metadataType' },
                            },
                            {
                              kind: 'Field',
                              name: { kind: 'Name', value: 'thumbUri' },
                            },
                            {
                              kind: 'Field',
                              name: { kind: 'Name', value: 'directPlayUrl' },
                            },
                            {
                              kind: 'Field',
                              name: { kind: 'Name', value: 'trickplayUrl' },
                            },
                          ],
                        },
                      },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'pageInfo' },
                        selectionSet: {
                          kind: 'SelectionSet',
                          selections: [
                            {
                              kind: 'Field',
                              name: { kind: 'Name', value: 'endCursor' },
                            },
                            {
                              kind: 'Field',
                              name: { kind: 'Name', value: 'startCursor' },
                            },
                            {
                              kind: 'Field',
                              name: { kind: 'Name', value: 'hasNextPage' },
                            },
                            {
                              kind: 'Field',
                              name: { kind: 'Name', value: 'hasPreviousPage' },
                            },
                          ],
                        },
                      },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'totalCount' },
                      },
                    ],
                  },
                },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  LibrarySectionChildrenQuery,
  LibrarySectionChildrenQueryVariables
>
export const MediaDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'query',
      name: { kind: 'Name', value: 'Media' },
      variableDefinitions: [
        {
          kind: 'VariableDefinition',
          variable: { kind: 'Variable', name: { kind: 'Name', value: 'id' } },
          type: {
            kind: 'NonNullType',
            type: { kind: 'NamedType', name: { kind: 'Name', value: 'ID' } },
          },
        },
      ],
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          {
            kind: 'Field',
            name: { kind: 'Name', value: 'metadataItem' },
            arguments: [
              {
                kind: 'Argument',
                name: { kind: 'Name', value: 'id' },
                value: {
                  kind: 'Variable',
                  name: { kind: 'Name', value: 'id' },
                },
              },
            ],
            selectionSet: {
              kind: 'SelectionSet',
              selections: [
                { kind: 'Field', name: { kind: 'Name', value: 'id' } },
                { kind: 'Field', name: { kind: 'Name', value: 'title' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'originalTitle' },
                },
                { kind: 'Field', name: { kind: 'Name', value: 'thumbUri' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'metadataType' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'directPlayUrl' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'trickplayUrl' },
                },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<MediaQuery, MediaQueryVariables>
export const ContentSourceDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'query',
      name: { kind: 'Name', value: 'ContentSource' },
      variableDefinitions: [
        {
          kind: 'VariableDefinition',
          variable: {
            kind: 'Variable',
            name: { kind: 'Name', value: 'contentSourceId' },
          },
          type: {
            kind: 'NonNullType',
            type: { kind: 'NamedType', name: { kind: 'Name', value: 'ID' } },
          },
        },
      ],
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          {
            kind: 'Field',
            name: { kind: 'Name', value: 'librarySection' },
            arguments: [
              {
                kind: 'Argument',
                name: { kind: 'Name', value: 'id' },
                value: {
                  kind: 'Variable',
                  name: { kind: 'Name', value: 'contentSourceId' },
                },
              },
            ],
            selectionSet: {
              kind: 'SelectionSet',
              selections: [
                { kind: 'Field', name: { kind: 'Name', value: 'id' } },
                { kind: 'Field', name: { kind: 'Name', value: 'name' } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<ContentSourceQuery, ContentSourceQueryVariables>
