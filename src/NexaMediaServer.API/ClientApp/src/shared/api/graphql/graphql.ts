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
  Any: { input: any; output: any }
  /** The `DateTime` scalar represents an exact point in time. This point in time is specified by having an offset to UTC and does not use a time zone. */
  DateTime: { input: Date; output: Date }
  /** The `LocalDate` scalar represents a date without a time-zone in the ISO-8601 calendar system. */
  LocalDate: { input: string; output: string }
  /** The `Long` scalar type represents non-fractional signed whole 64-bit numeric values. Long can represent values between -(2^63) and 2^63 - 1. */
  Long: { input: BigInt; output: BigInt }
  UUID: { input: string; output: string }
  /** The `Upload` scalar type represents a file upload. */
  Upload: { input: File; output: File }
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

/** Input for triggering file analysis, GoP-index generation, and trickplay generation for a metadata item. */
export type AnalyzeItemInput = {
  /** Gets or sets the metadata item identifier. */
  itemId: Scalars['ID']['input']
}

/** GraphQL payload indicating the outcome of an analyze item request. */
export type AnalyzeItemPayload = {
  __typename?: 'AnalyzeItemPayload'
  /** Gets an optional error description for failed requests. */
  error?: Maybe<Scalars['String']['output']>
  query: Query
  /** Gets a value indicating whether the request succeeded. */
  success: Scalars['Boolean']['output']
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

/** Represents an available root item type option for browsing a library section. */
export type BrowsableItemType = {
  __typename?: 'BrowsableItemType'
  /** Gets the user-facing display name for this item type. */
  displayName: Scalars['String']['output']
  /**
   * Gets the metadata types that this option represents.
   * When multiple types are present (e.g., Person and Group for Artists),
   * items of any of these types will be included.
   */
  metadataTypes: Array<MetadataType>
}

/** A segment of a collection. */
export type ChildrenCollectionSegment = {
  __typename?: 'ChildrenCollectionSegment'
  /** A flattened list of the items. */
  items?: Maybe<Array<Item>>
  /** Information to aid in pagination. */
  pageInfo: CollectionSegmentInfo
  totalCount: Scalars['Int']['output']
}

/** GraphQL input describing codec-level constraints. */
export type CodecProfileInput = {
  /** Gets or sets the codec this profile applies to, if limited. */
  codec?: InputMaybe<Scalars['String']['input']>
  /** Gets or sets the conditions that must be satisfied. */
  conditions: Array<ProfileConditionInput>
  /** Gets or sets the container restriction, if any. */
  container?: InputMaybe<Scalars['String']['input']>
  /** Gets or sets the media type (Video/Audio/Photo). */
  type: Scalars['String']['input']
}

/** Information about the offset pagination. */
export type CollectionSegmentInfo = {
  __typename?: 'CollectionSegmentInfo'
  /** Indicates whether more items exist following the set defined by the clients arguments. */
  hasNextPage: Scalars['Boolean']['output']
  /** Indicates whether more items exist prior the set defined by the clients arguments. */
  hasPreviousPage: Scalars['Boolean']['output']
}

/** GraphQL input describing container-specific constraints. */
export type ContainerProfileInput = {
  /** Gets or sets the conditions that gate support. */
  conditions: Array<ProfileConditionInput>
  /** Gets or sets the media type (Video/Audio/Photo). */
  type: Scalars['String']['input']
}

/** Input type for creating a custom field definition. */
export type CreateCustomFieldDefinitionInput = {
  /**
   * Gets or sets the metadata types this field applies to.
   * Empty list means the field applies to all metadata types.
   */
  applicableMetadataTypes?: InputMaybe<Array<MetadataType>>
  /** Gets or sets the unique key identifier for this field. */
  key: Scalars['String']['input']
  /** Gets or sets the display label for this field. */
  label: Scalars['String']['input']
  /** Gets or sets the display order of this field. */
  sortOrder: Scalars['Int']['input']
  /** Gets or sets the widget type for rendering this field. */
  widget: DetailFieldWidgetType
}

/** Represents a custom field definition for the GraphQL API. */
export type CustomFieldDefinition = {
  __typename?: 'CustomFieldDefinition'
  /** Gets the metadata types this field applies to. */
  applicableMetadataTypes: Array<MetadataType>
  /** Gets the unique identifier of the custom field definition. */
  id: Scalars['ID']['output']
  /** Gets a value indicating whether this field is enabled. */
  isEnabled: Scalars['Boolean']['output']
  /** Gets the unique key identifier for this field. */
  key: Scalars['String']['output']
  /** Gets the display label for this field. */
  label: Scalars['String']['output']
  /** Gets the display order of this field. */
  sortOrder: Scalars['Int']['output']
  /** Gets the widget type for rendering this field. */
  widget: DetailFieldWidgetType
}

/** GraphQL representation of a detail field configuration. */
export type DetailFieldConfiguration = {
  __typename?: 'DetailFieldConfiguration'
  /** Gets or sets the list of disabled custom field keys. */
  disabledCustomFieldKeys: Array<Scalars['String']['output']>
  /** Gets or sets the list of disabled field types. */
  disabledFieldTypes: Array<DetailFieldType>
  /** Gets or sets the ordered list of enabled fields. */
  enabledFieldTypes: Array<DetailFieldType>
  /** Gets or sets the field-to-group assignments as key-value pairs. */
  fieldGroupAssignments?: Maybe<Array<KeyValuePairOfStringAndString>>
  /** Gets or sets the list of field group definitions. */
  fieldGroups?: Maybe<Array<DetailFieldGroup>>
  /** Gets or sets the optional library section identifier when scoped. */
  librarySectionId?: Maybe<Scalars['ID']['output']>
  /** Gets or sets the metadata type this configuration targets. */
  metadataType: MetadataType
}

/** Input describing the scope of a detail field configuration lookup. */
export type DetailFieldConfigurationScopeInput = {
  /** Gets or sets the optional library section identifier for scoped overrides. */
  librarySectionId?: InputMaybe<Scalars['ID']['input']>
  /** Gets or sets the metadata type the configuration applies to. */
  metadataType: MetadataType
}

/** Represents a field definition for the GraphQL API. */
export type DetailFieldDefinition = {
  __typename?: 'DetailFieldDefinition'
  /** Gets the custom field key for Custom field types. */
  customFieldKey?: Maybe<Scalars['String']['output']>
  /** Gets the type of field (e.g., Title, Summary, Custom, etc.). */
  fieldType: DetailFieldType
  /** Gets the key of the group this field belongs to. */
  groupKey?: Maybe<Scalars['String']['output']>
  /** Gets the unique key identifying this field definition. */
  key: Scalars['String']['output']
  /** Gets the display label for this field. */
  label: Scalars['String']['output']
  /** Gets the display order of this field. */
  sortOrder: Scalars['Int']['output']
  /** Gets the recommended widget type for client-side rendering. */
  widget: DetailFieldWidgetType
}

/** Represents a field group definition for the GraphQL API. */
export type DetailFieldGroup = {
  __typename?: 'DetailFieldGroup'
  /** Gets the unique key identifying this group. */
  groupKey: Scalars['String']['output']
  /** Gets a value indicating whether this group can be collapsed by the user. */
  isCollapsible: Scalars['Boolean']['output']
  /** Gets the display label for this group. */
  label: Scalars['String']['output']
  /** Gets the layout type for rendering fields within this group. */
  layoutType: DetailFieldGroupLayoutType
  /** Gets the display order of this group. */
  sortOrder: Scalars['Int']['output']
}

/** Input for creating or updating a detail field group. */
export type DetailFieldGroupInput = {
  /** Gets or sets the unique key identifying this group. */
  groupKey: Scalars['String']['input']
  /** Gets or sets a value indicating whether this group can be collapsed by the user. */
  isCollapsible: Scalars['Boolean']['input']
  /** Gets or sets the display label for this group. */
  label: Scalars['String']['input']
  /** Gets or sets the layout type for rendering fields within this group. */
  layoutType: DetailFieldGroupLayoutType
  /** Gets or sets the display order of this group. */
  sortOrder: Scalars['Int']['input']
}

/** Defines the layout type for field groups on item detail pages. */
export enum DetailFieldGroupLayoutType {
  /** Fields are arranged in a responsive grid layout. */
  Grid = 'GRID',
  /** Fields are arranged horizontally in a single row. */
  Horizontal = 'HORIZONTAL',
  /** Fields are arranged vertically in a single column. */
  Vertical = 'VERTICAL',
}

/** Represents the type of field displayed on item detail pages. */
export enum DetailFieldType {
  /**
   * The actions button block (Play, Edit, Menu, etc.).
   * This is a non-configurable placeholder; the client determines which buttons to show.
   */
  Actions = 'ACTIONS',
  /** The content rating (e.g., "PG-13", "TV-MA"). */
  ContentRating = 'CONTENT_RATING',
  /** A custom field defined by an administrator, stored in ExtraFields. */
  Custom = 'CUSTOM',
  /** External identifiers (TMDB, TVDB, IMDb, etc.). */
  ExternalIds = 'EXTERNAL_IDS',
  /** The genres associated with the item. */
  Genres = 'GENRES',
  /** The original title in the original language. */
  OriginalTitle = 'ORIGINAL_TITLE',
  /** The release date of the item. */
  ReleaseDate = 'RELEASE_DATE',
  /** The runtime/duration of the item. */
  Runtime = 'RUNTIME',
  /** The summary or description text. */
  Summary = 'SUMMARY',
  /** The tagline or slogan. */
  Tagline = 'TAGLINE',
  /** The tags associated with the item. */
  Tags = 'TAGS',
  /** The display title of the item. */
  Title = 'TITLE',
  /** The release year of the item. */
  Year = 'YEAR',
}

/** Represents the widget type for rendering a field on the client. */
export enum DetailFieldWidgetType {
  /**
   * The actions button block (Play, Edit, Menu, etc.).
   * The client determines which buttons to render based on user role and item capabilities.
   */
  Actions = 'ACTIONS',
  /** A badge/pill display (e.g., content rating). */
  Badge = 'BADGE',
  /** A boolean/toggle display. */
  Boolean = 'BOOLEAN',
  /** A formatted date display. */
  Date = 'DATE',
  /** A formatted duration display (e.g., "2h 15m"). */
  Duration = 'DURATION',
  /** A heading/title display (typically larger, bold text). */
  Heading = 'HEADING',
  /** A clickable link. */
  Link = 'LINK',
  /** A list of items (e.g., genres, tags). */
  List = 'LIST',
  /** A numeric value display. */
  Number = 'NUMBER',
  /** Plain text display. */
  Text = 'TEXT',
}

/** GraphQL input describing a direct-play capability. */
export type DirectPlayProfileInput = {
  /** Gets or sets the supported audio codec, if constrained. */
  audioCodec?: InputMaybe<Scalars['String']['input']>
  /** Gets or sets the supported container(s). */
  container: Scalars['String']['input']
  /** Gets or sets the media type (Video/Audio/Photo). */
  type: Scalars['String']['input']
  /** Gets or sets the supported video codec, if constrained. */
  videoCodec?: InputMaybe<Scalars['String']['input']>
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

/** Represents an external identifier from a metadata provider. */
export type ExternalId = {
  __typename?: 'ExternalId'
  /** The provider name (e.g., "tmdb", "imdb", "tvdb"). */
  provider: Scalars['String']['output']
  /** The identifier value from the provider. */
  value: Scalars['String']['output']
}

/** Input for an external identifier. */
export type ExternalIdInput = {
  /** The provider name (e.g., "tmdb", "imdb", "tvdb"). */
  provider: Scalars['String']['input']
  /** The identifier value from the provider. */
  value?: InputMaybe<Scalars['String']['input']>
}

/** Represents a key-value pair for extra fields in the GraphQL API. */
export type ExtraField = {
  __typename?: 'ExtraField'
  /** Gets the field key. */
  key: Scalars['String']['output']
  /** Gets the field value as a JSON element. */
  value?: Maybe<Scalars['Any']['output']>
}

/** Input type for setting an extra field value on a metadata item. */
export type ExtraFieldInput = {
  /** Gets or sets the field key. */
  key: Scalars['String']['input']
  /** Gets or sets the field value as a JSON element. */
  value?: InputMaybe<Scalars['Any']['input']>
}

/** Payload containing detected FFmpeg capabilities and recommendations. */
export type FfmpegCapabilitiesPayload = {
  __typename?: 'FfmpegCapabilitiesPayload'
  /** Whether capability detection has completed. */
  isDetected: Scalars['Boolean']['output']
  /** The recommended hardware acceleration for this platform. */
  recommendedAcceleration: HardwareAccelerationKind
  /** List of all supported encoders. */
  supportedEncoders: Array<Scalars['String']['output']>
  /** List of all supported filters. */
  supportedFilters: Array<Scalars['String']['output']>
  /** List of supported hardware acceleration types. */
  supportedHardwareAccelerators: Array<HardwareAccelerationKind>
  /** The detected FFmpeg version string. */
  version: Scalars['String']['output']
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

/** Input for fetching hub items. */
export type GetHubItemsInput = {
  /** Gets the hub context. */
  context: HubContext
  /** Gets the optional filter value for filtered hubs. */
  filterValue?: InputMaybe<Scalars['String']['input']>
  /** Gets the type of hub. */
  hubType: HubType
  /** Gets the optional library section ID for library-specific hubs. */
  librarySectionId?: InputMaybe<Scalars['ID']['input']>
  /** Gets the optional metadata item ID for detail page hubs. */
  metadataItemId?: InputMaybe<Scalars['ID']['input']>
}

/** Hardware acceleration modes supported by the server configuration. */
export enum HardwareAccelerationKind {
  /** AMD Advanced Media Framework (Windows). */
  Amf = 'AMF',
  /** No hardware acceleration is used. */
  None = 'NONE',
  /** NVIDIA NVENC/NVDEC. */
  Nvenc = 'NVENC',
  /** Intel Quick Sync Video. */
  Qsv = 'QSV',
  /** Rockchip Media Process Platform (Rockchip SoCs). */
  Rkmpp = 'RKMPP',
  /** Video4Linux2 Memory-to-Memory (Raspberry Pi/ARM). */
  V4L2M2M = 'V4L2M2M',
  /** VAAPI acceleration (Linux GPUs supporting VA-API). */
  Vaapi = 'VAAPI',
  /** Apple VideoToolbox (macOS/iOS). */
  VideoToolbox = 'VIDEO_TOOLBOX',
}

/** GraphQL representation of a hub configuration. */
export type HubConfiguration = {
  __typename?: 'HubConfiguration'
  /** Gets or sets the list of disabled hub types. */
  disabledHubTypes: Array<HubType>
  /** Gets or sets the ordered list of enabled hub types. */
  enabledHubTypes: Array<HubType>
}

/** Input describing the scope of a hub configuration lookup. */
export type HubConfigurationScopeInput = {
  /** Gets or sets the hub context being configured. */
  context: HubContext
  /** Gets or sets the optional library section identifier for discover/detail scopes. */
  librarySectionId?: InputMaybe<Scalars['ID']['input']>
  /** Gets or sets the optional metadata type for item detail configurations. */
  metadataType?: InputMaybe<MetadataType>
}

/** Represents the context in which a hub is displayed. */
export enum HubContext {
  /** Hub is displayed on the global home page, aggregating content from all user-accessible libraries. */
  Home = 'HOME',
  /** Hub is displayed on a metadata item's detail page, showing related content. */
  ItemDetail = 'ITEM_DETAIL',
  /** Hub is displayed on the library-specific discover page. */
  LibraryDiscover = 'LIBRARY_DISCOVER',
}

/** Represents a hub definition for the GraphQL API. */
export type HubDefinition = {
  __typename?: 'HubDefinition'
  /** Gets the context ID (e.g., parent item ID for detail page hubs). */
  contextId?: Maybe<Scalars['ID']['output']>
  /** Gets the optional filter value for this hub (e.g., genre name, director name). */
  filterValue?: Maybe<Scalars['String']['output']>
  /** Gets the unique key identifying this hub definition. */
  key: Scalars['String']['output']
  /** Gets the library section ID this hub is scoped to, if any. */
  librarySectionId?: Maybe<Scalars['ID']['output']>
  /** Gets the metadata type of items in this hub. */
  metadataType: MetadataType
  /** Gets the display title for this hub. */
  title: Scalars['String']['output']
  /** Gets the type of hub (e.g., RecentlyAdded, TopRated, etc.). */
  type: HubType
  /** Gets the recommended widget type for client-side rendering. */
  widget: HubWidgetType
}

/** Represents the type of hub, defining the content and logic for populating it. */
export enum HubType {
  /** Album releases within an album release group. */
  AlbumReleases = 'ALBUM_RELEASES',
  /** Cast members for an item. */
  Cast = 'CAST',
  /** Items currently being watched (has view offset but not completed). */
  ContinueWatching = 'CONTINUE_WATCHING',
  /** Crew members for an item. */
  Crew = 'CREW',
  /** Extras associated with an item (trailers, behind-the-scenes, etc.). */
  Extras = 'EXTRAS',
  /** More items from the same artist. */
  MoreFromArtist = 'MORE_FROM_ARTIST',
  /** More items from the same director. */
  MoreFromDirector = 'MORE_FROM_DIRECTOR',
  /** The next episode to watch in a series (On Deck). */
  OnDeck = 'ON_DECK',
  /** Photos or pictures within a PhotoAlbum or PictureSet. */
  Photos = 'PHOTOS',
  /** Admin-promoted items for the hero carousel, backfilled with recently added items. */
  Promoted = 'PROMOTED',
  /** Items recently added to the library. */
  RecentlyAdded = 'RECENTLY_ADDED',
  /** Recently aired episodes. */
  RecentlyAired = 'RECENTLY_AIRED',
  /** Items recently played/listened to. */
  RecentlyPlayed = 'RECENTLY_PLAYED',
  /** Items recently released (by release date). */
  RecentlyReleased = 'RECENTLY_RELEASED',
  /** Items from the same collection. */
  RelatedCollection = 'RELATED_COLLECTION',
  /** Similar items based on genre/tags. */
  SimilarItems = 'SIMILAR_ITEMS',
  /** Top items by a specific artist. */
  TopByArtist = 'TOP_BY_ARTIST',
  /** Top items by a specific director. */
  TopByDirector = 'TOP_BY_DIRECTOR',
  /** Top items filtered by a specific genre. */
  TopByGenre = 'TOP_BY_GENRE',
  /** Tracks from an album release, grouped by medium/disc. */
  Tracks = 'TRACKS',
}

/** Represents the recommended widget type for rendering a hub on the client. */
export enum HubWidgetType {
  /** A grid layout for displaying photos or pictures. */
  Grid = 'GRID',
  /** A large hero carousel with backdrop images, logos, and rich metadata. */
  Hero = 'HERO',
  /** A horizontal slider of items or people cards. */
  Slider = 'SLIDER',
  /** A timeline list of items ordered from most recent to least recent. */
  Timeline = 'TIMELINE',
  /** A vertical tracklist of audio tracks grouped by medium/disc. */
  Tracklist = 'TRACKLIST',
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
  /** Gets the ThumbHash placeholder for the backdrop. */
  artHash?: Maybe<Scalars['String']['output']>
  /** Gets the backdrop URL of the metadata item. */
  artUri?: Maybe<Scalars['String']['output']>
  /** Gets the number of child items in the metadata item. */
  childCount: Scalars['Int']['output']
  /**
   * Gets an offset-paginated list of child metadata items for this item.
   * Useful for PhotoAlbum, PictureSet, Season, and other container types.
   *
   *
   * **Returns:**
   * An in-memory queryable used by HotChocolate to create a collection segment.
   */
  children?: Maybe<ChildrenCollectionSegment>
  /** Gets the content rating of the metadata item. */
  contentRating: Scalars['String']['output']
  /** Gets an optional context-specific string (e.g., role name for people in hubs). */
  context?: Maybe<Scalars['String']['output']>
  /**
   * Resolves external identifiers (TMDB, IMDb, TVDB, etc.) for the metadata item.
   *
   *
   * **Returns:**
   * A list of external identifiers.
   */
  externalIds: Array<ExternalId>
  /**
   * Gets the extra fields associated with this metadata item.
   *
   *
   * **Returns:**
   * A list of extra field key-value pairs.
   */
  extraFields: Array<ExtraField>
  /** Gets the list of genres associated with this metadata item. */
  genres: Array<Scalars['String']['output']>
  /** Gets the global Relay-compatible identifier of the metadata item. */
  id: Scalars['ID']['output']
  /** Gets the index of the metadata item. */
  index: Scalars['Int']['output']
  /** Gets a value indicating whether this item is promoted (featured in the Promoted hub). */
  isPromoted: Scalars['Boolean']['output']
  /** Gets the number of leaf items in the metadata item. */
  leafCount: Scalars['Int']['output']
  /** Gets the length of the metadata item in milliseconds. */
  length: Scalars['Int']['output']
  /** Gets the owning library section identifier (Relay GUID). */
  librarySectionId: Scalars['ID']['output']
  /** Gets the list of field names that are locked from automatic updates. */
  lockedFields: Array<Scalars['String']['output']>
  /** Gets the ThumbHash placeholder for the logo. */
  logoHash?: Maybe<Scalars['String']['output']>
  /** Gets the logo URL of the metadata item. */
  logoUri?: Maybe<Scalars['String']['output']>
  /** Gets the type of the metadata item. */
  metadataType: MetadataType
  /** Gets the original title of the metadata item. */
  originalTitle: Scalars['String']['output']
  /** Gets the date the metadata item was originally available. */
  originallyAvailableAt?: Maybe<Scalars['LocalDate']['output']>
  /**
   * Resolves the parent metadata item when available.
   *
   *
   * **Returns:**
   * The parent metadata item or null if none exists.
   */
  parent?: Maybe<Item>
  /**
   * Resolves all persons and groups for a music track.
   *
   *
   * **Returns:**
   * The list of persons and groups.
   */
  persons: Array<Item>
  /**
   * Resolves the primary person or group for a music album.
   * For albums, this looks at persons linked to child tracks and returns the first one.
   *
   *
   * **Returns:**
   * The primary person or group, or null if none exists.
   */
  primaryPerson?: Maybe<Item>
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
  /** Gets the list of tags associated with this metadata item. */
  tags: Array<Scalars['String']['output']>
  /** Gets the theme URL of the metadata item. */
  themeUrl?: Maybe<Scalars['String']['output']>
  /** Gets the ThumbHash placeholder for the thumbnail. */
  thumbHash?: Maybe<Scalars['String']['output']>
  /** Gets the thumbnail URL of the metadata item. */
  thumbUri?: Maybe<Scalars['String']['output']>
  /** Gets the title of the metadata item. */
  title: Scalars['String']['output']
  /** Gets the sortable title of the metadata item. */
  titleSort: Scalars['String']['output']
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
export type ItemChildrenArgs = {
  order?: InputMaybe<Array<ItemSortInput>>
  skip?: InputMaybe<Scalars['Int']['input']>
  take?: InputMaybe<Scalars['Int']['input']>
  where?: InputMaybe<ItemFilterInput>
}

/** Representation of a metadata item for pagination queries. */
export type ItemFilterInput = {
  and?: InputMaybe<Array<ItemFilterInput>>
  /** Gets the ThumbHash placeholder for the backdrop. */
  artHash?: InputMaybe<StringOperationFilterInput>
  /** Gets the backdrop URL of the metadata item. */
  artUri?: InputMaybe<StringOperationFilterInput>
  /** Gets the number of child items in the metadata item. */
  childCount?: InputMaybe<IntOperationFilterInput>
  /** Gets the content rating of the metadata item. */
  contentRating?: InputMaybe<StringOperationFilterInput>
  /** Gets an optional context-specific string (e.g., role name for people in hubs). */
  context?: InputMaybe<StringOperationFilterInput>
  /** Gets the list of genres associated with this metadata item. */
  genres?: InputMaybe<ListStringOperationFilterInput>
  /** Gets the global Relay-compatible identifier of the metadata item. */
  id?: InputMaybe<IdOperationFilterInput>
  /** Gets the index of the metadata item. */
  index?: InputMaybe<IntOperationFilterInput>
  /** Gets a value indicating whether this item is promoted (featured in the Promoted hub). */
  isPromoted?: InputMaybe<BooleanOperationFilterInput>
  /** Gets the number of leaf items in the metadata item. */
  leafCount?: InputMaybe<IntOperationFilterInput>
  /** Gets the length of the metadata item in milliseconds. */
  length?: InputMaybe<IntOperationFilterInput>
  /** Gets the owning library section identifier (Relay GUID). */
  librarySectionId?: InputMaybe<IdOperationFilterInput>
  /** Gets the list of field names that are locked from automatic updates. */
  lockedFields?: InputMaybe<ListStringOperationFilterInput>
  /** Gets the ThumbHash placeholder for the logo. */
  logoHash?: InputMaybe<StringOperationFilterInput>
  /** Gets the logo URL of the metadata item. */
  logoUri?: InputMaybe<StringOperationFilterInput>
  /** Gets the type of the metadata item. */
  metadataType?: InputMaybe<MetadataTypeOperationFilterInput>
  or?: InputMaybe<Array<ItemFilterInput>>
  /** Gets the original title of the metadata item. */
  originalTitle?: InputMaybe<StringOperationFilterInput>
  /** Gets the date the metadata item was originally available. */
  originallyAvailableAt?: InputMaybe<LocalDateOperationFilterInput>
  /** Gets the summary description of the metadata item. */
  summary?: InputMaybe<StringOperationFilterInput>
  /** Gets the tagline of the metadata item. */
  tagline?: InputMaybe<StringOperationFilterInput>
  /** Gets the list of tags associated with this metadata item. */
  tags?: InputMaybe<ListStringOperationFilterInput>
  /** Gets the theme URL of the metadata item. */
  themeUrl?: InputMaybe<StringOperationFilterInput>
  /** Gets the ThumbHash placeholder for the thumbnail. */
  thumbHash?: InputMaybe<StringOperationFilterInput>
  /** Gets the thumbnail URL of the metadata item. */
  thumbUri?: InputMaybe<StringOperationFilterInput>
  /** Gets the title of the metadata item. */
  title?: InputMaybe<StringOperationFilterInput>
  /** Gets the sortable title of the metadata item. */
  titleSort?: InputMaybe<StringOperationFilterInput>
  /** Gets the year the metadata item was released. */
  year?: InputMaybe<IntOperationFilterInput>
}

/** Representation of a metadata item for pagination queries. */
export type ItemSortInput = {
  /** Gets the content rating age value for sorting purposes. */
  contentRating?: InputMaybe<SortEnumType>
  /** Gets the date and time when this item was added to the library. */
  dateAdded?: InputMaybe<SortEnumType>
  /** Gets the length of the metadata item in milliseconds. */
  duration?: InputMaybe<SortEnumType>
  /** Gets the index of the metadata item. */
  index?: InputMaybe<SortEnumType>
  /** Gets the date the metadata item was originally available. */
  releaseDate?: InputMaybe<SortEnumType>
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
  /** Search index rebuild job. */
  SearchIndexRebuild = 'SEARCH_INDEX_REBUILD',
  /** Trickplay (BIF) generation job. */
  TrickplayGeneration = 'TRICKPLAY_GENERATION',
}

export type KeyValuePairOfStringAndBoolean = {
  __typename?: 'KeyValuePairOfStringAndBoolean'
  key: Scalars['String']['output']
  value: Scalars['Boolean']['output']
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

/**
 * Represents a letter in the alphabetical index with its item count and position.
 * Used for jump bar navigation in library browse views.
 */
export type LetterIndexEntry = {
  __typename?: 'LetterIndexEntry'
  /** Gets the number of items starting with this letter. */
  count: Scalars['Int']['output']
  /**
   * Gets the zero-based offset of the first item starting with this letter
   * in the sorted list. Used for skip-based pagination jumps.
   */
  firstItemOffset: Scalars['Int']['output']
  /**
   * Gets the letter for this index entry.
   * "#" represents all non-alphabetic characters (numbers, symbols).
   * "A" through "Z" represent alphabetic characters.
   */
  letter: Scalars['String']['output']
}

/** Representation of a library section for GraphQL queries. */
export type LibrarySection = Node & {
  __typename?: 'LibrarySection'
  /**
   * Gets the available root item types for browsing this library section.
   *
   *
   * **Returns:**
   * A list of browsable item type options.
   */
  availableRootItemTypes: Array<BrowsableItemType>
  /**
   * Gets the available sort fields for browsing this library section.
   *
   *
   * **Returns:**
   * A list of sort field options.
   */
  availableSortFields: Array<SortField>
  /**
   * Gets an offset-paginated list of top-level (root) metadata items (those without a parent) for this library section.
   * Uses skip/take parameters to allow arbitrary position jumping for jump bar navigation.
   *
   *
   * **Returns:**
   * An in-memory queryable used by HotChocolate to create a collection segment.
   */
  children?: Maybe<ChildrenCollectionSegment>
  /**
   * Gets all distinct genres present in this library section.
   *
   *
   * **Returns:**
   * A flat list of genre names.
   */
  genres: Array<Scalars['String']['output']>
  /** Gets the global Relay-compatible identifier of the library section. */
  id: Scalars['ID']['output']
  /**
   * Gets the alphabetical index for jump bar navigation.
   * Returns entries for "#" (non-alphabetic) and A-Z with counts and offsets.
   *
   *
   * **Returns:**
   * A list of letter index entries sorted alphabetically (# first, then A-Z).
   */
  letterIndex: Array<LetterIndexEntry>
  /** Gets the list of root locations for the library section. */
  locations: Array<Scalars['String']['output']>
  /** Gets the display name of the library section. */
  name: Scalars['String']['output']
  /** Gets the settings for this library section. */
  settings: LibrarySectionSettings
  /** Gets the sortable name of the library section. */
  sortName: Scalars['String']['output']
  /**
   * Gets all distinct tags present in this library section.
   *
   *
   * **Returns:**
   * A flat list of tag names.
   */
  tags: Array<Scalars['String']['output']>
  /** Gets the type of the library section. */
  type: LibraryType
}

/** Representation of a library section for GraphQL queries. */
export type LibrarySectionChildrenArgs = {
  metadataTypes: Array<MetadataType>
  order?: InputMaybe<Array<ItemSortInput>>
  skip?: InputMaybe<Scalars['Int']['input']>
  take?: InputMaybe<Scalars['Int']['input']>
  where?: InputMaybe<ItemFilterInput>
}

/** Representation of a library section for GraphQL queries. */
export type LibrarySectionLetterIndexArgs = {
  metadataTypes: Array<MetadataType>
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
  /** Gets the list of metadata agent identifiers that are disabled for this library. */
  disabledMetadataAgents: Array<Scalars['String']['output']>
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
  /** Gets the list of metadata agent identifiers that are disabled for this library. */
  disabledMetadataAgents?: InputMaybe<ListStringOperationFilterInput>
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
  /** Gets the list of metadata agent identifiers that are disabled for this library. */
  disabledMetadataAgents: Array<Scalars['String']['input']>
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

/** Input for locking fields on a metadata item. */
export type LockMetadataFieldsInput = {
  /** The field names to lock. Use constants from MetadataFieldNames for built-in fields. */
  fields: Array<Scalars['String']['input']>
  /** The UUID of the metadata item to lock fields on. */
  itemId: Scalars['ID']['input']
}

/**
 * Categorizes metadata agents by their data source type.
 * Used in the UI to display appropriate icons and group agents.
 */
export enum MetadataAgentCategory {
  /** Embedded metadata extractors that read tags from media containers (ID3, Matroska, MP4). */
  Embedded = 'EMBEDDED',
  /** Local metadata agents that derive information without network access. */
  Local = 'LOCAL',
  /** Remote metadata agents that fetch from external APIs. */
  Remote = 'REMOTE',
  /** Sidecar file parsers that read metadata from adjacent files (.nfo, metadata.json). */
  Sidecar = 'SIDECAR',
}

/** Represents a metadata agent available in the system for GraphQL exposure. */
export type MetadataAgentInfo = {
  __typename?: 'MetadataAgentInfo'
  /** Gets the category of this agent (Sidecar, Embedded, Local, Remote). */
  category: MetadataAgentCategory
  /** Gets the default execution order. Lower values run first. */
  defaultOrder: Scalars['Int']['output']
  /** Gets a user-friendly description of what this agent does. */
  description: Scalars['String']['output']
  /** Gets the human-readable display name for the UI. */
  displayName: Scalars['String']['output']
  /** Gets the unique identifier/name of the agent. */
  name: Scalars['String']['output']
  /**
   * Gets the library types this agent supports.
   * Empty means the agent supports all library types.
   */
  supportedLibraryTypes: Array<LibraryType>
}

/** Payload returned after locking or unlocking metadata fields. */
export type MetadataFieldLockPayload = {
  __typename?: 'MetadataFieldLockPayload'
  /** Error message if the operation failed. */
  error?: Maybe<Scalars['String']['output']>
  /** The current list of locked fields on the item after the operation. */
  lockedFields?: Maybe<Array<Scalars['String']['output']>>
  query: Query
  /** Whether the operation was successful. */
  success: Scalars['Boolean']['output']
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
  /** The metadata represents a medium within an audio album release, such as a disc in a multi-disc set. */
  AlbumMedium = 'ALBUM_MEDIUM',
  /** The metadata represents a audio album release. */
  AlbumRelease = 'ALBUM_RELEASE',
  /** The metadata represents a grouping of album releases, such as a studio album or compilation. */
  AlbumReleaseGroup = 'ALBUM_RELEASE_GROUP',
  /** The metadata represents a audio work. */
  AudioWork = 'AUDIO_WORK',
  /** Behind-the-scenes extra metadata type. */
  BehindTheScenes = 'BEHIND_THE_SCENES',
  /**
   * The metadata represents an ordered set of books, such as a manga series, a periodical,
   * or a comic book series.
   */
  BookSeries = 'BOOK_SERIES',
  /** Clip metadata type. */
  Clip = 'CLIP',
  /** Collection metadata type. */
  Collection = 'COLLECTION',
  /** The metadata represents a company or organization (e.g., production company, publisher, distributor). */
  Company = 'COMPANY',
  /** Deleted scene extra metadata type. */
  DeletedScene = 'DELETED_SCENE',
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
  /** Other/uncategorized extra metadata type. */
  ExtraOther = 'EXTRA_OTHER',
  /** Featurette extra metadata type. */
  Featurette = 'FEATURETTE',
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
  /** Interview extra metadata type. */
  Interview = 'INTERVIEW',
  /** The metadata represents a label (e.g., record label, movie studio label, book imprint). */
  Label = 'LABEL',
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
  /** Scene extra metadata type. */
  Scene = 'SCENE',
  /** The metadata represents a single season of a TV show. */
  Season = 'SEASON',
  /** Short-form extra metadata type. */
  ShortForm = 'SHORT_FORM',
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
  /**
   * Enqueues file analysis, GoP-index generation, and trickplay generation for a metadata item.
   *
   *
   * **Returns:**
   * Payload indicating success or error.
   */
  analyzeItem: AnalyzeItemPayload
  /**
   * Creates a new custom field definition (admin only).
   *
   *
   * **Returns:**
   * The created custom field definition.
   */
  createCustomFieldDefinition: CustomFieldDefinition
  /**
   * Requests a playback decision for the current session.
   *
   *
   * **Returns:**
   * The decision payload.
   */
  decidePlayback: PlaybackDecisionPayload
  /**
   * Deletes a custom field definition (admin only).
   *
   *
   * **Returns:**
   * True if the field was deleted, false if it was not found.
   */
  deleteCustomFieldDefinition: Scalars['Boolean']['output']
  /**
   * Locks specified fields on a metadata item, preventing automatic updates.
   *
   *
   * **Returns:**
   * Payload indicating success and the current locked fields.
   */
  lockMetadataFields: MetadataFieldLockPayload
  /**
   * Records a playback heartbeat for the active session.
   *
   *
   * **Returns:**
   * Heartbeat acknowledgement payload.
   */
  playbackHeartbeat: PlaybackHeartbeatPayload
  /**
   * Notifies the server of a seek operation and returns the nearest keyframe position.
   * Used to optimize transcoding/remuxing by seeking to keyframe boundaries.
   *
   *
   * **Returns:**
   * The seek payload with the nearest keyframe position.
   */
  playbackSeek: PlaybackSeekPayload
  /**
   * Jumps to a specific index in the playlist.
   *
   *
   * **Returns:**
   * The navigation payload with the item at the specified index.
   */
  playlistJump: PlaylistNavigatePayload
  /**
   * Navigates to the next item in the playlist.
   *
   *
   * **Returns:**
   * The navigation payload with the next item.
   */
  playlistNext: PlaylistNavigatePayload
  /**
   * Navigates to the previous item in the playlist.
   *
   *
   * **Returns:**
   * The navigation payload with the previous item.
   */
  playlistPrevious: PlaylistNavigatePayload
  /**
   * Sets repeat mode on the playlist.
   *
   *
   * **Returns:**
   * The navigation payload with updated state.
   */
  playlistSetRepeat: PlaylistNavigatePayload
  /**
   * Sets shuffle mode on the playlist.
   *
   *
   * **Returns:**
   * The navigation payload with updated state.
   */
  playlistSetShuffle: PlaylistNavigatePayload
  /**
   * Promotes a metadata item to the hero carousel.
   *
   *
   * **Returns:**
   * Payload indicating success or error.
   */
  promoteItem: PromoteItemPayload
  /**
   * Enqueues a metadata-only refresh for a metadata item (optionally its descendants).
   *
   *
   * **Returns:**
   * Payload indicating success or error.
   */
  refreshItemMetadata: RefreshMetadataPayload
  /**
   * Enqueues metadata-only refresh jobs for an entire library section.
   *
   *
   * **Returns:**
   * Payload indicating success or error.
   */
  refreshLibraryMetadata: RefreshMetadataPayload
  /**
   * Removes a library section and all associated metadata items.
   *
   *
   * **Returns:**
   * Payload indicating success or error.
   */
  removeLibrarySection: RemoveLibrarySectionPayload
  /**
   * Initiates a graceful server shutdown. Container orchestrators (Docker, systemd, etc.) will auto-restart the service.
   *
   *
   * **Returns:**
   * True if shutdown was initiated successfully.
   */
  restartServer: Scalars['Boolean']['output']
  /**
   * Attempts to resume a playback session by identifier.
   *
   *
   * **Returns:**
   * Resume details for the session.
   */
  resumePlayback: PlaybackResumePayload
  /**
   * Starts a full filesystem scan for an entire library section.
   *
   *
   * **Returns:**
   * Payload indicating success, scan ID, or error.
   */
  startLibraryScan: StartLibraryScanPayload
  /**
   * Starts playback for a metadata item.
   *
   *
   * **Returns:**
   * The created playback session details.
   */
  startPlayback: PlaybackStartPayload
  /**
   * Stops an active playback session and cleans up associated resources.
   *
   *
   * **Returns:**
   * Acknowledgement payload.
   */
  stopPlayback: PlaybackStopPayload
  /**
   * Unlocks specified fields on a metadata item, allowing automatic updates.
   *
   *
   * **Returns:**
   * Payload indicating success and the current locked fields.
   */
  unlockMetadataFields: MetadataFieldLockPayload
  /**
   * Unpromotes a metadata item from the hero carousel.
   *
   *
   * **Returns:**
   * Payload indicating success or error.
   */
  unpromoteItem: PromoteItemPayload
  /**
   * Updates the admin-defined detail field configuration for a metadata type and optional library.
   *
   *
   * **Returns:**
   * The updated configuration.
   */
  updateAdminDetailFieldConfiguration: DetailFieldConfiguration
  /**
   * Updates an existing custom field definition (admin only).
   *
   *
   * **Returns:**
   * The updated custom field definition.
   */
  updateCustomFieldDefinition: CustomFieldDefinition
  /**
   * Updates the field visibility configuration for the current user.
   *
   *
   * **Returns:**
   * The updated field definitions.
   */
  updateDetailFieldConfiguration: Array<DetailFieldDefinition>
  /**
   * Updates a hub configuration for the specified context and scope (admin only).
   *
   *
   * **Returns:**
   * The updated hub configuration.
   */
  updateHubConfiguration: HubConfiguration
  /**
   * Updates a metadata item with the specified fields, respecting locked field settings.
   *
   *
   * **Returns:**
   * Payload indicating success and the updated item.
   */
  updateMetadataItem: UpdateMetadataItemPayload
  /**
   * Updates server-wide configuration settings. Only specified fields are updated.
   *
   *
   * **Returns:**
   * The updated server settings.
   */
  updateServerSettings: ServerSettingsPayload
  /**
   * Updates transcode settings (admin only).
   *
   *
   * **Returns:**
   * The updated transcode settings.
   */
  updateTranscodeSettings: TranscodeSettingsPayload
}

export type MutationAddLibrarySectionArgs = {
  input: AddLibrarySectionInput
}

export type MutationAnalyzeItemArgs = {
  input: AnalyzeItemInput
}

export type MutationCreateCustomFieldDefinitionArgs = {
  input: CreateCustomFieldDefinitionInput
}

export type MutationDecidePlaybackArgs = {
  input: PlaybackDecisionInput
}

export type MutationDeleteCustomFieldDefinitionArgs = {
  id: Scalars['ID']['input']
}

export type MutationLockMetadataFieldsArgs = {
  input: LockMetadataFieldsInput
}

export type MutationPlaybackHeartbeatArgs = {
  input: PlaybackHeartbeatInput
}

export type MutationPlaybackSeekArgs = {
  input: PlaybackSeekInput
}

export type MutationPlaylistJumpArgs = {
  input: PlaylistJumpInput
}

export type MutationPlaylistNextArgs = {
  input: PlaylistNavigateInput
}

export type MutationPlaylistPreviousArgs = {
  input: PlaylistNavigateInput
}

export type MutationPlaylistSetRepeatArgs = {
  input: PlaylistModeInput
}

export type MutationPlaylistSetShuffleArgs = {
  input: PlaylistModeInput
}

export type MutationPromoteItemArgs = {
  input: PromoteItemInput
}

export type MutationRefreshItemMetadataArgs = {
  input: RefreshItemMetadataInput
}

export type MutationRefreshLibraryMetadataArgs = {
  input: RefreshLibraryMetadataInput
}

export type MutationRemoveLibrarySectionArgs = {
  input: RemoveLibrarySectionInput
}

export type MutationResumePlaybackArgs = {
  input: PlaybackResumeInput
}

export type MutationStartLibraryScanArgs = {
  input: StartLibraryScanInput
}

export type MutationStartPlaybackArgs = {
  input: PlaybackStartInput
}

export type MutationStopPlaybackArgs = {
  input: PlaybackStopInput
}

export type MutationUnlockMetadataFieldsArgs = {
  input: UnlockMetadataFieldsInput
}

export type MutationUnpromoteItemArgs = {
  input: UnpromoteItemInput
}

export type MutationUpdateAdminDetailFieldConfigurationArgs = {
  input: UpdateAdminDetailFieldConfigurationInput
}

export type MutationUpdateCustomFieldDefinitionArgs = {
  input: UpdateCustomFieldDefinitionInput
}

export type MutationUpdateDetailFieldConfigurationArgs = {
  input: UpdateDetailFieldConfigurationInput
}

export type MutationUpdateHubConfigurationArgs = {
  input: UpdateHubConfigurationInput
}

export type MutationUpdateMetadataItemArgs = {
  input: UpdateMetadataItemInput
}

export type MutationUpdateServerSettingsArgs = {
  input: UpdateServerSettingsInput
}

export type MutationUpdateTranscodeSettingsArgs = {
  input: UpdateTranscodeSettingsInput
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

/** GraphQL input type describing client playback capabilities. */
export type PlaybackCapabilitiesInput = {
  /** Gets or sets a value indicating whether tone mapping is acceptable for the device. */
  allowToneMapping?: InputMaybe<Scalars['Boolean']['input']>
  /** Gets or sets codec-specific constraints. */
  codecProfiles: Array<CodecProfileInput>
  /** Gets or sets container-level constraints. */
  containerProfiles: Array<ContainerProfileInput>
  /** Gets or sets the direct-play formats the client can handle. */
  directPlayProfiles: Array<DirectPlayProfileInput>
  /** Gets or sets the maximum static download bitrate (bits per second). */
  maxStaticBitrate?: InputMaybe<Scalars['Int']['input']>
  /** Gets or sets the maximum streaming bitrate (bits per second). */
  maxStreamingBitrate?: InputMaybe<Scalars['Int']['input']>
  /** Gets or sets the preferred music transcoding bitrate (bits per second). */
  musicStreamingTranscodingBitrate?: InputMaybe<Scalars['Int']['input']>
  /** Gets or sets response overrides for certain media types. */
  responseProfiles: Array<ResponseProfileInput>
  /** Gets or sets subtitle delivery capabilities. */
  subtitleProfiles: Array<SubtitleProfileInput>
  /** Gets or sets the image formats the client can render without server-side resizing. */
  supportedImageFormats: Array<Scalars['String']['input']>
  /** Gets or sets a value indicating whether DASH playback is supported. */
  supportsDash?: InputMaybe<Scalars['Boolean']['input']>
  /** Gets or sets a value indicating whether the device can render HDR natively. */
  supportsHdr?: InputMaybe<Scalars['Boolean']['input']>
  /** Gets or sets a value indicating whether HLS playback is supported. */
  supportsHls?: InputMaybe<Scalars['Boolean']['input']>
  /** Gets or sets the acceptable transcoding targets. */
  transcodingProfiles: Array<TranscodingProfileInput>
}

/** Client capability declaration supplied with playback operations. */
export type PlaybackCapabilityInput = {
  /** Gets or sets the structured playback capabilities payload. */
  capabilities: PlaybackCapabilitiesInput
  /** Gets or sets the client device identifier. */
  deviceId?: InputMaybe<Scalars['String']['input']>
  /** Gets or sets a friendly device name. */
  name?: InputMaybe<Scalars['String']['input']>
  /** Gets or sets the explicit version the client wants to use. */
  version?: InputMaybe<Scalars['Int']['input']>
}

/** Input payload used when the client requests a playback decision. */
export type PlaybackDecisionInput = {
  /** Gets or sets an optional capability declaration to upsert. */
  capability?: InputMaybe<PlaybackCapabilityInput>
  /** Gets or sets the capability profile version the client is using. */
  capabilityProfileVersion?: InputMaybe<Scalars['Int']['input']>
  /** Gets or sets the current metadata item identifier. */
  currentItemId: Scalars['ID']['input']
  /** Gets or sets the target playlist index when status is "jump". */
  jumpIndex?: InputMaybe<Scalars['Int']['input']>
  /** Gets or sets the playback session identifier. */
  playbackSessionId: Scalars['UUID']['input']
  /** Gets or sets the current progress in milliseconds. */
  progressMs: Scalars['Long']['input']
  /** Gets or sets the current playback status. */
  status: Scalars['String']['input']
}

/** GraphQL payload describing the server decision for playback continuation. */
export type PlaybackDecisionPayload = {
  __typename?: 'PlaybackDecisionPayload'
  /** Gets the action the client should take. */
  action: Scalars['String']['output']
  /** Gets the latest capability profile version known to the server. */
  capabilityProfileVersion: Scalars['Int']['output']
  /** Gets a value indicating whether the client should refresh capabilities. */
  capabilityVersionMismatch: Scalars['Boolean']['output']
  /** Gets the next metadata item identifier. */
  nextItemId?: Maybe<Scalars['ID']['output']>
  /** Gets the original title of the next item (e.g., artist name for tracks). */
  nextItemOriginalTitle?: Maybe<Scalars['String']['output']>
  /** Gets the parent title of the next item (e.g., album name for tracks). */
  nextItemParentTitle?: Maybe<Scalars['String']['output']>
  /** Gets the thumbnail URL of the next item. */
  nextItemThumbUrl?: Maybe<Scalars['String']['output']>
  /** Gets the title of the next item. */
  nextItemTitle?: Maybe<Scalars['String']['output']>
  /** Gets the URL the client should load for the decided item. */
  playbackUrl: Scalars['String']['output']
  query: Query
  /** Gets the serialized stream plan for the next item. */
  streamPlanJson: Scalars['String']['output']
  /** Gets the trickplay thumbnail track URL when available. */
  trickplayUrl?: Maybe<Scalars['String']['output']>
}

/** Input payload used when sending playback heartbeats. */
export type PlaybackHeartbeatInput = {
  /** Gets or sets an optional capability declaration to upsert. */
  capability?: InputMaybe<PlaybackCapabilityInput>
  /** Gets or sets the capability profile version the client is using. */
  capabilityProfileVersion?: InputMaybe<Scalars['Int']['input']>
  /** Gets or sets the current media part identifier, if known. */
  mediaPartId?: InputMaybe<Scalars['Int']['input']>
  /** Gets or sets the playback session identifier. */
  playbackSessionId: Scalars['UUID']['input']
  /** Gets or sets the playhead position in milliseconds. */
  playheadMs: Scalars['Long']['input']
  /** Gets or sets the playback state. */
  state: Scalars['String']['input']
}

/** GraphQL payload returned after recording a playback heartbeat. */
export type PlaybackHeartbeatPayload = {
  __typename?: 'PlaybackHeartbeatPayload'
  /** Gets the latest capability profile version known to the server. */
  capabilityProfileVersion: Scalars['Int']['output']
  /** Gets a value indicating whether the client should refresh capabilities. */
  capabilityVersionMismatch: Scalars['Boolean']['output']
  /** Gets the playback session identifier. */
  playbackSessionId: Scalars['UUID']['output']
  query: Query
}

/** Input payload used to resume an existing playback session. */
export type PlaybackResumeInput = {
  /** Gets or sets an optional capability declaration to upsert. */
  capability?: InputMaybe<PlaybackCapabilityInput>
  /** Gets or sets the capability profile version the client is using. */
  capabilityProfileVersion?: InputMaybe<Scalars['Int']['input']>
  /** Gets or sets the playback session identifier. */
  playbackSessionId: Scalars['UUID']['input']
}

/** GraphQL payload returned when resuming an existing playback session. */
export type PlaybackResumePayload = {
  __typename?: 'PlaybackResumePayload'
  /** Gets the latest capability profile version known to the server. */
  capabilityProfileVersion: Scalars['Int']['output']
  /** Gets a value indicating whether the client should refresh capabilities. */
  capabilityVersionMismatch: Scalars['Boolean']['output']
  /** Gets the current metadata item identifier. */
  currentItemId: Scalars['ID']['output']
  /** Gets the duration of the media item in milliseconds. */
  durationMs?: Maybe<Scalars['Long']['output']>
  /** Gets the playback session identifier. */
  playbackSessionId: Scalars['UUID']['output']
  /** Gets the playback URL the client should load when resuming. */
  playbackUrl: Scalars['String']['output']
  /** Gets the current playhead in milliseconds. */
  playheadMs: Scalars['Long']['output']
  /** Gets the playlist generator identifier. */
  playlistGeneratorId: Scalars['UUID']['output']
  query: Query
  /** Gets the current playback state. */
  state: Scalars['String']['output']
  /** Gets the serialized stream plan for the current playback. */
  streamPlanJson: Scalars['String']['output']
  /** Gets the trickplay track URL when available. */
  trickplayUrl?: Maybe<Scalars['String']['output']>
}

/**
 * Input payload for notifying the server of a seek operation during playback.
 * Used to obtain the nearest keyframe position for optimal transcoding/remuxing.
 */
export type PlaybackSeekInput = {
  /** Gets or sets the current media part identifier. */
  mediaPartId: Scalars['Int']['input']
  /** Gets or sets the playback session identifier. */
  playbackSessionId: Scalars['UUID']['input']
  /** Gets or sets the target seek position in milliseconds. */
  targetMs: Scalars['Long']['input']
}

/**
 * Payload returned after processing a seek notification.
 * Contains the nearest keyframe position for optimal seeking during transcoding/remuxing.
 */
export type PlaybackSeekPayload = {
  __typename?: 'PlaybackSeekPayload'
  /** Gets the duration of the keyframe's group of pictures in milliseconds. */
  gopDurationMs: Scalars['Long']['output']
  /**
   * Gets a value indicating whether a GoP index was available for this seek.
   * When false, KeyframeMs equals the original target position.
   */
  hasGopIndex: Scalars['Boolean']['output']
  /**
   * Gets the nearest keyframe position in milliseconds.
   * This is the position the transcoder/remuxer will seek to for faster feedback.
   */
  keyframeMs: Scalars['Long']['output']
  /** Gets the original requested seek position in milliseconds. */
  originalTargetMs: Scalars['Long']['output']
  query: Query
}

/** Input payload used to start playback for a metadata item. */
export type PlaybackStartInput = {
  /** Gets or sets an optional capability declaration to upsert. */
  capability?: InputMaybe<PlaybackCapabilityInput>
  /** Gets or sets the capability profile version the client believes is current. */
  capabilityProfileVersion?: InputMaybe<Scalars['Int']['input']>
  /**
   * Gets or sets an optional JSON payload describing playback context.
   * This property is deprecated; use PlaylistType, OriginatorId, Shuffle, and Repeat instead.
   */
  contextJson?: InputMaybe<Scalars['String']['input']>
  /**
   * Gets or sets the metadata item identifier to start playing.
   * For single item playback, this is the item to play.
   * For container playback (album, show), this can be the specific child to start with.
   */
  itemId: Scalars['ID']['input']
  /** Gets or sets an optional originator descriptor. */
  originator?: InputMaybe<Scalars['String']['input']>
  /**
   * Gets or sets an optional originator identifier for container-based playlists.
   * When playing an album track or show episode, set this to the parent container ID
   * to enable playlist navigation through all items in the container.
   */
  originatorId?: InputMaybe<Scalars['ID']['input']>
  /**
   * Gets or sets the playlist type. Defaults to "single" for single item playback.
   * Supported values: "single", "album", "season", "show", "artist", "library", "explicit".
   */
  playlistType: Scalars['String']['input']
  /** Gets or sets a value indicating whether repeat mode should be enabled for the playlist. */
  repeat: Scalars['Boolean']['input']
  /** Gets or sets a value indicating whether shuffle mode should be enabled for the playlist. */
  shuffle: Scalars['Boolean']['input']
}

/** GraphQL payload returned after starting playback. */
export type PlaybackStartPayload = {
  __typename?: 'PlaybackStartPayload'
  /** Gets the capability profile version the server used. */
  capabilityProfileVersion: Scalars['Int']['output']
  /** Gets a value indicating whether the client should refresh capabilities. */
  capabilityVersionMismatch: Scalars['Boolean']['output']
  /** Gets the current item's identifier (public UUID). */
  currentItemId?: Maybe<Scalars['ID']['output']>
  /** Gets the metadata type of the current item. */
  currentItemMetadataType: Scalars['String']['output']
  /** Gets the original title (e.g., artist) of the current item being played. */
  currentItemOriginalTitle?: Maybe<Scalars['String']['output']>
  /** Gets the parent thumbnail URL of the current item being played. */
  currentItemParentThumbUrl?: Maybe<Scalars['String']['output']>
  /** Gets the parent title (e.g., album) of the current item being played. */
  currentItemParentTitle?: Maybe<Scalars['String']['output']>
  /** Gets the thumbnail URL of the current item being played. */
  currentItemThumbUrl?: Maybe<Scalars['String']['output']>
  /** Gets the title of the current item being played. */
  currentItemTitle?: Maybe<Scalars['String']['output']>
  /** Gets the duration of the media item in milliseconds. */
  durationMs?: Maybe<Scalars['Long']['output']>
  /** Gets the playback session identifier. */
  playbackSessionId: Scalars['UUID']['output']
  /** Gets the URL the client should load to start playback. */
  playbackUrl: Scalars['String']['output']
  /** Gets the playlist generator identifier. */
  playlistGeneratorId: Scalars['UUID']['output']
  /** Gets the current index within the playlist (0-based). */
  playlistIndex: Scalars['Int']['output']
  /** Gets the total number of items in the playlist. */
  playlistTotalCount: Scalars['Int']['output']
  query: Query
  /** Gets a value indicating whether repeat mode is enabled. */
  repeat: Scalars['Boolean']['output']
  /** Gets a value indicating whether shuffle mode is enabled. */
  shuffle: Scalars['Boolean']['output']
  /** Gets the serialized stream plan for the current item. */
  streamPlanJson: Scalars['String']['output']
  /** Gets the trickplay thumbnail track URL when available. */
  trickplayUrl?: Maybe<Scalars['String']['output']>
}

/** Input payload used to stop an active playback session. */
export type PlaybackStopInput = {
  /** Gets or sets the playback session identifier to stop. */
  playbackSessionId: Scalars['UUID']['input']
}

/** Payload returned after stopping a playback session. */
export type PlaybackStopPayload = {
  __typename?: 'PlaybackStopPayload'
  query: Query
  /** Gets a value indicating whether the playback session was stopped. */
  success: Scalars['Boolean']['output']
}

/** Input payload used to request a chunk of playlist items. */
export type PlaylistChunkInput = {
  /** Gets or sets the maximum number of items to return. */
  limit: Scalars['Int']['input']
  /** Gets or sets the playlist generator identifier. */
  playlistGeneratorId: Scalars['UUID']['input']
  /** Gets or sets the starting index (0-based). */
  startIndex: Scalars['Int']['input']
}

/** GraphQL payload containing a chunk of playlist items. */
export type PlaylistChunkPayload = {
  __typename?: 'PlaylistChunkPayload'
  /** Gets or sets the current cursor position (0-based index of the currently playing item). */
  currentIndex: Scalars['Int']['output']
  /** Gets or sets a value indicating whether there are more items available after this chunk. */
  hasMore: Scalars['Boolean']['output']
  /** Gets or sets the items in this chunk. */
  items: Array<PlaylistItemPayload>
  /** Gets or sets the playlist generator identifier. */
  playlistGeneratorId: Scalars['UUID']['output']
  /** Gets or sets a value indicating whether repeat mode is enabled. */
  repeat: Scalars['Boolean']['output']
  /** Gets or sets a value indicating whether shuffle mode is enabled. */
  shuffle: Scalars['Boolean']['output']
  /**
   * Gets or sets the total number of items in the playlist.
   * -1 indicates the total is unknown (e.g., for infinite/dynamic playlists).
   */
  totalCount: Scalars['Int']['output']
}

/** GraphQL payload representing a single playlist item. */
export type PlaylistItemPayload = {
  __typename?: 'PlaylistItemPayload'
  /** Gets or sets the duration in milliseconds, if known. */
  durationMs?: Maybe<Scalars['Long']['output']>
  /** Gets or sets the 0-based index of this item within the playlist. */
  index: Scalars['Int']['output']
  /** Gets or sets the unique identifier of the playlist item entry. */
  itemEntryId: Scalars['Int']['output']
  /** Gets or sets the public UUID of the metadata item. */
  itemId: Scalars['ID']['output']
  /** Gets or sets the metadata type (Movie, Episode, Track, etc.). */
  metadataType: Scalars['String']['output']
  /** Gets or sets the parent title (e.g., album for tracks, show for episodes). */
  parentTitle?: Maybe<Scalars['String']['output']>
  /** Gets or sets the playback URL for this playlist entry when precomputed (e.g., images). */
  playbackUrl?: Maybe<Scalars['String']['output']>
  /** Gets or sets the primary person (e.g., artist for tracks, director for movies). */
  primaryPerson?: Maybe<Item>
  /** Gets or sets a value indicating whether this item has been served to the client. */
  served: Scalars['Boolean']['output']
  /** Gets or sets additional context like episode number or track number. */
  subtitle?: Maybe<Scalars['String']['output']>
  /** Gets or sets the thumbnail URI for the item. */
  thumbUri?: Maybe<Scalars['String']['output']>
  /** Gets or sets the title of the item. */
  title: Scalars['String']['output']
}

/** Input for jumping to a specific index in the playlist. */
export type PlaylistJumpInput = {
  /** Gets or sets the 0-based index to jump to. */
  index: Scalars['Int']['input']
  /** Gets or sets the playlist generator identifier. */
  playlistGeneratorId: Scalars['UUID']['input']
}

/** Input for setting shuffle or repeat mode on a playlist. */
export type PlaylistModeInput = {
  /** Gets or sets a value indicating whether the mode should be enabled. */
  enabled: Scalars['Boolean']['input']
  /** Gets or sets the playlist generator identifier. */
  playlistGeneratorId: Scalars['UUID']['input']
}

/** Input for navigating to next/previous item in a playlist. */
export type PlaylistNavigateInput = {
  /** Gets or sets the playlist generator identifier. */
  playlistGeneratorId: Scalars['UUID']['input']
}

/** Payload returned after navigating in a playlist or changing modes. */
export type PlaylistNavigatePayload = {
  __typename?: 'PlaylistNavigatePayload'
  /** Gets or sets the current cursor position. */
  currentIndex: Scalars['Int']['output']
  /** Gets or sets the current playlist item after navigation. */
  currentItem?: Maybe<PlaylistItemPayload>
  query: Query
  /** Gets or sets a value indicating whether repeat mode is enabled. */
  repeat: Scalars['Boolean']['output']
  /** Gets or sets a value indicating whether shuffle mode is enabled. */
  shuffle: Scalars['Boolean']['output']
  /** Gets or sets a value indicating whether the operation succeeded. */
  success: Scalars['Boolean']['output']
  /** Gets or sets the total count of items in the playlist. */
  totalCount: Scalars['Int']['output']
}

/** GraphQL input describing a condition evaluated against media attributes. */
export type ProfileConditionInput = {
  /** Gets or sets the comparison operator (e.g., Equals, Contains, IsIn, NotEquals). */
  condition: Scalars['String']['input']
  /** Gets or sets a value indicating whether the condition is mandatory. */
  isRequired: Scalars['Boolean']['input']
  /** Gets or sets a value indicating whether the condition applies only when transcoding. */
  isRequiredForTranscoding: Scalars['Boolean']['input']
  /** Gets or sets the property name to evaluate. */
  property: Scalars['String']['input']
  /** Gets or sets the value to compare against. */
  value?: InputMaybe<Scalars['String']['input']>
}

/** Input for promoting a metadata item to the hero carousel. */
export type PromoteItemInput = {
  /** Gets or sets the metadata item identifier to promote. */
  itemId: Scalars['ID']['input']
  /**
   * Gets or sets the optional expiration date for the promotion.
   * If set, the item will be automatically unpromoted after this time.
   */
  promotedUntil?: InputMaybe<Scalars['DateTime']['input']>
}

/** GraphQL payload indicating the outcome of a promote/unpromote request. */
export type PromoteItemPayload = {
  __typename?: 'PromoteItemPayload'
  /** Gets an optional error description for failed requests. */
  error?: Maybe<Scalars['String']['output']>
  query: Query
  /** Gets a value indicating whether the request succeeded. */
  success: Scalars['Boolean']['output']
}

/** Defines GraphQL query operations for search. */
export type Query = {
  __typename?: 'Query'
  /**
   * Gets all active job notifications for bootstrapping client state.
   * Active jobs are those with status Pending or Running.
   *
   *
   * **Returns:**
   * A collection of active job notifications.
   */
  activeJobNotifications: Array<JobNotification>
  /**
   * Retrieves the admin-defined detail field configuration for a metadata type and optional library scope (admin only).
   *
   *
   * **Returns:**
   * The stored configuration if found; otherwise null.
   */
  adminDetailFieldConfiguration?: Maybe<DetailFieldConfiguration>
  /**
   * Gets all available metadata agents, sidecar parsers, and embedded metadata extractors.
   * Optionally filtered by library type.
   *
   *
   * **Returns:**
   * A list of available metadata agents.
   */
  availableMetadataAgents: Array<MetadataAgentInfo>
  /**
   * Browses a directory path, returning child entries, while ensuring access restrictions.
   *
   *
   * **Returns:**
   * The directory listing for the requested path.
   */
  browseDirectory: DirectoryListing
  /**
   * Gets all custom field definitions (admin only).
   *
   *
   * **Returns:**
   * A collection of custom field definitions.
   */
  customFieldDefinitions: Array<CustomFieldDefinition>
  /**
   * Gets the detected FFmpeg capabilities and hardware acceleration recommendations (admin only).
   *
   *
   * **Returns:**
   * The detected FFmpeg capabilities.
   */
  ffmpegCapabilities: FfmpegCapabilitiesPayload
  /**
   * Gets field definitions for a specific metadata type.
   *
   *
   * **Returns:**
   * A collection of field definitions.
   */
  fieldDefinitionsForType: Array<DetailFieldDefinition>
  /**
   * Lists filesystem roots (drives, mounts) that can be browsed for library creation.
   *
   *
   * **Returns:**
   * A collection of filesystem roots.
   */
  fileSystemRoots: Array<FileSystemRoot>
  /**
   * Gets hub definitions for the home page (aggregated from user's accessible libraries).
   *
   *
   * **Returns:**
   * A collection of hub definitions.
   */
  homeHubDefinitions: Array<HubDefinition>
  /**
   * Retrieves a hub configuration for the given context and scope (admin only).
   *
   *
   * **Returns:**
   * The stored hub configuration if found; otherwise null.
   */
  hubConfiguration?: Maybe<HubConfiguration>
  /**
   * Gets hub items for a specific hub type and context.
   *
   *
   * **Returns:**
   * A list of metadata items.
   */
  hubItems: Array<Item>
  /**
   * Gets hub people for a specific hub type.
   *
   *
   * **Returns:**
   * A list of metadata items representing people.
   */
  hubPeople: Array<Item>
  /**
   * Gets field definitions for a metadata item's detail page.
   *
   *
   * **Returns:**
   * A collection of field definitions.
   */
  itemDetailFieldDefinitions: Array<DetailFieldDefinition>
  /**
   * Gets hub definitions for an item's detail page.
   *
   *
   * **Returns:**
   * A collection of hub definitions.
   */
  itemDetailHubDefinitions: Array<HubDefinition>
  /**
   * Gets hub definitions for a library's discover page.
   *
   *
   * **Returns:**
   * A collection of hub definitions.
   */
  libraryDiscoverHubDefinitions: Array<HubDefinition>
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
   * Retrieves a chunk of playlist items.
   *
   *
   * **Returns:**
   * The playlist chunk payload.
   */
  playlistChunk: PlaylistChunkPayload
  /**
   * Searches for metadata items matching the specified query.
   *
   *
   * **Returns:**
   * A list of search results ordered by relevance.
   */
  search: Array<SearchResult>
  /**
   * Gets basic server information like version and environment.
   *
   *
   * **Returns:**
   * The server info object.
   */
  serverInfo: ServerInfo
  /**
   * Gets the current server-wide configuration settings.
   *
   *
   * **Returns:**
   * The current server settings.
   */
  serverSettings: ServerSettingsPayload
  /**
   * Gets the current transcode settings and detected hardware capabilities (admin only).
   *
   *
   * **Returns:**
   * The current transcode settings.
   */
  transcodeSettings: TranscodeSettingsPayload
}

/** Defines GraphQL query operations for search. */
export type QueryAdminDetailFieldConfigurationArgs = {
  input: DetailFieldConfigurationScopeInput
}

/** Defines GraphQL query operations for search. */
export type QueryAvailableMetadataAgentsArgs = {
  libraryType?: InputMaybe<LibraryType>
}

/** Defines GraphQL query operations for search. */
export type QueryBrowseDirectoryArgs = {
  path: Scalars['String']['input']
}

/** Defines GraphQL query operations for search. */
export type QueryFieldDefinitionsForTypeArgs = {
  metadataType: MetadataType
}

/** Defines GraphQL query operations for search. */
export type QueryHubConfigurationArgs = {
  input: HubConfigurationScopeInput
}

/** Defines GraphQL query operations for search. */
export type QueryHubItemsArgs = {
  input: GetHubItemsInput
}

/** Defines GraphQL query operations for search. */
export type QueryHubPeopleArgs = {
  hubType: HubType
  metadataItemId: Scalars['ID']['input']
}

/** Defines GraphQL query operations for search. */
export type QueryItemDetailFieldDefinitionsArgs = {
  itemId: Scalars['ID']['input']
}

/** Defines GraphQL query operations for search. */
export type QueryItemDetailHubDefinitionsArgs = {
  itemId: Scalars['ID']['input']
}

/** Defines GraphQL query operations for search. */
export type QueryLibraryDiscoverHubDefinitionsArgs = {
  librarySectionId: Scalars['ID']['input']
}

/** Defines GraphQL query operations for search. */
export type QueryLibrarySectionArgs = {
  id: Scalars['ID']['input']
}

/** Defines GraphQL query operations for search. */
export type QueryLibrarySectionsArgs = {
  after?: InputMaybe<Scalars['String']['input']>
  before?: InputMaybe<Scalars['String']['input']>
  first?: InputMaybe<Scalars['Int']['input']>
  last?: InputMaybe<Scalars['Int']['input']>
  order?: InputMaybe<Array<LibrarySectionSortInput>>
  where?: InputMaybe<LibrarySectionFilterInput>
}

/** Defines GraphQL query operations for search. */
export type QueryMetadataItemArgs = {
  id: Scalars['ID']['input']
}

/** Defines GraphQL query operations for search. */
export type QueryMetadataItemsArgs = {
  after?: InputMaybe<Scalars['String']['input']>
  before?: InputMaybe<Scalars['String']['input']>
  first?: InputMaybe<Scalars['Int']['input']>
  last?: InputMaybe<Scalars['Int']['input']>
  order?: InputMaybe<Array<ItemSortInput>>
  where?: InputMaybe<ItemFilterInput>
}

/** Defines GraphQL query operations for search. */
export type QueryNodeArgs = {
  id: Scalars['ID']['input']
}

/** Defines GraphQL query operations for search. */
export type QueryNodesArgs = {
  ids: Array<Scalars['ID']['input']>
}

/** Defines GraphQL query operations for search. */
export type QueryPlaylistChunkArgs = {
  input: PlaylistChunkInput
}

/** Defines GraphQL query operations for search. */
export type QuerySearchArgs = {
  limit?: Scalars['Int']['input']
  pivot?: SearchPivot
  query: Scalars['String']['input']
}

/** Input for refreshing metadata for a single item (optionally including descendants). */
export type RefreshItemMetadataInput = {
  /**
   * Gets or sets a value indicating whether to include all descendants.
   * Defaults to true to refresh entire item trees.
   */
  includeChildren: Scalars['Boolean']['input']
  /** Gets or sets the metadata item identifier. */
  itemId: Scalars['ID']['input']
  /**
   * Gets or sets optional field names to force update, bypassing any locks.
   * Use constants from MetadataFieldNames for built-in fields.
   * When not specified or empty, locked fields are respected.
   */
  overrideFields?: InputMaybe<Array<Scalars['String']['input']>>
}

/** Input for refreshing metadata for an entire library section. */
export type RefreshLibraryMetadataInput = {
  /** Gets or sets the library section identifier. */
  librarySectionId: Scalars['ID']['input']
}

/** GraphQL payload indicating the outcome of a metadata refresh request. */
export type RefreshMetadataPayload = {
  __typename?: 'RefreshMetadataPayload'
  /** Gets an optional error description for failed requests. */
  error?: Maybe<Scalars['String']['output']>
  query: Query
  /** Gets a value indicating whether the request succeeded. */
  success: Scalars['Boolean']['output']
}

/** Input for removing a library section. */
export type RemoveLibrarySectionInput = {
  /** Gets or sets the library section identifier. */
  librarySectionId: Scalars['ID']['input']
}

/** GraphQL payload indicating the outcome of a library section removal request. */
export type RemoveLibrarySectionPayload = {
  __typename?: 'RemoveLibrarySectionPayload'
  /** Gets an optional error description for failed requests. */
  error?: Maybe<Scalars['String']['output']>
  query: Query
  /** Gets a value indicating whether the request succeeded. */
  success: Scalars['Boolean']['output']
}

/** GraphQL input mapping response overrides for specific media/container pairs. */
export type ResponseProfileInput = {
  /** Gets or sets the container restriction. */
  container: Scalars['String']['input']
  /** Gets or sets the MIME type to advertise. */
  mimeType: Scalars['String']['input']
  /** Gets or sets the media type (Video/Audio/Photo). */
  type: Scalars['String']['input']
}

/** Specifies the type of content to search for in the search index. */
export enum SearchPivot {
  /** Returns only music album results. */
  Album = 'ALBUM',
  /** Returns only episode results. */
  Episode = 'EPISODE',
  /** Returns only movie results. */
  Movie = 'MOVIE',
  /** Returns only person and group results. */
  People = 'PEOPLE',
  /** Returns only TV show results. */
  Show = 'SHOW',
  /** Returns top results across all metadata item types. */
  Top = 'TOP',
  /** Returns only music track results. */
  Track = 'TRACK',
}

/** Represents a single result from a search query. */
export type SearchResult = {
  __typename?: 'SearchResult'
  /** Gets the unique identifier of the metadata item. */
  id: Scalars['ID']['output']
  /** Gets the library section ID of the metadata item. */
  librarySectionId: Scalars['ID']['output']
  /** Gets the type of the metadata item. */
  metadataType: MetadataType
  /** Gets the relevance score of the search result. */
  score: Scalars['Float']['output']
  /** Gets the thumbnail URL of the metadata item, if available. */
  thumbUri?: Maybe<Scalars['String']['output']>
  /** Gets the title of the metadata item. */
  title: Scalars['String']['output']
  /** Gets the release year of the metadata item, if available. */
  year?: Maybe<Scalars['Int']['output']>
}

/** Represents server runtime information for client display. */
export type ServerInfo = {
  __typename?: 'ServerInfo'
  /** Gets a value indicating whether the server is running in Development environment. */
  isDevelopment: Scalars['Boolean']['output']
  /** Gets the semantic version string of the running server build. */
  versionString: Scalars['String']['output']
}

/** Payload containing server-wide configuration settings. */
export type ServerSettingsPayload = {
  __typename?: 'ServerSettingsPayload'
  /** Whether to allow HEVC encoding when transcoding video. */
  allowHEVCEncoding: Scalars['Boolean']['output']
  /** Whether to allow remuxing (container change without re-encoding). */
  allowRemuxing: Scalars['Boolean']['output']
  /** List of allowed tags (empty = no allowlist). */
  allowedTags: Array<Scalars['String']['output']>
  /** List of blocked tags (empty = no blocklist). */
  blockedTags: Array<Scalars['String']['output']>
  /** Default audio codec for DASH transcoding. */
  dashAudioCodec: Scalars['String']['output']
  /** DASH segment duration in seconds. */
  dashSegmentDurationSeconds: Scalars['Int']['output']
  /** Default video codec for DASH transcoding. */
  dashVideoCodec: Scalars['String']['output']
  /** Whether tone mapping is enabled for HDR content. */
  enableToneMapping: Scalars['Boolean']['output']
  /** Genre normalization mappings (input → canonical). */
  genreMappings: Array<KeyValuePairOfStringAndString>
  /** Minimum log level (Debug, Information, Warning, Error, Fatal). */
  logLevel: Scalars['String']['output']
  /** Maximum streaming bitrate in bits per second. */
  maxStreamingBitrate: Scalars['Int']['output']
  /** Whether to prefer H.265 (HEVC) codec for video transcoding. */
  preferH265: Scalars['Boolean']['output']
  query: Query
  /** The friendly display name of the server. */
  serverName: Scalars['String']['output']
  /** User's preferred hardware acceleration (null = auto-detect). */
  userPreferredAcceleration?: Maybe<HardwareAccelerationKind>
}

export enum SortEnumType {
  Asc = 'ASC',
  Desc = 'DESC',
}

/** Represents an available sort field option for browsing a library section. */
export type SortField = {
  __typename?: 'SortField'
  /** Gets the user-facing display name for this sort field. */
  displayName: Scalars['String']['output']
  /**
   * Gets the unique key identifier for this sort field.
   * This key should be used when constructing sort input objects.
   */
  key: Scalars['String']['output']
  /**
   * Gets a value indicating whether sorting by this field requires user-specific data.
   * When true, sorting uses a SQL join to user data (e.g., Progress, Date Viewed, Plays).
   * When false, sorting can be performed efficiently without additional joins.
   */
  requiresUserData: Scalars['Boolean']['output']
}

/** Input for starting a full library scan. */
export type StartLibraryScanInput = {
  /** Gets or sets the library section identifier. */
  librarySectionId: Scalars['ID']['input']
}

/** GraphQL payload indicating the outcome of a library scan request. */
export type StartLibraryScanPayload = {
  __typename?: 'StartLibraryScanPayload'
  /** Gets an optional error description for failed requests. */
  error?: Maybe<Scalars['String']['output']>
  query: Query
  /** Gets the scan job identifier. */
  scanId?: Maybe<Scalars['Int']['output']>
  /** Gets a value indicating whether the request succeeded. */
  success: Scalars['Boolean']['output']
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

/** GraphQL input describing subtitle delivery capabilities. */
export type SubtitleProfileInput = {
  /** Gets or sets the subtitle format. */
  format: Scalars['String']['input']
  /** Gets or sets languages this profile covers, if constrained. */
  languages: Array<Scalars['String']['input']>
  /** Gets or sets the delivery method (External/Embed/Encode). */
  method: Scalars['String']['input']
  /** Gets or sets the delivery protocol when applicable (e.g., hls/dash). */
  protocol?: InputMaybe<Scalars['String']['input']>
}

/** Payload returned after updating or retrieving transcode settings. */
export type TranscodeSettingsPayload = {
  __typename?: 'TranscodeSettingsPayload'
  /** List of available FFmpeg encoders. */
  availableEncoders: Array<Scalars['String']['output']>
  /** List of available FFmpeg filters. */
  availableFilters: Array<Scalars['String']['output']>
  /** Default audio codec for DASH transcoding. */
  dashAudioCodec: Scalars['String']['output']
  /** DASH segment duration in seconds. */
  dashSegmentDurationSeconds: Scalars['Int']['output']
  /** Default video codec for DASH transcoding. */
  dashVideoCodec: Scalars['String']['output']
  /** Dictionary of detected hardware acceleration capabilities. */
  detectedCapabilities: Array<KeyValuePairOfStringAndBoolean>
  /** The currently active hardware acceleration. */
  effectiveAcceleration: HardwareAccelerationKind
  /** Whether tone mapping is enabled. */
  enableToneMapping: Scalars['Boolean']['output']
  query: Query
  /** The system-recommended hardware acceleration for this platform. */
  recommendedAcceleration: HardwareAccelerationKind
  /** The user's manually specified preference (null = auto). */
  userPreferredAcceleration?: Maybe<HardwareAccelerationKind>
}

/** GraphQL input describing an allowed transcoding target. */
export type TranscodingProfileInput = {
  /** Gets or sets the conditions under which this profile applies. */
  applyConditions: Array<ProfileConditionInput>
  /** Gets or sets the preferred audio codec. */
  audioCodec?: InputMaybe<Scalars['String']['input']>
  /** Gets or sets the container output. */
  container: Scalars['String']['input']
  /** Gets or sets the playback context (Streaming/Static). */
  context: Scalars['String']['input']
  /** Gets or sets a value indicating whether timestamps should be preserved when possible. */
  copyTimestamps?: InputMaybe<Scalars['Boolean']['input']>
  /** Gets or sets the maximum audio channels the client expects. */
  maxAudioChannels?: InputMaybe<Scalars['String']['input']>
  /** Gets or sets the delivery protocol (e.g., hls). */
  protocol: Scalars['String']['input']
  /** Gets or sets the media type (Video/Audio/Photo). */
  type: Scalars['String']['input']
  /** Gets or sets the preferred video codec. */
  videoCodec?: InputMaybe<Scalars['String']['input']>
}

/** Input for unlocking fields on a metadata item. */
export type UnlockMetadataFieldsInput = {
  /** The field names to unlock. */
  fields: Array<Scalars['String']['input']>
  /** The UUID of the metadata item to unlock fields on. */
  itemId: Scalars['ID']['input']
}

/** Input for unpromoting a metadata item from the hero carousel. */
export type UnpromoteItemInput = {
  /** Gets or sets the metadata item identifier to unpromote. */
  itemId: Scalars['ID']['input']
}

/** Input for updating an admin-defined detail field configuration. */
export type UpdateAdminDetailFieldConfigurationInput = {
  /** Gets or sets the disabled custom field keys. */
  disabledCustomFieldKeys?: InputMaybe<Array<Scalars['String']['input']>>
  /** Gets or sets the disabled field types. */
  disabledFieldTypes?: InputMaybe<Array<DetailFieldType>>
  /** Gets or sets the enabled field types in display order. */
  enabledFieldTypes?: InputMaybe<Array<DetailFieldType>>
  /** Gets or sets the field-to-group assignments as key-value pairs. */
  fieldGroupAssignments?: InputMaybe<Array<KeyValuePairOfStringAndStringInput>>
  /** Gets or sets the list of field group definitions. */
  fieldGroups?: InputMaybe<Array<DetailFieldGroupInput>>
  /** Gets or sets the optional library section identifier when scoping the configuration. */
  librarySectionId?: InputMaybe<Scalars['ID']['input']>
  /** Gets or sets the metadata type this configuration targets. */
  metadataType: MetadataType
}

/** Input type for updating a custom field definition. */
export type UpdateCustomFieldDefinitionInput = {
  /**
   * Gets or sets the new metadata types this field applies to.
   * Empty list means the field applies to all metadata types.
   */
  applicableMetadataTypes?: InputMaybe<Array<MetadataType>>
  /** Gets or sets the ID of the custom field definition to update. */
  id: Scalars['ID']['input']
  /** Gets or sets whether the field is enabled. */
  isEnabled?: InputMaybe<Scalars['Boolean']['input']>
  /** Gets or sets the new display label for this field. */
  label?: InputMaybe<Scalars['String']['input']>
  /** Gets or sets the new display order of this field. */
  sortOrder?: InputMaybe<Scalars['Int']['input']>
  /** Gets or sets the new widget type for rendering this field. */
  widget?: InputMaybe<DetailFieldWidgetType>
}

/** Input type for updating user field configuration. */
export type UpdateDetailFieldConfigurationInput = {
  /** Gets or sets the list of explicitly disabled field types. */
  disabledFieldTypes?: InputMaybe<Array<DetailFieldType>>
  /** Gets or sets the list of enabled field types in display order. */
  enabledFieldTypes?: InputMaybe<Array<DetailFieldType>>
  /** Gets or sets the metadata type this configuration applies to. */
  metadataType: MetadataType
}

/** Input for updating a hub configuration for a given scope. */
export type UpdateHubConfigurationInput = {
  /** Gets or sets the hub context to update. */
  context: HubContext
  /** Gets or sets the disabled hub types. */
  disabledHubTypes?: InputMaybe<Array<HubType>>
  /** Gets or sets the enabled hub types in display order. */
  enabledHubTypes?: InputMaybe<Array<HubType>>
  /** Gets or sets the optional library section identifier for discover/detail scopes. */
  librarySectionId?: InputMaybe<Scalars['ID']['input']>
  /** Gets or sets the optional metadata type for item detail scopes. */
  metadataType?: InputMaybe<MetadataType>
}

/** Input for updating a metadata item. */
export type UpdateMetadataItemInput = {
  /** The new content rating for the item. */
  contentRating?: InputMaybe<Scalars['String']['input']>
  /** The external identifiers for the item. */
  externalIds?: InputMaybe<Array<ExternalIdInput>>
  /** The custom extra fields for the item. */
  extraFields?: InputMaybe<Array<ExtraFieldInput>>
  /** The new genres for the item. */
  genres?: InputMaybe<Array<Scalars['String']['input']>>
  /** The UUID of the metadata item to update. */
  itemId: Scalars['ID']['input']
  /** The field names that should be locked from automatic updates. */
  lockedFields?: InputMaybe<Array<Scalars['String']['input']>>
  /** The new original title for the item. */
  originalTitle?: InputMaybe<Scalars['String']['input']>
  /** The new release date for the item. */
  releaseDate?: InputMaybe<Scalars['LocalDate']['input']>
  /** The new sort title for the item. */
  sortTitle?: InputMaybe<Scalars['String']['input']>
  /** The new summary/description for the item. */
  summary?: InputMaybe<Scalars['String']['input']>
  /** The new tagline for the item. */
  tagline?: InputMaybe<Scalars['String']['input']>
  /** The new tags for the item. */
  tags?: InputMaybe<Array<Scalars['String']['input']>>
  /** The new title for the item. */
  title?: InputMaybe<Scalars['String']['input']>
}

/** Payload returned after updating a metadata item. */
export type UpdateMetadataItemPayload = {
  __typename?: 'UpdateMetadataItemPayload'
  /** Error message if the operation failed. */
  error?: Maybe<Scalars['String']['output']>
  /** The updated metadata item. */
  item?: Maybe<Item>
  query: Query
  /** Whether the operation was successful. */
  success: Scalars['Boolean']['output']
}

/**
 * Input for updating server-wide configuration settings.
 * All fields are optional; only specified fields will be updated.
 */
export type UpdateServerSettingsInput = {
  /** Gets a value indicating whether to allow HEVC encoding when transcoding video. */
  allowHEVCEncoding?: InputMaybe<Scalars['Boolean']['input']>
  /** Gets a value indicating whether to allow remuxing (container change without re-encoding). */
  allowRemuxing?: InputMaybe<Scalars['Boolean']['input']>
  /** Gets the list of allowed tags (empty = no allowlist). */
  allowedTags?: InputMaybe<Array<Scalars['String']['input']>>
  /** Gets the list of blocked tags (empty = no blocklist). */
  blockedTags?: InputMaybe<Array<Scalars['String']['input']>>
  /** Gets the default audio codec for DASH transcoding. */
  dashAudioCodec?: InputMaybe<Scalars['String']['input']>
  /** Gets the DASH segment duration in seconds. */
  dashSegmentDurationSeconds?: InputMaybe<Scalars['Int']['input']>
  /** Gets the default video codec for DASH transcoding. */
  dashVideoCodec?: InputMaybe<Scalars['String']['input']>
  /** Gets a value indicating whether tone mapping is enabled for HDR content. */
  enableToneMapping?: InputMaybe<Scalars['Boolean']['input']>
  /** Gets the genre normalization mappings (input → canonical). */
  genreMappings?: InputMaybe<Array<KeyValuePairOfStringAndStringInput>>
  /** Gets the minimum log level (Debug, Information, Warning, Error, Fatal). */
  logLevel?: InputMaybe<Scalars['String']['input']>
  /** Gets the maximum streaming bitrate in bits per second. */
  maxStreamingBitrate?: InputMaybe<Scalars['Int']['input']>
  /** Gets a value indicating whether to prefer H.265 (HEVC) codec for video transcoding. */
  preferH265?: InputMaybe<Scalars['Boolean']['input']>
  /** Gets the friendly display name of the server. */
  serverName?: InputMaybe<Scalars['String']['input']>
  /** Gets the user's preferred hardware acceleration (null = auto-detect). */
  userPreferredAcceleration?: InputMaybe<HardwareAccelerationKind>
}

/** Input for updating transcode settings. */
export type UpdateTranscodeSettingsInput = {
  /** Gets the default audio codec for DASH transcoding. */
  dashAudioCodec?: InputMaybe<Scalars['String']['input']>
  /** Gets the DASH segment duration in seconds. */
  dashSegmentDurationSeconds?: InputMaybe<Scalars['Int']['input']>
  /** Gets the default video codec for DASH transcoding. */
  dashVideoCodec?: InputMaybe<Scalars['String']['input']>
  /** Gets a value indicating whether tone mapping is enabled. */
  enableToneMapping?: InputMaybe<Scalars['Boolean']['input']>
  /** Gets the user's preferred hardware acceleration (null = use auto-detected). */
  userPreferredAcceleration?: InputMaybe<HardwareAccelerationKind>
}

export type AnalyzeItemMutationVariables = Exact<{
  itemId: Scalars['ID']['input']
}>

export type AnalyzeItemMutation = {
  __typename?: 'Mutation'
  analyzeItem: {
    __typename?: 'AnalyzeItemPayload'
    success: boolean
    error?: string | null
  }
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

export type StartLibraryScanMutationVariables = Exact<{
  librarySectionId: Scalars['ID']['input']
}>

export type StartLibraryScanMutation = {
  __typename?: 'Mutation'
  startLibraryScan: {
    __typename?: 'StartLibraryScanPayload'
    success: boolean
    error?: string | null
    scanId?: number | null
  }
}

export type RemoveLibrarySectionMutationVariables = Exact<{
  librarySectionId: Scalars['ID']['input']
}>

export type RemoveLibrarySectionMutation = {
  __typename?: 'Mutation'
  removeLibrarySection: {
    __typename?: 'RemoveLibrarySectionPayload'
    success: boolean
    error?: string | null
  }
}

export type UpdateMetadataItemMutationVariables = Exact<{
  input: UpdateMetadataItemInput
}>

export type UpdateMetadataItemMutation = {
  __typename?: 'Mutation'
  updateMetadataItem: {
    __typename?: 'UpdateMetadataItemPayload'
    success: boolean
    error?: string | null
    item?: {
      __typename?: 'Item'
      id: string
      title: string
      titleSort: string
      originalTitle: string
      summary: string
      tagline: string
      contentRating: string
      year: number
      originallyAvailableAt?: string | null
      genres: Array<string>
      tags: Array<string>
      lockedFields: Array<string>
      externalIds: Array<{
        __typename?: 'ExternalId'
        provider: string
        value: string
      }>
      extraFields: Array<{
        __typename?: 'ExtraField'
        key: string
        value?: any | null
      }>
    } | null
  }
}

export type LockMetadataFieldsMutationVariables = Exact<{
  input: LockMetadataFieldsInput
}>

export type LockMetadataFieldsMutation = {
  __typename?: 'Mutation'
  lockMetadataFields: {
    __typename?: 'MetadataFieldLockPayload'
    success: boolean
    error?: string | null
    lockedFields?: Array<string> | null
  }
}

export type UnlockMetadataFieldsMutationVariables = Exact<{
  input: UnlockMetadataFieldsInput
}>

export type UnlockMetadataFieldsMutation = {
  __typename?: 'Mutation'
  unlockMetadataFields: {
    __typename?: 'MetadataFieldLockPayload'
    success: boolean
    error?: string | null
    lockedFields?: Array<string> | null
  }
}

export type MetadataItemForEditQueryVariables = Exact<{
  id: Scalars['ID']['input']
}>

export type MetadataItemForEditQuery = {
  __typename?: 'Query'
  metadataItem?: {
    __typename?: 'Item'
    id: string
    metadataType: MetadataType
    title: string
    titleSort: string
    originalTitle: string
    summary: string
    tagline: string
    contentRating: string
    year: number
    originallyAvailableAt?: string | null
    genres: Array<string>
    tags: Array<string>
    lockedFields: Array<string>
    thumbUri?: string | null
    thumbHash?: string | null
    externalIds: Array<{
      __typename?: 'ExternalId'
      provider: string
      value: string
    }>
    extraFields: Array<{
      __typename?: 'ExtraField'
      key: string
      value?: any | null
    }>
  } | null
}

export type RefreshLibraryMetadataMutationVariables = Exact<{
  librarySectionId: Scalars['ID']['input']
}>

export type RefreshLibraryMetadataMutation = {
  __typename?: 'Mutation'
  refreshLibraryMetadata: {
    __typename?: 'RefreshMetadataPayload'
    success: boolean
    error?: string | null
  }
}

export type RefreshItemMetadataMutationVariables = Exact<{
  itemId: Scalars['ID']['input']
  includeChildren?: Scalars['Boolean']['input']
}>

export type RefreshItemMetadataMutation = {
  __typename?: 'Mutation'
  refreshItemMetadata: {
    __typename?: 'RefreshMetadataPayload'
    success: boolean
    error?: string | null
  }
}

export type PromoteItemMutationVariables = Exact<{
  itemId: Scalars['ID']['input']
  promotedUntil?: InputMaybe<Scalars['DateTime']['input']>
}>

export type PromoteItemMutation = {
  __typename?: 'Mutation'
  promoteItem: {
    __typename?: 'PromoteItemPayload'
    success: boolean
    error?: string | null
  }
}

export type UnpromoteItemMutationVariables = Exact<{
  itemId: Scalars['ID']['input']
}>

export type UnpromoteItemMutation = {
  __typename?: 'Mutation'
  unpromoteItem: {
    __typename?: 'PromoteItemPayload'
    success: boolean
    error?: string | null
  }
}

export type SearchQueryVariables = Exact<{
  query: Scalars['String']['input']
  pivot?: InputMaybe<SearchPivot>
  limit?: InputMaybe<Scalars['Int']['input']>
}>

export type SearchQuery = {
  __typename?: 'Query'
  search: Array<{
    __typename?: 'SearchResult'
    id: string
    title: string
    metadataType: MetadataType
    score: number
    year?: number | null
    thumbUri?: string | null
    librarySectionId: string
  }>
}

export type RestartServerMutationVariables = Exact<{ [key: string]: never }>

export type RestartServerMutation = {
  __typename?: 'Mutation'
  restartServer: boolean
}

export type ServerSettingsQueryVariables = Exact<{ [key: string]: never }>

export type ServerSettingsQuery = {
  __typename?: 'Query'
  serverSettings: {
    __typename?: 'ServerSettingsPayload'
    serverName: string
    maxStreamingBitrate: number
    preferH265: boolean
    allowRemuxing: boolean
    allowHEVCEncoding: boolean
    dashVideoCodec: string
    dashAudioCodec: string
    dashSegmentDurationSeconds: number
    enableToneMapping: boolean
    userPreferredAcceleration?: HardwareAccelerationKind | null
    allowedTags: Array<string>
    blockedTags: Array<string>
    logLevel: string
    genreMappings: Array<{
      __typename?: 'KeyValuePairOfStringAndString'
      key: string
      value: string
    }>
  }
}

export type UpdateServerSettingsMutationVariables = Exact<{
  input: UpdateServerSettingsInput
}>

export type UpdateServerSettingsMutation = {
  __typename?: 'Mutation'
  updateServerSettings: {
    __typename?: 'ServerSettingsPayload'
    serverName: string
    maxStreamingBitrate: number
    preferH265: boolean
    allowRemuxing: boolean
    allowHEVCEncoding: boolean
    dashVideoCodec: string
    dashAudioCodec: string
    dashSegmentDurationSeconds: number
    enableToneMapping: boolean
    userPreferredAcceleration?: HardwareAccelerationKind | null
    allowedTags: Array<string>
    blockedTags: Array<string>
    logLevel: string
    genreMappings: Array<{
      __typename?: 'KeyValuePairOfStringAndString'
      key: string
      value: string
    }>
  }
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
    timestamp: Date
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

export type AvailableMetadataAgentsQueryVariables = Exact<{
  libraryType: LibraryType
}>

export type AvailableMetadataAgentsQuery = {
  __typename?: 'Query'
  availableMetadataAgents: Array<{
    __typename?: 'MetadataAgentInfo'
    name: string
    displayName: string
    description: string
    category: MetadataAgentCategory
    defaultOrder: number
    supportedLibraryTypes: Array<LibraryType>
  }>
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

export type LibrarySectionBrowseOptionsQueryVariables = Exact<{
  contentSourceId: Scalars['ID']['input']
}>

export type LibrarySectionBrowseOptionsQuery = {
  __typename?: 'Query'
  librarySection?: {
    __typename?: 'LibrarySection'
    id: string
    availableRootItemTypes: Array<{
      __typename?: 'BrowsableItemType'
      displayName: string
      metadataTypes: Array<MetadataType>
    }>
    availableSortFields: Array<{
      __typename?: 'SortField'
      key: string
      displayName: string
      requiresUserData: boolean
    }>
  } | null
}

export type LibrarySectionChildrenQueryVariables = Exact<{
  contentSourceId: Scalars['ID']['input']
  metadataTypes: Array<MetadataType> | MetadataType
  skip?: InputMaybe<Scalars['Int']['input']>
  take?: InputMaybe<Scalars['Int']['input']>
  order?: InputMaybe<Array<ItemSortInput> | ItemSortInput>
}>

export type LibrarySectionChildrenQuery = {
  __typename?: 'Query'
  librarySection?: {
    __typename?: 'LibrarySection'
    id: string
    children?: {
      __typename?: 'ChildrenCollectionSegment'
      totalCount: number
      items?: Array<{
        __typename?: 'Item'
        id: string
        isPromoted: boolean
        librarySectionId: string
        title: string
        year: number
        thumbUri?: string | null
        metadataType: MetadataType
        length: number
        viewCount: number
        viewOffset: number
        primaryPerson?: {
          __typename?: 'Item'
          id: string
          title: string
          metadataType: MetadataType
        } | null
        persons: Array<{
          __typename?: 'Item'
          id: string
          title: string
          metadataType: MetadataType
        }>
      }> | null
      pageInfo: {
        __typename?: 'CollectionSegmentInfo'
        hasNextPage: boolean
        hasPreviousPage: boolean
      }
    } | null
  } | null
}

export type LibrarySectionLetterIndexQueryVariables = Exact<{
  contentSourceId: Scalars['ID']['input']
  metadataTypes: Array<MetadataType> | MetadataType
}>

export type LibrarySectionLetterIndexQuery = {
  __typename?: 'Query'
  librarySection?: {
    __typename?: 'LibrarySection'
    id: string
    letterIndex: Array<{
      __typename?: 'LetterIndexEntry'
      letter: string
      count: number
      firstItemOffset: number
    }>
  } | null
}

export type HomeHubDefinitionsQueryVariables = Exact<{ [key: string]: never }>

export type HomeHubDefinitionsQuery = {
  __typename?: 'Query'
  homeHubDefinitions: Array<{
    __typename?: 'HubDefinition'
    key: string
    type: HubType
    title: string
    metadataType: MetadataType
    widget: HubWidgetType
    filterValue?: string | null
    librarySectionId?: string | null
    contextId?: string | null
  }>
}

export type LibraryDiscoverHubDefinitionsQueryVariables = Exact<{
  librarySectionId: Scalars['ID']['input']
}>

export type LibraryDiscoverHubDefinitionsQuery = {
  __typename?: 'Query'
  libraryDiscoverHubDefinitions: Array<{
    __typename?: 'HubDefinition'
    key: string
    type: HubType
    title: string
    metadataType: MetadataType
    widget: HubWidgetType
    filterValue?: string | null
    librarySectionId?: string | null
    contextId?: string | null
  }>
}

export type ItemDetailHubDefinitionsQueryVariables = Exact<{
  itemId: Scalars['ID']['input']
}>

export type ItemDetailHubDefinitionsQuery = {
  __typename?: 'Query'
  itemDetailHubDefinitions: Array<{
    __typename?: 'HubDefinition'
    key: string
    type: HubType
    title: string
    metadataType: MetadataType
    widget: HubWidgetType
    filterValue?: string | null
    librarySectionId?: string | null
    contextId?: string | null
  }>
}

export type HubItemsQueryVariables = Exact<{
  input: GetHubItemsInput
}>

export type HubItemsQuery = {
  __typename?: 'Query'
  hubItems: Array<{
    __typename?: 'Item'
    id: string
    librarySectionId: string
    metadataType: MetadataType
    title: string
    year: number
    index: number
    length: number
    viewCount: number
    viewOffset: number
    thumbUri?: string | null
    thumbHash?: string | null
    artUri?: string | null
    artHash?: string | null
    logoUri?: string | null
    logoHash?: string | null
    tagline: string
    contentRating: string
    summary: string
    context?: string | null
    parent?: {
      __typename?: 'Item'
      id: string
      index: number
      title: string
    } | null
    primaryPerson?: {
      __typename?: 'Item'
      id: string
      title: string
      metadataType: MetadataType
    } | null
    persons: Array<{
      __typename?: 'Item'
      id: string
      title: string
      metadataType: MetadataType
    }>
  }>
}

export type HubPeopleQueryVariables = Exact<{
  hubType: HubType
  metadataItemId: Scalars['ID']['input']
}>

export type HubPeopleQuery = {
  __typename?: 'Query'
  hubPeople: Array<{
    __typename?: 'Item'
    id: string
    metadataType: MetadataType
    title: string
    thumbUri?: string | null
    thumbHash?: string | null
    context?: string | null
  }>
}

export type MetadataItemChildrenQueryVariables = Exact<{
  itemId: Scalars['ID']['input']
  skip?: InputMaybe<Scalars['Int']['input']>
  take?: InputMaybe<Scalars['Int']['input']>
}>

export type MetadataItemChildrenQuery = {
  __typename?: 'Query'
  metadataItem?: {
    __typename?: 'Item'
    id: string
    librarySectionId: string
    children?: {
      __typename?: 'ChildrenCollectionSegment'
      totalCount: number
      items?: Array<{
        __typename?: 'Item'
        id: string
        isPromoted: boolean
        librarySectionId: string
        metadataType: MetadataType
        title: string
        year: number
        index: number
        length: number
        viewCount: number
        viewOffset: number
        thumbUri?: string | null
        thumbHash?: string | null
        primaryPerson?: {
          __typename?: 'Item'
          id: string
          title: string
          metadataType: MetadataType
        } | null
        persons: Array<{
          __typename?: 'Item'
          id: string
          title: string
          metadataType: MetadataType
        }>
      }> | null
      pageInfo: {
        __typename?: 'CollectionSegmentInfo'
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
    librarySectionId: string
    title: string
    originalTitle: string
    thumbUri?: string | null
    thumbHash?: string | null
    artUri?: string | null
    artHash?: string | null
    metadataType: MetadataType
    year: number
    length: number
    genres: Array<string>
    tags: Array<string>
    contentRating: string
    viewCount: number
    viewOffset: number
    isPromoted: boolean
    primaryPerson?: {
      __typename?: 'Item'
      id: string
      title: string
      metadataType: MetadataType
    } | null
    persons: Array<{
      __typename?: 'Item'
      id: string
      title: string
      metadataType: MetadataType
    }>
    extraFields: Array<{
      __typename?: 'ExtraField'
      key: string
      value?: any | null
    }>
  } | null
}

export type ItemDetailFieldDefinitionsQueryVariables = Exact<{
  itemId: Scalars['ID']['input']
}>

export type ItemDetailFieldDefinitionsQuery = {
  __typename?: 'Query'
  itemDetailFieldDefinitions: Array<{
    __typename?: 'DetailFieldDefinition'
    key: string
    fieldType: DetailFieldType
    label: string
    widget: DetailFieldWidgetType
    sortOrder: number
    customFieldKey?: string | null
    groupKey?: string | null
  }>
}

export type FieldDefinitionsForTypeQueryVariables = Exact<{
  metadataType: MetadataType
}>

export type FieldDefinitionsForTypeQuery = {
  __typename?: 'Query'
  fieldDefinitionsForType: Array<{
    __typename?: 'DetailFieldDefinition'
    key: string
    fieldType: DetailFieldType
    label: string
    widget: DetailFieldWidgetType
    sortOrder: number
    customFieldKey?: string | null
    groupKey?: string | null
  }>
}

export type CustomFieldDefinitionsQueryVariables = Exact<{
  [key: string]: never
}>

export type CustomFieldDefinitionsQuery = {
  __typename?: 'Query'
  customFieldDefinitions: Array<{
    __typename?: 'CustomFieldDefinition'
    id: string
    key: string
    label: string
    widget: DetailFieldWidgetType
    applicableMetadataTypes: Array<MetadataType>
    sortOrder: number
    isEnabled: boolean
  }>
}

export type CreateCustomFieldDefinitionMutationVariables = Exact<{
  input: CreateCustomFieldDefinitionInput
}>

export type CreateCustomFieldDefinitionMutation = {
  __typename?: 'Mutation'
  createCustomFieldDefinition: {
    __typename?: 'CustomFieldDefinition'
    id: string
    key: string
    label: string
    widget: DetailFieldWidgetType
    applicableMetadataTypes: Array<MetadataType>
    sortOrder: number
    isEnabled: boolean
  }
}

export type UpdateCustomFieldDefinitionMutationVariables = Exact<{
  input: UpdateCustomFieldDefinitionInput
}>

export type UpdateCustomFieldDefinitionMutation = {
  __typename?: 'Mutation'
  updateCustomFieldDefinition: {
    __typename?: 'CustomFieldDefinition'
    id: string
    key: string
    label: string
    widget: DetailFieldWidgetType
    applicableMetadataTypes: Array<MetadataType>
    sortOrder: number
    isEnabled: boolean
  }
}

export type DeleteCustomFieldDefinitionMutationVariables = Exact<{
  id: Scalars['ID']['input']
}>

export type DeleteCustomFieldDefinitionMutation = {
  __typename?: 'Mutation'
  deleteCustomFieldDefinition: boolean
}

export type UpdateDetailFieldConfigurationMutationVariables = Exact<{
  input: UpdateDetailFieldConfigurationInput
}>

export type UpdateDetailFieldConfigurationMutation = {
  __typename?: 'Mutation'
  updateDetailFieldConfiguration: Array<{
    __typename?: 'DetailFieldDefinition'
    key: string
    fieldType: DetailFieldType
    label: string
    widget: DetailFieldWidgetType
    sortOrder: number
    customFieldKey?: string | null
  }>
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

export type DecidePlaybackMutationVariables = Exact<{
  input: PlaybackDecisionInput
}>

export type DecidePlaybackMutation = {
  __typename?: 'Mutation'
  decidePlayback: {
    __typename?: 'PlaybackDecisionPayload'
    action: string
    streamPlanJson: string
    nextItemId?: string | null
    nextItemTitle?: string | null
    nextItemOriginalTitle?: string | null
    nextItemParentTitle?: string | null
    nextItemThumbUrl?: string | null
    playbackUrl: string
    trickplayUrl?: string | null
    capabilityProfileVersion: number
    capabilityVersionMismatch: boolean
  }
}

export type PlaybackHeartbeatMutationVariables = Exact<{
  input: PlaybackHeartbeatInput
}>

export type PlaybackHeartbeatMutation = {
  __typename?: 'Mutation'
  playbackHeartbeat: {
    __typename?: 'PlaybackHeartbeatPayload'
    playbackSessionId: string
    capabilityProfileVersion: number
    capabilityVersionMismatch: boolean
  }
}

export type PlaylistChunkQueryVariables = Exact<{
  input: PlaylistChunkInput
}>

export type PlaylistChunkQuery = {
  __typename?: 'Query'
  playlistChunk: {
    __typename?: 'PlaylistChunkPayload'
    playlistGeneratorId: string
    currentIndex: number
    totalCount: number
    hasMore: boolean
    shuffle: boolean
    repeat: boolean
    items: Array<{
      __typename?: 'PlaylistItemPayload'
      itemEntryId: number
      itemId: string
      index: number
      served: boolean
      title: string
      metadataType: string
      durationMs?: BigInt | null
      playbackUrl?: string | null
      thumbUri?: string | null
      parentTitle?: string | null
      subtitle?: string | null
      primaryPerson?: {
        __typename?: 'Item'
        id: string
        title: string
        metadataType: MetadataType
      } | null
    }>
  }
}

export type PlaylistNextMutationVariables = Exact<{
  input: PlaylistNavigateInput
}>

export type PlaylistNextMutation = {
  __typename?: 'Mutation'
  playlistNext: {
    __typename?: 'PlaylistNavigatePayload'
    success: boolean
    shuffle: boolean
    repeat: boolean
    currentIndex: number
    totalCount: number
    currentItem?: {
      __typename?: 'PlaylistItemPayload'
      itemEntryId: number
      itemId: string
      index: number
      served: boolean
      title: string
      metadataType: string
      durationMs?: BigInt | null
      thumbUri?: string | null
      parentTitle?: string | null
      subtitle?: string | null
      primaryPerson?: {
        __typename?: 'Item'
        id: string
        title: string
        metadataType: MetadataType
      } | null
    } | null
  }
}

export type PlaylistPreviousMutationVariables = Exact<{
  input: PlaylistNavigateInput
}>

export type PlaylistPreviousMutation = {
  __typename?: 'Mutation'
  playlistPrevious: {
    __typename?: 'PlaylistNavigatePayload'
    success: boolean
    shuffle: boolean
    repeat: boolean
    currentIndex: number
    totalCount: number
    currentItem?: {
      __typename?: 'PlaylistItemPayload'
      itemEntryId: number
      itemId: string
      index: number
      served: boolean
      title: string
      metadataType: string
      durationMs?: BigInt | null
      thumbUri?: string | null
      parentTitle?: string | null
      subtitle?: string | null
      primaryPerson?: {
        __typename?: 'Item'
        id: string
        title: string
        metadataType: MetadataType
      } | null
    } | null
  }
}

export type PlaylistJumpMutationVariables = Exact<{
  input: PlaylistJumpInput
}>

export type PlaylistJumpMutation = {
  __typename?: 'Mutation'
  playlistJump: {
    __typename?: 'PlaylistNavigatePayload'
    success: boolean
    shuffle: boolean
    repeat: boolean
    currentIndex: number
    totalCount: number
    currentItem?: {
      __typename?: 'PlaylistItemPayload'
      itemEntryId: number
      itemId: string
      index: number
      served: boolean
      title: string
      metadataType: string
      durationMs?: BigInt | null
      playbackUrl?: string | null
      thumbUri?: string | null
      parentTitle?: string | null
      subtitle?: string | null
      primaryPerson?: {
        __typename?: 'Item'
        id: string
        title: string
        metadataType: MetadataType
      } | null
    } | null
  }
}

export type PlaylistSetShuffleMutationVariables = Exact<{
  input: PlaylistModeInput
}>

export type PlaylistSetShuffleMutation = {
  __typename?: 'Mutation'
  playlistSetShuffle: {
    __typename?: 'PlaylistNavigatePayload'
    success: boolean
    shuffle: boolean
    repeat: boolean
    currentIndex: number
    totalCount: number
  }
}

export type PlaylistSetRepeatMutationVariables = Exact<{
  input: PlaylistModeInput
}>

export type PlaylistSetRepeatMutation = {
  __typename?: 'Mutation'
  playlistSetRepeat: {
    __typename?: 'PlaylistNavigatePayload'
    success: boolean
    shuffle: boolean
    repeat: boolean
    currentIndex: number
    totalCount: number
  }
}

export type DecidePlaybackNavigationMutationVariables = Exact<{
  input: PlaybackDecisionInput
}>

export type DecidePlaybackNavigationMutation = {
  __typename?: 'Mutation'
  decidePlayback: {
    __typename?: 'PlaybackDecisionPayload'
    action: string
    streamPlanJson: string
    nextItemId?: string | null
    nextItemTitle?: string | null
    nextItemOriginalTitle?: string | null
    nextItemParentTitle?: string | null
    nextItemThumbUrl?: string | null
    playbackUrl: string
    trickplayUrl?: string | null
    capabilityProfileVersion: number
    capabilityVersionMismatch: boolean
  }
}

export type ResumePlaybackMutationVariables = Exact<{
  input: PlaybackResumeInput
}>

export type ResumePlaybackMutation = {
  __typename?: 'Mutation'
  resumePlayback: {
    __typename?: 'PlaybackResumePayload'
    playbackSessionId: string
    currentItemId: string
    playlistGeneratorId: string
    playheadMs: BigInt
    state: string
    capabilityProfileVersion: number
    capabilityVersionMismatch: boolean
    streamPlanJson: string
    playbackUrl: string
    trickplayUrl?: string | null
    durationMs?: BigInt | null
  }
}

export type StartPlaybackMutationVariables = Exact<{
  input: PlaybackStartInput
}>

export type StartPlaybackMutation = {
  __typename?: 'Mutation'
  startPlayback: {
    __typename?: 'PlaybackStartPayload'
    playbackSessionId: string
    playlistGeneratorId: string
    capabilityProfileVersion: number
    streamPlanJson: string
    playbackUrl: string
    trickplayUrl?: string | null
    durationMs?: BigInt | null
    capabilityVersionMismatch: boolean
    playlistIndex: number
    playlistTotalCount: number
    shuffle: boolean
    repeat: boolean
    currentItemId?: string | null
    currentItemMetadataType: string
    currentItemTitle?: string | null
    currentItemOriginalTitle?: string | null
    currentItemParentTitle?: string | null
    currentItemThumbUrl?: string | null
    currentItemParentThumbUrl?: string | null
  }
}

export type StopPlaybackMutationVariables = Exact<{
  input: PlaybackStopInput
}>

export type StopPlaybackMutation = {
  __typename?: 'Mutation'
  stopPlayback: { __typename?: 'PlaybackStopPayload'; success: boolean }
}

export type HubConfigurationQueryVariables = Exact<{
  input: HubConfigurationScopeInput
}>

export type HubConfigurationQuery = {
  __typename?: 'Query'
  hubConfiguration?: {
    __typename?: 'HubConfiguration'
    enabledHubTypes: Array<HubType>
    disabledHubTypes: Array<HubType>
  } | null
}

export type UpdateHubConfigurationMutationVariables = Exact<{
  input: UpdateHubConfigurationInput
}>

export type UpdateHubConfigurationMutation = {
  __typename?: 'Mutation'
  updateHubConfiguration: {
    __typename?: 'HubConfiguration'
    enabledHubTypes: Array<HubType>
    disabledHubTypes: Array<HubType>
  }
}

export type AdminDetailFieldConfigurationQueryVariables = Exact<{
  input: DetailFieldConfigurationScopeInput
}>

export type AdminDetailFieldConfigurationQuery = {
  __typename?: 'Query'
  adminDetailFieldConfiguration?: {
    __typename?: 'DetailFieldConfiguration'
    metadataType: MetadataType
    librarySectionId?: string | null
    enabledFieldTypes: Array<DetailFieldType>
    disabledFieldTypes: Array<DetailFieldType>
    disabledCustomFieldKeys: Array<string>
    fieldGroups?: Array<{
      __typename?: 'DetailFieldGroup'
      groupKey: string
      label: string
      layoutType: DetailFieldGroupLayoutType
      sortOrder: number
      isCollapsible: boolean
    }> | null
    fieldGroupAssignments?: Array<{
      __typename?: 'KeyValuePairOfStringAndString'
      key: string
      value: string
    }> | null
  } | null
}

export type UpdateAdminDetailFieldConfigurationMutationVariables = Exact<{
  input: UpdateAdminDetailFieldConfigurationInput
}>

export type UpdateAdminDetailFieldConfigurationMutation = {
  __typename?: 'Mutation'
  updateAdminDetailFieldConfiguration: {
    __typename?: 'DetailFieldConfiguration'
    metadataType: MetadataType
    librarySectionId?: string | null
    enabledFieldTypes: Array<DetailFieldType>
    disabledFieldTypes: Array<DetailFieldType>
    disabledCustomFieldKeys: Array<string>
    fieldGroups?: Array<{
      __typename?: 'DetailFieldGroup'
      groupKey: string
      label: string
      layoutType: DetailFieldGroupLayoutType
      sortOrder: number
      isCollapsible: boolean
    }> | null
    fieldGroupAssignments?: Array<{
      __typename?: 'KeyValuePairOfStringAndString'
      key: string
      value: string
    }> | null
  }
}

export type AdminLibrarySectionsListQueryVariables = Exact<{
  [key: string]: never
}>

export type AdminLibrarySectionsListQuery = {
  __typename?: 'Query'
  librarySections?: {
    __typename?: 'LibrarySectionsConnection'
    edges?: Array<{
      __typename?: 'LibrarySectionsEdge'
      node: {
        __typename?: 'LibrarySection'
        id: string
        name: string
        type: LibraryType
      }
    }> | null
  } | null
}

export const AnalyzeItemDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'mutation',
      name: { kind: 'Name', value: 'AnalyzeItem' },
      variableDefinitions: [
        {
          kind: 'VariableDefinition',
          variable: {
            kind: 'Variable',
            name: { kind: 'Name', value: 'itemId' },
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
            name: { kind: 'Name', value: 'analyzeItem' },
            arguments: [
              {
                kind: 'Argument',
                name: { kind: 'Name', value: 'input' },
                value: {
                  kind: 'ObjectValue',
                  fields: [
                    {
                      kind: 'ObjectField',
                      name: { kind: 'Name', value: 'itemId' },
                      value: {
                        kind: 'Variable',
                        name: { kind: 'Name', value: 'itemId' },
                      },
                    },
                  ],
                },
              },
            ],
            selectionSet: {
              kind: 'SelectionSet',
              selections: [
                { kind: 'Field', name: { kind: 'Name', value: 'success' } },
                { kind: 'Field', name: { kind: 'Name', value: 'error' } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<AnalyzeItemMutation, AnalyzeItemMutationVariables>
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
export const StartLibraryScanDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'mutation',
      name: { kind: 'Name', value: 'StartLibraryScan' },
      variableDefinitions: [
        {
          kind: 'VariableDefinition',
          variable: {
            kind: 'Variable',
            name: { kind: 'Name', value: 'librarySectionId' },
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
            name: { kind: 'Name', value: 'startLibraryScan' },
            arguments: [
              {
                kind: 'Argument',
                name: { kind: 'Name', value: 'input' },
                value: {
                  kind: 'ObjectValue',
                  fields: [
                    {
                      kind: 'ObjectField',
                      name: { kind: 'Name', value: 'librarySectionId' },
                      value: {
                        kind: 'Variable',
                        name: { kind: 'Name', value: 'librarySectionId' },
                      },
                    },
                  ],
                },
              },
            ],
            selectionSet: {
              kind: 'SelectionSet',
              selections: [
                { kind: 'Field', name: { kind: 'Name', value: 'success' } },
                { kind: 'Field', name: { kind: 'Name', value: 'error' } },
                { kind: 'Field', name: { kind: 'Name', value: 'scanId' } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  StartLibraryScanMutation,
  StartLibraryScanMutationVariables
>
export const RemoveLibrarySectionDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'mutation',
      name: { kind: 'Name', value: 'RemoveLibrarySection' },
      variableDefinitions: [
        {
          kind: 'VariableDefinition',
          variable: {
            kind: 'Variable',
            name: { kind: 'Name', value: 'librarySectionId' },
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
            name: { kind: 'Name', value: 'removeLibrarySection' },
            arguments: [
              {
                kind: 'Argument',
                name: { kind: 'Name', value: 'input' },
                value: {
                  kind: 'ObjectValue',
                  fields: [
                    {
                      kind: 'ObjectField',
                      name: { kind: 'Name', value: 'librarySectionId' },
                      value: {
                        kind: 'Variable',
                        name: { kind: 'Name', value: 'librarySectionId' },
                      },
                    },
                  ],
                },
              },
            ],
            selectionSet: {
              kind: 'SelectionSet',
              selections: [
                { kind: 'Field', name: { kind: 'Name', value: 'success' } },
                { kind: 'Field', name: { kind: 'Name', value: 'error' } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  RemoveLibrarySectionMutation,
  RemoveLibrarySectionMutationVariables
>
export const UpdateMetadataItemDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'mutation',
      name: { kind: 'Name', value: 'UpdateMetadataItem' },
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
              name: { kind: 'Name', value: 'UpdateMetadataItemInput' },
            },
          },
        },
      ],
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          {
            kind: 'Field',
            name: { kind: 'Name', value: 'updateMetadataItem' },
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
                { kind: 'Field', name: { kind: 'Name', value: 'success' } },
                { kind: 'Field', name: { kind: 'Name', value: 'error' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'item' },
                  selectionSet: {
                    kind: 'SelectionSet',
                    selections: [
                      { kind: 'Field', name: { kind: 'Name', value: 'id' } },
                      { kind: 'Field', name: { kind: 'Name', value: 'title' } },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'titleSort' },
                      },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'originalTitle' },
                      },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'summary' },
                      },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'tagline' },
                      },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'contentRating' },
                      },
                      { kind: 'Field', name: { kind: 'Name', value: 'year' } },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'originallyAvailableAt' },
                      },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'genres' },
                      },
                      { kind: 'Field', name: { kind: 'Name', value: 'tags' } },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'lockedFields' },
                      },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'externalIds' },
                        selectionSet: {
                          kind: 'SelectionSet',
                          selections: [
                            {
                              kind: 'Field',
                              name: { kind: 'Name', value: 'provider' },
                            },
                            {
                              kind: 'Field',
                              name: { kind: 'Name', value: 'value' },
                            },
                          ],
                        },
                      },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'extraFields' },
                        selectionSet: {
                          kind: 'SelectionSet',
                          selections: [
                            {
                              kind: 'Field',
                              name: { kind: 'Name', value: 'key' },
                            },
                            {
                              kind: 'Field',
                              name: { kind: 'Name', value: 'value' },
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
      },
    },
  ],
} as unknown as DocumentNode<
  UpdateMetadataItemMutation,
  UpdateMetadataItemMutationVariables
>
export const LockMetadataFieldsDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'mutation',
      name: { kind: 'Name', value: 'LockMetadataFields' },
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
              name: { kind: 'Name', value: 'LockMetadataFieldsInput' },
            },
          },
        },
      ],
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          {
            kind: 'Field',
            name: { kind: 'Name', value: 'lockMetadataFields' },
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
                { kind: 'Field', name: { kind: 'Name', value: 'success' } },
                { kind: 'Field', name: { kind: 'Name', value: 'error' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'lockedFields' },
                },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  LockMetadataFieldsMutation,
  LockMetadataFieldsMutationVariables
>
export const UnlockMetadataFieldsDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'mutation',
      name: { kind: 'Name', value: 'UnlockMetadataFields' },
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
              name: { kind: 'Name', value: 'UnlockMetadataFieldsInput' },
            },
          },
        },
      ],
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          {
            kind: 'Field',
            name: { kind: 'Name', value: 'unlockMetadataFields' },
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
                { kind: 'Field', name: { kind: 'Name', value: 'success' } },
                { kind: 'Field', name: { kind: 'Name', value: 'error' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'lockedFields' },
                },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  UnlockMetadataFieldsMutation,
  UnlockMetadataFieldsMutationVariables
>
export const MetadataItemForEditDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'query',
      name: { kind: 'Name', value: 'MetadataItemForEdit' },
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
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'metadataType' },
                },
                { kind: 'Field', name: { kind: 'Name', value: 'title' } },
                { kind: 'Field', name: { kind: 'Name', value: 'titleSort' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'originalTitle' },
                },
                { kind: 'Field', name: { kind: 'Name', value: 'summary' } },
                { kind: 'Field', name: { kind: 'Name', value: 'tagline' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'contentRating' },
                },
                { kind: 'Field', name: { kind: 'Name', value: 'year' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'originallyAvailableAt' },
                },
                { kind: 'Field', name: { kind: 'Name', value: 'genres' } },
                { kind: 'Field', name: { kind: 'Name', value: 'tags' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'lockedFields' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'externalIds' },
                  selectionSet: {
                    kind: 'SelectionSet',
                    selections: [
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'provider' },
                      },
                      { kind: 'Field', name: { kind: 'Name', value: 'value' } },
                    ],
                  },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'extraFields' },
                  selectionSet: {
                    kind: 'SelectionSet',
                    selections: [
                      { kind: 'Field', name: { kind: 'Name', value: 'key' } },
                      { kind: 'Field', name: { kind: 'Name', value: 'value' } },
                    ],
                  },
                },
                { kind: 'Field', name: { kind: 'Name', value: 'thumbUri' } },
                { kind: 'Field', name: { kind: 'Name', value: 'thumbHash' } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  MetadataItemForEditQuery,
  MetadataItemForEditQueryVariables
>
export const RefreshLibraryMetadataDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'mutation',
      name: { kind: 'Name', value: 'RefreshLibraryMetadata' },
      variableDefinitions: [
        {
          kind: 'VariableDefinition',
          variable: {
            kind: 'Variable',
            name: { kind: 'Name', value: 'librarySectionId' },
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
            name: { kind: 'Name', value: 'refreshLibraryMetadata' },
            arguments: [
              {
                kind: 'Argument',
                name: { kind: 'Name', value: 'input' },
                value: {
                  kind: 'ObjectValue',
                  fields: [
                    {
                      kind: 'ObjectField',
                      name: { kind: 'Name', value: 'librarySectionId' },
                      value: {
                        kind: 'Variable',
                        name: { kind: 'Name', value: 'librarySectionId' },
                      },
                    },
                  ],
                },
              },
            ],
            selectionSet: {
              kind: 'SelectionSet',
              selections: [
                { kind: 'Field', name: { kind: 'Name', value: 'success' } },
                { kind: 'Field', name: { kind: 'Name', value: 'error' } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  RefreshLibraryMetadataMutation,
  RefreshLibraryMetadataMutationVariables
>
export const RefreshItemMetadataDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'mutation',
      name: { kind: 'Name', value: 'RefreshItemMetadata' },
      variableDefinitions: [
        {
          kind: 'VariableDefinition',
          variable: {
            kind: 'Variable',
            name: { kind: 'Name', value: 'itemId' },
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
            name: { kind: 'Name', value: 'includeChildren' },
          },
          type: {
            kind: 'NonNullType',
            type: {
              kind: 'NamedType',
              name: { kind: 'Name', value: 'Boolean' },
            },
          },
          defaultValue: { kind: 'BooleanValue', value: true },
        },
      ],
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          {
            kind: 'Field',
            name: { kind: 'Name', value: 'refreshItemMetadata' },
            arguments: [
              {
                kind: 'Argument',
                name: { kind: 'Name', value: 'input' },
                value: {
                  kind: 'ObjectValue',
                  fields: [
                    {
                      kind: 'ObjectField',
                      name: { kind: 'Name', value: 'itemId' },
                      value: {
                        kind: 'Variable',
                        name: { kind: 'Name', value: 'itemId' },
                      },
                    },
                    {
                      kind: 'ObjectField',
                      name: { kind: 'Name', value: 'includeChildren' },
                      value: {
                        kind: 'Variable',
                        name: { kind: 'Name', value: 'includeChildren' },
                      },
                    },
                  ],
                },
              },
            ],
            selectionSet: {
              kind: 'SelectionSet',
              selections: [
                { kind: 'Field', name: { kind: 'Name', value: 'success' } },
                { kind: 'Field', name: { kind: 'Name', value: 'error' } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  RefreshItemMetadataMutation,
  RefreshItemMetadataMutationVariables
>
export const PromoteItemDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'mutation',
      name: { kind: 'Name', value: 'PromoteItem' },
      variableDefinitions: [
        {
          kind: 'VariableDefinition',
          variable: {
            kind: 'Variable',
            name: { kind: 'Name', value: 'itemId' },
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
            name: { kind: 'Name', value: 'promotedUntil' },
          },
          type: {
            kind: 'NamedType',
            name: { kind: 'Name', value: 'DateTime' },
          },
        },
      ],
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          {
            kind: 'Field',
            name: { kind: 'Name', value: 'promoteItem' },
            arguments: [
              {
                kind: 'Argument',
                name: { kind: 'Name', value: 'input' },
                value: {
                  kind: 'ObjectValue',
                  fields: [
                    {
                      kind: 'ObjectField',
                      name: { kind: 'Name', value: 'itemId' },
                      value: {
                        kind: 'Variable',
                        name: { kind: 'Name', value: 'itemId' },
                      },
                    },
                    {
                      kind: 'ObjectField',
                      name: { kind: 'Name', value: 'promotedUntil' },
                      value: {
                        kind: 'Variable',
                        name: { kind: 'Name', value: 'promotedUntil' },
                      },
                    },
                  ],
                },
              },
            ],
            selectionSet: {
              kind: 'SelectionSet',
              selections: [
                { kind: 'Field', name: { kind: 'Name', value: 'success' } },
                { kind: 'Field', name: { kind: 'Name', value: 'error' } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<PromoteItemMutation, PromoteItemMutationVariables>
export const UnpromoteItemDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'mutation',
      name: { kind: 'Name', value: 'UnpromoteItem' },
      variableDefinitions: [
        {
          kind: 'VariableDefinition',
          variable: {
            kind: 'Variable',
            name: { kind: 'Name', value: 'itemId' },
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
            name: { kind: 'Name', value: 'unpromoteItem' },
            arguments: [
              {
                kind: 'Argument',
                name: { kind: 'Name', value: 'input' },
                value: {
                  kind: 'ObjectValue',
                  fields: [
                    {
                      kind: 'ObjectField',
                      name: { kind: 'Name', value: 'itemId' },
                      value: {
                        kind: 'Variable',
                        name: { kind: 'Name', value: 'itemId' },
                      },
                    },
                  ],
                },
              },
            ],
            selectionSet: {
              kind: 'SelectionSet',
              selections: [
                { kind: 'Field', name: { kind: 'Name', value: 'success' } },
                { kind: 'Field', name: { kind: 'Name', value: 'error' } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  UnpromoteItemMutation,
  UnpromoteItemMutationVariables
>
export const SearchDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'query',
      name: { kind: 'Name', value: 'Search' },
      variableDefinitions: [
        {
          kind: 'VariableDefinition',
          variable: {
            kind: 'Variable',
            name: { kind: 'Name', value: 'query' },
          },
          type: {
            kind: 'NonNullType',
            type: {
              kind: 'NamedType',
              name: { kind: 'Name', value: 'String' },
            },
          },
        },
        {
          kind: 'VariableDefinition',
          variable: {
            kind: 'Variable',
            name: { kind: 'Name', value: 'pivot' },
          },
          type: {
            kind: 'NamedType',
            name: { kind: 'Name', value: 'SearchPivot' },
          },
        },
        {
          kind: 'VariableDefinition',
          variable: {
            kind: 'Variable',
            name: { kind: 'Name', value: 'limit' },
          },
          type: { kind: 'NamedType', name: { kind: 'Name', value: 'Int' } },
        },
      ],
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          {
            kind: 'Field',
            name: { kind: 'Name', value: 'search' },
            arguments: [
              {
                kind: 'Argument',
                name: { kind: 'Name', value: 'query' },
                value: {
                  kind: 'Variable',
                  name: { kind: 'Name', value: 'query' },
                },
              },
              {
                kind: 'Argument',
                name: { kind: 'Name', value: 'pivot' },
                value: {
                  kind: 'Variable',
                  name: { kind: 'Name', value: 'pivot' },
                },
              },
              {
                kind: 'Argument',
                name: { kind: 'Name', value: 'limit' },
                value: {
                  kind: 'Variable',
                  name: { kind: 'Name', value: 'limit' },
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
                  name: { kind: 'Name', value: 'metadataType' },
                },
                { kind: 'Field', name: { kind: 'Name', value: 'score' } },
                { kind: 'Field', name: { kind: 'Name', value: 'year' } },
                { kind: 'Field', name: { kind: 'Name', value: 'thumbUri' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'librarySectionId' },
                },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<SearchQuery, SearchQueryVariables>
export const RestartServerDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'mutation',
      name: { kind: 'Name', value: 'RestartServer' },
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          { kind: 'Field', name: { kind: 'Name', value: 'restartServer' } },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  RestartServerMutation,
  RestartServerMutationVariables
>
export const ServerSettingsDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'query',
      name: { kind: 'Name', value: 'ServerSettings' },
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          {
            kind: 'Field',
            name: { kind: 'Name', value: 'serverSettings' },
            selectionSet: {
              kind: 'SelectionSet',
              selections: [
                { kind: 'Field', name: { kind: 'Name', value: 'serverName' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'maxStreamingBitrate' },
                },
                { kind: 'Field', name: { kind: 'Name', value: 'preferH265' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'allowRemuxing' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'allowHEVCEncoding' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'dashVideoCodec' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'dashAudioCodec' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'dashSegmentDurationSeconds' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'enableToneMapping' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'userPreferredAcceleration' },
                },
                { kind: 'Field', name: { kind: 'Name', value: 'allowedTags' } },
                { kind: 'Field', name: { kind: 'Name', value: 'blockedTags' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'genreMappings' },
                  selectionSet: {
                    kind: 'SelectionSet',
                    selections: [
                      { kind: 'Field', name: { kind: 'Name', value: 'key' } },
                      { kind: 'Field', name: { kind: 'Name', value: 'value' } },
                    ],
                  },
                },
                { kind: 'Field', name: { kind: 'Name', value: 'logLevel' } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<ServerSettingsQuery, ServerSettingsQueryVariables>
export const UpdateServerSettingsDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'mutation',
      name: { kind: 'Name', value: 'UpdateServerSettings' },
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
              name: { kind: 'Name', value: 'UpdateServerSettingsInput' },
            },
          },
        },
      ],
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          {
            kind: 'Field',
            name: { kind: 'Name', value: 'updateServerSettings' },
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
                { kind: 'Field', name: { kind: 'Name', value: 'serverName' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'maxStreamingBitrate' },
                },
                { kind: 'Field', name: { kind: 'Name', value: 'preferH265' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'allowRemuxing' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'allowHEVCEncoding' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'dashVideoCodec' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'dashAudioCodec' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'dashSegmentDurationSeconds' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'enableToneMapping' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'userPreferredAcceleration' },
                },
                { kind: 'Field', name: { kind: 'Name', value: 'allowedTags' } },
                { kind: 'Field', name: { kind: 'Name', value: 'blockedTags' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'genreMappings' },
                  selectionSet: {
                    kind: 'SelectionSet',
                    selections: [
                      { kind: 'Field', name: { kind: 'Name', value: 'key' } },
                      { kind: 'Field', name: { kind: 'Name', value: 'value' } },
                    ],
                  },
                },
                { kind: 'Field', name: { kind: 'Name', value: 'logLevel' } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  UpdateServerSettingsMutation,
  UpdateServerSettingsMutationVariables
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
export const AvailableMetadataAgentsDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'query',
      name: { kind: 'Name', value: 'AvailableMetadataAgents' },
      variableDefinitions: [
        {
          kind: 'VariableDefinition',
          variable: {
            kind: 'Variable',
            name: { kind: 'Name', value: 'libraryType' },
          },
          type: {
            kind: 'NonNullType',
            type: {
              kind: 'NamedType',
              name: { kind: 'Name', value: 'LibraryType' },
            },
          },
        },
      ],
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          {
            kind: 'Field',
            name: { kind: 'Name', value: 'availableMetadataAgents' },
            arguments: [
              {
                kind: 'Argument',
                name: { kind: 'Name', value: 'libraryType' },
                value: {
                  kind: 'Variable',
                  name: { kind: 'Name', value: 'libraryType' },
                },
              },
            ],
            selectionSet: {
              kind: 'SelectionSet',
              selections: [
                { kind: 'Field', name: { kind: 'Name', value: 'name' } },
                { kind: 'Field', name: { kind: 'Name', value: 'displayName' } },
                { kind: 'Field', name: { kind: 'Name', value: 'description' } },
                { kind: 'Field', name: { kind: 'Name', value: 'category' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'defaultOrder' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'supportedLibraryTypes' },
                },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  AvailableMetadataAgentsQuery,
  AvailableMetadataAgentsQueryVariables
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
export const LibrarySectionBrowseOptionsDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'query',
      name: { kind: 'Name', value: 'LibrarySectionBrowseOptions' },
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
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'availableRootItemTypes' },
                  selectionSet: {
                    kind: 'SelectionSet',
                    selections: [
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'displayName' },
                      },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'metadataTypes' },
                      },
                    ],
                  },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'availableSortFields' },
                  selectionSet: {
                    kind: 'SelectionSet',
                    selections: [
                      { kind: 'Field', name: { kind: 'Name', value: 'key' } },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'displayName' },
                      },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'requiresUserData' },
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
  LibrarySectionBrowseOptionsQuery,
  LibrarySectionBrowseOptionsQueryVariables
>
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
            name: { kind: 'Name', value: 'metadataTypes' },
          },
          type: {
            kind: 'NonNullType',
            type: {
              kind: 'ListType',
              type: {
                kind: 'NonNullType',
                type: {
                  kind: 'NamedType',
                  name: { kind: 'Name', value: 'MetadataType' },
                },
              },
            },
          },
        },
        {
          kind: 'VariableDefinition',
          variable: { kind: 'Variable', name: { kind: 'Name', value: 'skip' } },
          type: { kind: 'NamedType', name: { kind: 'Name', value: 'Int' } },
        },
        {
          kind: 'VariableDefinition',
          variable: { kind: 'Variable', name: { kind: 'Name', value: 'take' } },
          type: { kind: 'NamedType', name: { kind: 'Name', value: 'Int' } },
        },
        {
          kind: 'VariableDefinition',
          variable: {
            kind: 'Variable',
            name: { kind: 'Name', value: 'order' },
          },
          type: {
            kind: 'ListType',
            type: {
              kind: 'NonNullType',
              type: {
                kind: 'NamedType',
                name: { kind: 'Name', value: 'ItemSortInput' },
              },
            },
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
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'children' },
                  arguments: [
                    {
                      kind: 'Argument',
                      name: { kind: 'Name', value: 'metadataTypes' },
                      value: {
                        kind: 'Variable',
                        name: { kind: 'Name', value: 'metadataTypes' },
                      },
                    },
                    {
                      kind: 'Argument',
                      name: { kind: 'Name', value: 'skip' },
                      value: {
                        kind: 'Variable',
                        name: { kind: 'Name', value: 'skip' },
                      },
                    },
                    {
                      kind: 'Argument',
                      name: { kind: 'Name', value: 'take' },
                      value: {
                        kind: 'Variable',
                        name: { kind: 'Name', value: 'take' },
                      },
                    },
                    {
                      kind: 'Argument',
                      name: { kind: 'Name', value: 'order' },
                      value: {
                        kind: 'Variable',
                        name: { kind: 'Name', value: 'order' },
                      },
                    },
                  ],
                  selectionSet: {
                    kind: 'SelectionSet',
                    selections: [
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'items' },
                        selectionSet: {
                          kind: 'SelectionSet',
                          selections: [
                            {
                              kind: 'Field',
                              name: { kind: 'Name', value: 'id' },
                            },
                            {
                              kind: 'Field',
                              name: { kind: 'Name', value: 'isPromoted' },
                            },
                            {
                              kind: 'Field',
                              name: { kind: 'Name', value: 'librarySectionId' },
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
                              name: { kind: 'Name', value: 'thumbUri' },
                            },
                            {
                              kind: 'Field',
                              name: { kind: 'Name', value: 'metadataType' },
                            },
                            {
                              kind: 'Field',
                              name: { kind: 'Name', value: 'length' },
                            },
                            {
                              kind: 'Field',
                              name: { kind: 'Name', value: 'viewCount' },
                            },
                            {
                              kind: 'Field',
                              name: { kind: 'Name', value: 'viewOffset' },
                            },
                            {
                              kind: 'Field',
                              name: { kind: 'Name', value: 'primaryPerson' },
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
                                    name: {
                                      kind: 'Name',
                                      value: 'metadataType',
                                    },
                                  },
                                ],
                              },
                            },
                            {
                              kind: 'Field',
                              name: { kind: 'Name', value: 'persons' },
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
                                    name: {
                                      kind: 'Name',
                                      value: 'metadataType',
                                    },
                                  },
                                ],
                              },
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
export const LibrarySectionLetterIndexDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'query',
      name: { kind: 'Name', value: 'LibrarySectionLetterIndex' },
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
            name: { kind: 'Name', value: 'metadataTypes' },
          },
          type: {
            kind: 'NonNullType',
            type: {
              kind: 'ListType',
              type: {
                kind: 'NonNullType',
                type: {
                  kind: 'NamedType',
                  name: { kind: 'Name', value: 'MetadataType' },
                },
              },
            },
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
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'letterIndex' },
                  arguments: [
                    {
                      kind: 'Argument',
                      name: { kind: 'Name', value: 'metadataTypes' },
                      value: {
                        kind: 'Variable',
                        name: { kind: 'Name', value: 'metadataTypes' },
                      },
                    },
                  ],
                  selectionSet: {
                    kind: 'SelectionSet',
                    selections: [
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'letter' },
                      },
                      { kind: 'Field', name: { kind: 'Name', value: 'count' } },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'firstItemOffset' },
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
  LibrarySectionLetterIndexQuery,
  LibrarySectionLetterIndexQueryVariables
>
export const HomeHubDefinitionsDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'query',
      name: { kind: 'Name', value: 'HomeHubDefinitions' },
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          {
            kind: 'Field',
            name: { kind: 'Name', value: 'homeHubDefinitions' },
            selectionSet: {
              kind: 'SelectionSet',
              selections: [
                { kind: 'Field', name: { kind: 'Name', value: 'key' } },
                { kind: 'Field', name: { kind: 'Name', value: 'type' } },
                { kind: 'Field', name: { kind: 'Name', value: 'title' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'metadataType' },
                },
                { kind: 'Field', name: { kind: 'Name', value: 'widget' } },
                { kind: 'Field', name: { kind: 'Name', value: 'filterValue' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'librarySectionId' },
                },
                { kind: 'Field', name: { kind: 'Name', value: 'contextId' } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  HomeHubDefinitionsQuery,
  HomeHubDefinitionsQueryVariables
>
export const LibraryDiscoverHubDefinitionsDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'query',
      name: { kind: 'Name', value: 'LibraryDiscoverHubDefinitions' },
      variableDefinitions: [
        {
          kind: 'VariableDefinition',
          variable: {
            kind: 'Variable',
            name: { kind: 'Name', value: 'librarySectionId' },
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
            name: { kind: 'Name', value: 'libraryDiscoverHubDefinitions' },
            arguments: [
              {
                kind: 'Argument',
                name: { kind: 'Name', value: 'librarySectionId' },
                value: {
                  kind: 'Variable',
                  name: { kind: 'Name', value: 'librarySectionId' },
                },
              },
            ],
            selectionSet: {
              kind: 'SelectionSet',
              selections: [
                { kind: 'Field', name: { kind: 'Name', value: 'key' } },
                { kind: 'Field', name: { kind: 'Name', value: 'type' } },
                { kind: 'Field', name: { kind: 'Name', value: 'title' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'metadataType' },
                },
                { kind: 'Field', name: { kind: 'Name', value: 'widget' } },
                { kind: 'Field', name: { kind: 'Name', value: 'filterValue' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'librarySectionId' },
                },
                { kind: 'Field', name: { kind: 'Name', value: 'contextId' } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  LibraryDiscoverHubDefinitionsQuery,
  LibraryDiscoverHubDefinitionsQueryVariables
>
export const ItemDetailHubDefinitionsDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'query',
      name: { kind: 'Name', value: 'ItemDetailHubDefinitions' },
      variableDefinitions: [
        {
          kind: 'VariableDefinition',
          variable: {
            kind: 'Variable',
            name: { kind: 'Name', value: 'itemId' },
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
            name: { kind: 'Name', value: 'itemDetailHubDefinitions' },
            arguments: [
              {
                kind: 'Argument',
                name: { kind: 'Name', value: 'itemId' },
                value: {
                  kind: 'Variable',
                  name: { kind: 'Name', value: 'itemId' },
                },
              },
            ],
            selectionSet: {
              kind: 'SelectionSet',
              selections: [
                { kind: 'Field', name: { kind: 'Name', value: 'key' } },
                { kind: 'Field', name: { kind: 'Name', value: 'type' } },
                { kind: 'Field', name: { kind: 'Name', value: 'title' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'metadataType' },
                },
                { kind: 'Field', name: { kind: 'Name', value: 'widget' } },
                { kind: 'Field', name: { kind: 'Name', value: 'filterValue' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'librarySectionId' },
                },
                { kind: 'Field', name: { kind: 'Name', value: 'contextId' } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  ItemDetailHubDefinitionsQuery,
  ItemDetailHubDefinitionsQueryVariables
>
export const HubItemsDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'query',
      name: { kind: 'Name', value: 'HubItems' },
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
              name: { kind: 'Name', value: 'GetHubItemsInput' },
            },
          },
        },
      ],
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          {
            kind: 'Field',
            name: { kind: 'Name', value: 'hubItems' },
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
                { kind: 'Field', name: { kind: 'Name', value: 'id' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'librarySectionId' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'metadataType' },
                },
                { kind: 'Field', name: { kind: 'Name', value: 'title' } },
                { kind: 'Field', name: { kind: 'Name', value: 'year' } },
                { kind: 'Field', name: { kind: 'Name', value: 'index' } },
                { kind: 'Field', name: { kind: 'Name', value: 'length' } },
                { kind: 'Field', name: { kind: 'Name', value: 'viewCount' } },
                { kind: 'Field', name: { kind: 'Name', value: 'viewOffset' } },
                { kind: 'Field', name: { kind: 'Name', value: 'thumbUri' } },
                { kind: 'Field', name: { kind: 'Name', value: 'thumbHash' } },
                { kind: 'Field', name: { kind: 'Name', value: 'artUri' } },
                { kind: 'Field', name: { kind: 'Name', value: 'artHash' } },
                { kind: 'Field', name: { kind: 'Name', value: 'logoUri' } },
                { kind: 'Field', name: { kind: 'Name', value: 'logoHash' } },
                { kind: 'Field', name: { kind: 'Name', value: 'tagline' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'contentRating' },
                },
                { kind: 'Field', name: { kind: 'Name', value: 'summary' } },
                { kind: 'Field', name: { kind: 'Name', value: 'context' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'parent' },
                  selectionSet: {
                    kind: 'SelectionSet',
                    selections: [
                      { kind: 'Field', name: { kind: 'Name', value: 'id' } },
                      { kind: 'Field', name: { kind: 'Name', value: 'index' } },
                      { kind: 'Field', name: { kind: 'Name', value: 'title' } },
                    ],
                  },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'primaryPerson' },
                  selectionSet: {
                    kind: 'SelectionSet',
                    selections: [
                      { kind: 'Field', name: { kind: 'Name', value: 'id' } },
                      { kind: 'Field', name: { kind: 'Name', value: 'title' } },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'metadataType' },
                      },
                    ],
                  },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'persons' },
                  selectionSet: {
                    kind: 'SelectionSet',
                    selections: [
                      { kind: 'Field', name: { kind: 'Name', value: 'id' } },
                      { kind: 'Field', name: { kind: 'Name', value: 'title' } },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'metadataType' },
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
} as unknown as DocumentNode<HubItemsQuery, HubItemsQueryVariables>
export const HubPeopleDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'query',
      name: { kind: 'Name', value: 'HubPeople' },
      variableDefinitions: [
        {
          kind: 'VariableDefinition',
          variable: {
            kind: 'Variable',
            name: { kind: 'Name', value: 'hubType' },
          },
          type: {
            kind: 'NonNullType',
            type: {
              kind: 'NamedType',
              name: { kind: 'Name', value: 'HubType' },
            },
          },
        },
        {
          kind: 'VariableDefinition',
          variable: {
            kind: 'Variable',
            name: { kind: 'Name', value: 'metadataItemId' },
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
            name: { kind: 'Name', value: 'hubPeople' },
            arguments: [
              {
                kind: 'Argument',
                name: { kind: 'Name', value: 'hubType' },
                value: {
                  kind: 'Variable',
                  name: { kind: 'Name', value: 'hubType' },
                },
              },
              {
                kind: 'Argument',
                name: { kind: 'Name', value: 'metadataItemId' },
                value: {
                  kind: 'Variable',
                  name: { kind: 'Name', value: 'metadataItemId' },
                },
              },
            ],
            selectionSet: {
              kind: 'SelectionSet',
              selections: [
                { kind: 'Field', name: { kind: 'Name', value: 'id' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'metadataType' },
                },
                { kind: 'Field', name: { kind: 'Name', value: 'title' } },
                { kind: 'Field', name: { kind: 'Name', value: 'thumbUri' } },
                { kind: 'Field', name: { kind: 'Name', value: 'thumbHash' } },
                { kind: 'Field', name: { kind: 'Name', value: 'context' } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<HubPeopleQuery, HubPeopleQueryVariables>
export const MetadataItemChildrenDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'query',
      name: { kind: 'Name', value: 'MetadataItemChildren' },
      variableDefinitions: [
        {
          kind: 'VariableDefinition',
          variable: {
            kind: 'Variable',
            name: { kind: 'Name', value: 'itemId' },
          },
          type: {
            kind: 'NonNullType',
            type: { kind: 'NamedType', name: { kind: 'Name', value: 'ID' } },
          },
        },
        {
          kind: 'VariableDefinition',
          variable: { kind: 'Variable', name: { kind: 'Name', value: 'skip' } },
          type: { kind: 'NamedType', name: { kind: 'Name', value: 'Int' } },
        },
        {
          kind: 'VariableDefinition',
          variable: { kind: 'Variable', name: { kind: 'Name', value: 'take' } },
          type: { kind: 'NamedType', name: { kind: 'Name', value: 'Int' } },
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
                  name: { kind: 'Name', value: 'itemId' },
                },
              },
            ],
            selectionSet: {
              kind: 'SelectionSet',
              selections: [
                { kind: 'Field', name: { kind: 'Name', value: 'id' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'librarySectionId' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'children' },
                  arguments: [
                    {
                      kind: 'Argument',
                      name: { kind: 'Name', value: 'skip' },
                      value: {
                        kind: 'Variable',
                        name: { kind: 'Name', value: 'skip' },
                      },
                    },
                    {
                      kind: 'Argument',
                      name: { kind: 'Name', value: 'take' },
                      value: {
                        kind: 'Variable',
                        name: { kind: 'Name', value: 'take' },
                      },
                    },
                  ],
                  selectionSet: {
                    kind: 'SelectionSet',
                    selections: [
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'items' },
                        selectionSet: {
                          kind: 'SelectionSet',
                          selections: [
                            {
                              kind: 'Field',
                              name: { kind: 'Name', value: 'id' },
                            },
                            {
                              kind: 'Field',
                              name: { kind: 'Name', value: 'isPromoted' },
                            },
                            {
                              kind: 'Field',
                              name: { kind: 'Name', value: 'librarySectionId' },
                            },
                            {
                              kind: 'Field',
                              name: { kind: 'Name', value: 'metadataType' },
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
                              name: { kind: 'Name', value: 'index' },
                            },
                            {
                              kind: 'Field',
                              name: { kind: 'Name', value: 'length' },
                            },
                            {
                              kind: 'Field',
                              name: { kind: 'Name', value: 'viewCount' },
                            },
                            {
                              kind: 'Field',
                              name: { kind: 'Name', value: 'viewOffset' },
                            },
                            {
                              kind: 'Field',
                              name: { kind: 'Name', value: 'thumbUri' },
                            },
                            {
                              kind: 'Field',
                              name: { kind: 'Name', value: 'thumbHash' },
                            },
                            {
                              kind: 'Field',
                              name: { kind: 'Name', value: 'primaryPerson' },
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
                                    name: {
                                      kind: 'Name',
                                      value: 'metadataType',
                                    },
                                  },
                                ],
                              },
                            },
                            {
                              kind: 'Field',
                              name: { kind: 'Name', value: 'persons' },
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
                                    name: {
                                      kind: 'Name',
                                      value: 'metadataType',
                                    },
                                  },
                                ],
                              },
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
  MetadataItemChildrenQuery,
  MetadataItemChildrenQueryVariables
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
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'librarySectionId' },
                },
                { kind: 'Field', name: { kind: 'Name', value: 'title' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'originalTitle' },
                },
                { kind: 'Field', name: { kind: 'Name', value: 'thumbUri' } },
                { kind: 'Field', name: { kind: 'Name', value: 'thumbHash' } },
                { kind: 'Field', name: { kind: 'Name', value: 'artUri' } },
                { kind: 'Field', name: { kind: 'Name', value: 'artHash' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'metadataType' },
                },
                { kind: 'Field', name: { kind: 'Name', value: 'year' } },
                { kind: 'Field', name: { kind: 'Name', value: 'length' } },
                { kind: 'Field', name: { kind: 'Name', value: 'genres' } },
                { kind: 'Field', name: { kind: 'Name', value: 'tags' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'contentRating' },
                },
                { kind: 'Field', name: { kind: 'Name', value: 'viewCount' } },
                { kind: 'Field', name: { kind: 'Name', value: 'viewOffset' } },
                { kind: 'Field', name: { kind: 'Name', value: 'isPromoted' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'primaryPerson' },
                  selectionSet: {
                    kind: 'SelectionSet',
                    selections: [
                      { kind: 'Field', name: { kind: 'Name', value: 'id' } },
                      { kind: 'Field', name: { kind: 'Name', value: 'title' } },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'metadataType' },
                      },
                    ],
                  },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'persons' },
                  selectionSet: {
                    kind: 'SelectionSet',
                    selections: [
                      { kind: 'Field', name: { kind: 'Name', value: 'id' } },
                      { kind: 'Field', name: { kind: 'Name', value: 'title' } },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'metadataType' },
                      },
                    ],
                  },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'extraFields' },
                  selectionSet: {
                    kind: 'SelectionSet',
                    selections: [
                      { kind: 'Field', name: { kind: 'Name', value: 'key' } },
                      { kind: 'Field', name: { kind: 'Name', value: 'value' } },
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
} as unknown as DocumentNode<MediaQuery, MediaQueryVariables>
export const ItemDetailFieldDefinitionsDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'query',
      name: { kind: 'Name', value: 'ItemDetailFieldDefinitions' },
      variableDefinitions: [
        {
          kind: 'VariableDefinition',
          variable: {
            kind: 'Variable',
            name: { kind: 'Name', value: 'itemId' },
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
            name: { kind: 'Name', value: 'itemDetailFieldDefinitions' },
            arguments: [
              {
                kind: 'Argument',
                name: { kind: 'Name', value: 'itemId' },
                value: {
                  kind: 'Variable',
                  name: { kind: 'Name', value: 'itemId' },
                },
              },
            ],
            selectionSet: {
              kind: 'SelectionSet',
              selections: [
                { kind: 'Field', name: { kind: 'Name', value: 'key' } },
                { kind: 'Field', name: { kind: 'Name', value: 'fieldType' } },
                { kind: 'Field', name: { kind: 'Name', value: 'label' } },
                { kind: 'Field', name: { kind: 'Name', value: 'widget' } },
                { kind: 'Field', name: { kind: 'Name', value: 'sortOrder' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'customFieldKey' },
                },
                { kind: 'Field', name: { kind: 'Name', value: 'groupKey' } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  ItemDetailFieldDefinitionsQuery,
  ItemDetailFieldDefinitionsQueryVariables
>
export const FieldDefinitionsForTypeDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'query',
      name: { kind: 'Name', value: 'FieldDefinitionsForType' },
      variableDefinitions: [
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
      ],
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          {
            kind: 'Field',
            name: { kind: 'Name', value: 'fieldDefinitionsForType' },
            arguments: [
              {
                kind: 'Argument',
                name: { kind: 'Name', value: 'metadataType' },
                value: {
                  kind: 'Variable',
                  name: { kind: 'Name', value: 'metadataType' },
                },
              },
            ],
            selectionSet: {
              kind: 'SelectionSet',
              selections: [
                { kind: 'Field', name: { kind: 'Name', value: 'key' } },
                { kind: 'Field', name: { kind: 'Name', value: 'fieldType' } },
                { kind: 'Field', name: { kind: 'Name', value: 'label' } },
                { kind: 'Field', name: { kind: 'Name', value: 'widget' } },
                { kind: 'Field', name: { kind: 'Name', value: 'sortOrder' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'customFieldKey' },
                },
                { kind: 'Field', name: { kind: 'Name', value: 'groupKey' } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  FieldDefinitionsForTypeQuery,
  FieldDefinitionsForTypeQueryVariables
>
export const CustomFieldDefinitionsDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'query',
      name: { kind: 'Name', value: 'CustomFieldDefinitions' },
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          {
            kind: 'Field',
            name: { kind: 'Name', value: 'customFieldDefinitions' },
            selectionSet: {
              kind: 'SelectionSet',
              selections: [
                { kind: 'Field', name: { kind: 'Name', value: 'id' } },
                { kind: 'Field', name: { kind: 'Name', value: 'key' } },
                { kind: 'Field', name: { kind: 'Name', value: 'label' } },
                { kind: 'Field', name: { kind: 'Name', value: 'widget' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'applicableMetadataTypes' },
                },
                { kind: 'Field', name: { kind: 'Name', value: 'sortOrder' } },
                { kind: 'Field', name: { kind: 'Name', value: 'isEnabled' } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  CustomFieldDefinitionsQuery,
  CustomFieldDefinitionsQueryVariables
>
export const CreateCustomFieldDefinitionDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'mutation',
      name: { kind: 'Name', value: 'CreateCustomFieldDefinition' },
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
              name: { kind: 'Name', value: 'CreateCustomFieldDefinitionInput' },
            },
          },
        },
      ],
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          {
            kind: 'Field',
            name: { kind: 'Name', value: 'createCustomFieldDefinition' },
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
                { kind: 'Field', name: { kind: 'Name', value: 'id' } },
                { kind: 'Field', name: { kind: 'Name', value: 'key' } },
                { kind: 'Field', name: { kind: 'Name', value: 'label' } },
                { kind: 'Field', name: { kind: 'Name', value: 'widget' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'applicableMetadataTypes' },
                },
                { kind: 'Field', name: { kind: 'Name', value: 'sortOrder' } },
                { kind: 'Field', name: { kind: 'Name', value: 'isEnabled' } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  CreateCustomFieldDefinitionMutation,
  CreateCustomFieldDefinitionMutationVariables
>
export const UpdateCustomFieldDefinitionDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'mutation',
      name: { kind: 'Name', value: 'UpdateCustomFieldDefinition' },
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
              name: { kind: 'Name', value: 'UpdateCustomFieldDefinitionInput' },
            },
          },
        },
      ],
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          {
            kind: 'Field',
            name: { kind: 'Name', value: 'updateCustomFieldDefinition' },
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
                { kind: 'Field', name: { kind: 'Name', value: 'id' } },
                { kind: 'Field', name: { kind: 'Name', value: 'key' } },
                { kind: 'Field', name: { kind: 'Name', value: 'label' } },
                { kind: 'Field', name: { kind: 'Name', value: 'widget' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'applicableMetadataTypes' },
                },
                { kind: 'Field', name: { kind: 'Name', value: 'sortOrder' } },
                { kind: 'Field', name: { kind: 'Name', value: 'isEnabled' } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  UpdateCustomFieldDefinitionMutation,
  UpdateCustomFieldDefinitionMutationVariables
>
export const DeleteCustomFieldDefinitionDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'mutation',
      name: { kind: 'Name', value: 'DeleteCustomFieldDefinition' },
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
            name: { kind: 'Name', value: 'deleteCustomFieldDefinition' },
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
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  DeleteCustomFieldDefinitionMutation,
  DeleteCustomFieldDefinitionMutationVariables
>
export const UpdateDetailFieldConfigurationDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'mutation',
      name: { kind: 'Name', value: 'UpdateDetailFieldConfiguration' },
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
              name: {
                kind: 'Name',
                value: 'UpdateDetailFieldConfigurationInput',
              },
            },
          },
        },
      ],
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          {
            kind: 'Field',
            name: { kind: 'Name', value: 'updateDetailFieldConfiguration' },
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
                { kind: 'Field', name: { kind: 'Name', value: 'key' } },
                { kind: 'Field', name: { kind: 'Name', value: 'fieldType' } },
                { kind: 'Field', name: { kind: 'Name', value: 'label' } },
                { kind: 'Field', name: { kind: 'Name', value: 'widget' } },
                { kind: 'Field', name: { kind: 'Name', value: 'sortOrder' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'customFieldKey' },
                },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  UpdateDetailFieldConfigurationMutation,
  UpdateDetailFieldConfigurationMutationVariables
>
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
export const DecidePlaybackDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'mutation',
      name: { kind: 'Name', value: 'DecidePlayback' },
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
              name: { kind: 'Name', value: 'PlaybackDecisionInput' },
            },
          },
        },
      ],
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          {
            kind: 'Field',
            name: { kind: 'Name', value: 'decidePlayback' },
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
                { kind: 'Field', name: { kind: 'Name', value: 'action' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'streamPlanJson' },
                },
                { kind: 'Field', name: { kind: 'Name', value: 'nextItemId' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'nextItemTitle' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'nextItemOriginalTitle' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'nextItemParentTitle' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'nextItemThumbUrl' },
                },
                { kind: 'Field', name: { kind: 'Name', value: 'playbackUrl' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'trickplayUrl' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'capabilityProfileVersion' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'capabilityVersionMismatch' },
                },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  DecidePlaybackMutation,
  DecidePlaybackMutationVariables
>
export const PlaybackHeartbeatDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'mutation',
      name: { kind: 'Name', value: 'PlaybackHeartbeat' },
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
              name: { kind: 'Name', value: 'PlaybackHeartbeatInput' },
            },
          },
        },
      ],
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          {
            kind: 'Field',
            name: { kind: 'Name', value: 'playbackHeartbeat' },
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
                  name: { kind: 'Name', value: 'playbackSessionId' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'capabilityProfileVersion' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'capabilityVersionMismatch' },
                },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  PlaybackHeartbeatMutation,
  PlaybackHeartbeatMutationVariables
>
export const PlaylistChunkDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'query',
      name: { kind: 'Name', value: 'PlaylistChunk' },
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
              name: { kind: 'Name', value: 'PlaylistChunkInput' },
            },
          },
        },
      ],
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          {
            kind: 'Field',
            name: { kind: 'Name', value: 'playlistChunk' },
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
                  name: { kind: 'Name', value: 'playlistGeneratorId' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'items' },
                  selectionSet: {
                    kind: 'SelectionSet',
                    selections: [
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'itemEntryId' },
                      },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'itemId' },
                      },
                      { kind: 'Field', name: { kind: 'Name', value: 'index' } },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'served' },
                      },
                      { kind: 'Field', name: { kind: 'Name', value: 'title' } },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'metadataType' },
                      },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'durationMs' },
                      },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'playbackUrl' },
                      },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'thumbUri' },
                      },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'parentTitle' },
                      },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'subtitle' },
                      },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'primaryPerson' },
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
                              name: { kind: 'Name', value: 'metadataType' },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'currentIndex' },
                },
                { kind: 'Field', name: { kind: 'Name', value: 'totalCount' } },
                { kind: 'Field', name: { kind: 'Name', value: 'hasMore' } },
                { kind: 'Field', name: { kind: 'Name', value: 'shuffle' } },
                { kind: 'Field', name: { kind: 'Name', value: 'repeat' } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<PlaylistChunkQuery, PlaylistChunkQueryVariables>
export const PlaylistNextDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'mutation',
      name: { kind: 'Name', value: 'PlaylistNext' },
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
              name: { kind: 'Name', value: 'PlaylistNavigateInput' },
            },
          },
        },
      ],
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          {
            kind: 'Field',
            name: { kind: 'Name', value: 'playlistNext' },
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
                { kind: 'Field', name: { kind: 'Name', value: 'success' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'currentItem' },
                  selectionSet: {
                    kind: 'SelectionSet',
                    selections: [
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'itemEntryId' },
                      },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'itemId' },
                      },
                      { kind: 'Field', name: { kind: 'Name', value: 'index' } },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'served' },
                      },
                      { kind: 'Field', name: { kind: 'Name', value: 'title' } },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'metadataType' },
                      },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'durationMs' },
                      },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'thumbUri' },
                      },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'parentTitle' },
                      },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'subtitle' },
                      },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'primaryPerson' },
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
                              name: { kind: 'Name', value: 'metadataType' },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                { kind: 'Field', name: { kind: 'Name', value: 'shuffle' } },
                { kind: 'Field', name: { kind: 'Name', value: 'repeat' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'currentIndex' },
                },
                { kind: 'Field', name: { kind: 'Name', value: 'totalCount' } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  PlaylistNextMutation,
  PlaylistNextMutationVariables
>
export const PlaylistPreviousDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'mutation',
      name: { kind: 'Name', value: 'PlaylistPrevious' },
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
              name: { kind: 'Name', value: 'PlaylistNavigateInput' },
            },
          },
        },
      ],
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          {
            kind: 'Field',
            name: { kind: 'Name', value: 'playlistPrevious' },
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
                { kind: 'Field', name: { kind: 'Name', value: 'success' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'currentItem' },
                  selectionSet: {
                    kind: 'SelectionSet',
                    selections: [
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'itemEntryId' },
                      },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'itemId' },
                      },
                      { kind: 'Field', name: { kind: 'Name', value: 'index' } },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'served' },
                      },
                      { kind: 'Field', name: { kind: 'Name', value: 'title' } },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'metadataType' },
                      },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'durationMs' },
                      },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'thumbUri' },
                      },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'parentTitle' },
                      },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'subtitle' },
                      },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'primaryPerson' },
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
                              name: { kind: 'Name', value: 'metadataType' },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                { kind: 'Field', name: { kind: 'Name', value: 'shuffle' } },
                { kind: 'Field', name: { kind: 'Name', value: 'repeat' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'currentIndex' },
                },
                { kind: 'Field', name: { kind: 'Name', value: 'totalCount' } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  PlaylistPreviousMutation,
  PlaylistPreviousMutationVariables
>
export const PlaylistJumpDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'mutation',
      name: { kind: 'Name', value: 'PlaylistJump' },
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
              name: { kind: 'Name', value: 'PlaylistJumpInput' },
            },
          },
        },
      ],
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          {
            kind: 'Field',
            name: { kind: 'Name', value: 'playlistJump' },
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
                { kind: 'Field', name: { kind: 'Name', value: 'success' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'currentItem' },
                  selectionSet: {
                    kind: 'SelectionSet',
                    selections: [
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'itemEntryId' },
                      },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'itemId' },
                      },
                      { kind: 'Field', name: { kind: 'Name', value: 'index' } },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'served' },
                      },
                      { kind: 'Field', name: { kind: 'Name', value: 'title' } },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'metadataType' },
                      },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'durationMs' },
                      },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'playbackUrl' },
                      },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'thumbUri' },
                      },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'parentTitle' },
                      },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'subtitle' },
                      },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'primaryPerson' },
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
                              name: { kind: 'Name', value: 'metadataType' },
                            },
                          ],
                        },
                      },
                    ],
                  },
                },
                { kind: 'Field', name: { kind: 'Name', value: 'shuffle' } },
                { kind: 'Field', name: { kind: 'Name', value: 'repeat' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'currentIndex' },
                },
                { kind: 'Field', name: { kind: 'Name', value: 'totalCount' } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  PlaylistJumpMutation,
  PlaylistJumpMutationVariables
>
export const PlaylistSetShuffleDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'mutation',
      name: { kind: 'Name', value: 'PlaylistSetShuffle' },
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
              name: { kind: 'Name', value: 'PlaylistModeInput' },
            },
          },
        },
      ],
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          {
            kind: 'Field',
            name: { kind: 'Name', value: 'playlistSetShuffle' },
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
                { kind: 'Field', name: { kind: 'Name', value: 'success' } },
                { kind: 'Field', name: { kind: 'Name', value: 'shuffle' } },
                { kind: 'Field', name: { kind: 'Name', value: 'repeat' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'currentIndex' },
                },
                { kind: 'Field', name: { kind: 'Name', value: 'totalCount' } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  PlaylistSetShuffleMutation,
  PlaylistSetShuffleMutationVariables
>
export const PlaylistSetRepeatDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'mutation',
      name: { kind: 'Name', value: 'PlaylistSetRepeat' },
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
              name: { kind: 'Name', value: 'PlaylistModeInput' },
            },
          },
        },
      ],
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          {
            kind: 'Field',
            name: { kind: 'Name', value: 'playlistSetRepeat' },
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
                { kind: 'Field', name: { kind: 'Name', value: 'success' } },
                { kind: 'Field', name: { kind: 'Name', value: 'shuffle' } },
                { kind: 'Field', name: { kind: 'Name', value: 'repeat' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'currentIndex' },
                },
                { kind: 'Field', name: { kind: 'Name', value: 'totalCount' } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  PlaylistSetRepeatMutation,
  PlaylistSetRepeatMutationVariables
>
export const DecidePlaybackNavigationDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'mutation',
      name: { kind: 'Name', value: 'DecidePlaybackNavigation' },
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
              name: { kind: 'Name', value: 'PlaybackDecisionInput' },
            },
          },
        },
      ],
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          {
            kind: 'Field',
            name: { kind: 'Name', value: 'decidePlayback' },
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
                { kind: 'Field', name: { kind: 'Name', value: 'action' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'streamPlanJson' },
                },
                { kind: 'Field', name: { kind: 'Name', value: 'nextItemId' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'nextItemTitle' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'nextItemOriginalTitle' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'nextItemParentTitle' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'nextItemThumbUrl' },
                },
                { kind: 'Field', name: { kind: 'Name', value: 'playbackUrl' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'trickplayUrl' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'capabilityProfileVersion' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'capabilityVersionMismatch' },
                },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  DecidePlaybackNavigationMutation,
  DecidePlaybackNavigationMutationVariables
>
export const ResumePlaybackDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'mutation',
      name: { kind: 'Name', value: 'ResumePlayback' },
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
              name: { kind: 'Name', value: 'PlaybackResumeInput' },
            },
          },
        },
      ],
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          {
            kind: 'Field',
            name: { kind: 'Name', value: 'resumePlayback' },
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
                  name: { kind: 'Name', value: 'playbackSessionId' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'currentItemId' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'playlistGeneratorId' },
                },
                { kind: 'Field', name: { kind: 'Name', value: 'playheadMs' } },
                { kind: 'Field', name: { kind: 'Name', value: 'state' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'capabilityProfileVersion' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'capabilityVersionMismatch' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'streamPlanJson' },
                },
                { kind: 'Field', name: { kind: 'Name', value: 'playbackUrl' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'trickplayUrl' },
                },
                { kind: 'Field', name: { kind: 'Name', value: 'durationMs' } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  ResumePlaybackMutation,
  ResumePlaybackMutationVariables
>
export const StartPlaybackDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'mutation',
      name: { kind: 'Name', value: 'StartPlayback' },
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
              name: { kind: 'Name', value: 'PlaybackStartInput' },
            },
          },
        },
      ],
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          {
            kind: 'Field',
            name: { kind: 'Name', value: 'startPlayback' },
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
                  name: { kind: 'Name', value: 'playbackSessionId' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'playlistGeneratorId' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'capabilityProfileVersion' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'streamPlanJson' },
                },
                { kind: 'Field', name: { kind: 'Name', value: 'playbackUrl' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'trickplayUrl' },
                },
                { kind: 'Field', name: { kind: 'Name', value: 'durationMs' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'capabilityVersionMismatch' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'playlistIndex' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'playlistTotalCount' },
                },
                { kind: 'Field', name: { kind: 'Name', value: 'shuffle' } },
                { kind: 'Field', name: { kind: 'Name', value: 'repeat' } },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'currentItemId' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'currentItemMetadataType' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'currentItemTitle' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'currentItemOriginalTitle' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'currentItemParentTitle' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'currentItemThumbUrl' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'currentItemParentThumbUrl' },
                },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  StartPlaybackMutation,
  StartPlaybackMutationVariables
>
export const StopPlaybackDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'mutation',
      name: { kind: 'Name', value: 'StopPlayback' },
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
              name: { kind: 'Name', value: 'PlaybackStopInput' },
            },
          },
        },
      ],
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          {
            kind: 'Field',
            name: { kind: 'Name', value: 'stopPlayback' },
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
                { kind: 'Field', name: { kind: 'Name', value: 'success' } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  StopPlaybackMutation,
  StopPlaybackMutationVariables
>
export const HubConfigurationDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'query',
      name: { kind: 'Name', value: 'HubConfiguration' },
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
              name: { kind: 'Name', value: 'HubConfigurationScopeInput' },
            },
          },
        },
      ],
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          {
            kind: 'Field',
            name: { kind: 'Name', value: 'hubConfiguration' },
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
                  name: { kind: 'Name', value: 'enabledHubTypes' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'disabledHubTypes' },
                },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  HubConfigurationQuery,
  HubConfigurationQueryVariables
>
export const UpdateHubConfigurationDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'mutation',
      name: { kind: 'Name', value: 'UpdateHubConfiguration' },
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
              name: { kind: 'Name', value: 'UpdateHubConfigurationInput' },
            },
          },
        },
      ],
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          {
            kind: 'Field',
            name: { kind: 'Name', value: 'updateHubConfiguration' },
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
                  name: { kind: 'Name', value: 'enabledHubTypes' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'disabledHubTypes' },
                },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<
  UpdateHubConfigurationMutation,
  UpdateHubConfigurationMutationVariables
>
export const AdminDetailFieldConfigurationDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'query',
      name: { kind: 'Name', value: 'AdminDetailFieldConfiguration' },
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
              name: {
                kind: 'Name',
                value: 'DetailFieldConfigurationScopeInput',
              },
            },
          },
        },
      ],
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          {
            kind: 'Field',
            name: { kind: 'Name', value: 'adminDetailFieldConfiguration' },
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
                  name: { kind: 'Name', value: 'metadataType' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'librarySectionId' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'enabledFieldTypes' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'disabledFieldTypes' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'disabledCustomFieldKeys' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'fieldGroups' },
                  selectionSet: {
                    kind: 'SelectionSet',
                    selections: [
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'groupKey' },
                      },
                      { kind: 'Field', name: { kind: 'Name', value: 'label' } },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'layoutType' },
                      },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'sortOrder' },
                      },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'isCollapsible' },
                      },
                    ],
                  },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'fieldGroupAssignments' },
                  selectionSet: {
                    kind: 'SelectionSet',
                    selections: [
                      { kind: 'Field', name: { kind: 'Name', value: 'key' } },
                      { kind: 'Field', name: { kind: 'Name', value: 'value' } },
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
  AdminDetailFieldConfigurationQuery,
  AdminDetailFieldConfigurationQueryVariables
>
export const UpdateAdminDetailFieldConfigurationDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'mutation',
      name: { kind: 'Name', value: 'UpdateAdminDetailFieldConfiguration' },
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
              name: {
                kind: 'Name',
                value: 'UpdateAdminDetailFieldConfigurationInput',
              },
            },
          },
        },
      ],
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          {
            kind: 'Field',
            name: {
              kind: 'Name',
              value: 'updateAdminDetailFieldConfiguration',
            },
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
                  name: { kind: 'Name', value: 'metadataType' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'librarySectionId' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'enabledFieldTypes' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'disabledFieldTypes' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'disabledCustomFieldKeys' },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'fieldGroups' },
                  selectionSet: {
                    kind: 'SelectionSet',
                    selections: [
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'groupKey' },
                      },
                      { kind: 'Field', name: { kind: 'Name', value: 'label' } },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'layoutType' },
                      },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'sortOrder' },
                      },
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'isCollapsible' },
                      },
                    ],
                  },
                },
                {
                  kind: 'Field',
                  name: { kind: 'Name', value: 'fieldGroupAssignments' },
                  selectionSet: {
                    kind: 'SelectionSet',
                    selections: [
                      { kind: 'Field', name: { kind: 'Name', value: 'key' } },
                      { kind: 'Field', name: { kind: 'Name', value: 'value' } },
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
  UpdateAdminDetailFieldConfigurationMutation,
  UpdateAdminDetailFieldConfigurationMutationVariables
>
export const AdminLibrarySectionsListDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'query',
      name: { kind: 'Name', value: 'AdminLibrarySectionsList' },
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
                value: { kind: 'IntValue', value: '50' },
              },
              {
                kind: 'Argument',
                name: { kind: 'Name', value: 'order' },
                value: {
                  kind: 'ListValue',
                  values: [
                    {
                      kind: 'ObjectValue',
                      fields: [
                        {
                          kind: 'ObjectField',
                          name: { kind: 'Name', value: 'name' },
                          value: { kind: 'EnumValue', value: 'ASC' },
                        },
                      ],
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
                  name: { kind: 'Name', value: 'edges' },
                  selectionSet: {
                    kind: 'SelectionSet',
                    selections: [
                      {
                        kind: 'Field',
                        name: { kind: 'Name', value: 'node' },
                        selectionSet: {
                          kind: 'SelectionSet',
                          selections: [
                            {
                              kind: 'Field',
                              name: { kind: 'Name', value: 'id' },
                            },
                            {
                              kind: 'Field',
                              name: { kind: 'Name', value: 'name' },
                            },
                            {
                              kind: 'Field',
                              name: { kind: 'Name', value: 'type' },
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
      },
    },
  ],
} as unknown as DocumentNode<
  AdminLibrarySectionsListQuery,
  AdminLibrarySectionsListQueryVariables
>
