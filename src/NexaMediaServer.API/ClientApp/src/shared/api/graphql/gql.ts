/* eslint-disable */
import * as types from './graphql'
import type { TypedDocumentNode as DocumentNode } from '@graphql-typed-document-node/core'

/**
 * Map of all GraphQL operations in the project.
 *
 * This map has several performance disadvantages:
 * 1. It is not tree-shakeable, so it will include all operations in the project.
 * 2. It is not minifiable, so the string of a GraphQL query will be multiple times inside the bundle.
 * 3. It does not support dead code elimination, so it will add unused operations.
 *
 * Therefore it is highly recommended to use the babel or swc plugin for production.
 * Learn more about it here: https://the-guild.dev/graphql/codegen/plugins/presets/preset-client#reducing-bundle-size
 */
type Documents = {
  '\n  query LibrarySectionsList(\n    $first: Int\n    $after: String\n    $last: Int\n    $before: String\n  ) {\n    librarySections(\n      first: $first\n      after: $after\n      last: $last\n      before: $before\n    ) {\n      nodes {\n        id\n        name\n        type\n      }\n      pageInfo {\n        hasNextPage\n        hasPreviousPage\n        startCursor\n        endCursor\n      }\n    }\n  }\n': typeof types.LibrarySectionsListDocument
  '\n  mutation RemoveLibrarySection($librarySectionId: ID!) {\n    removeLibrarySection(input: { librarySectionId: $librarySectionId }) {\n      success\n      error\n    }\n  }\n': typeof types.RemoveLibrarySectionDocument
  '\n  mutation RefreshLibraryMetadata($librarySectionId: ID!) {\n    refreshLibraryMetadata(input: { librarySectionId: $librarySectionId }) {\n      success\n      error\n    }\n  }\n': typeof types.RefreshLibraryMetadataDocument
  '\n  mutation RefreshItemMetadata(\n    $itemId: ID!\n    $includeChildren: Boolean! = true\n  ) {\n    refreshItemMetadata(\n      input: { itemId: $itemId, includeChildren: $includeChildren }\n    ) {\n      success\n      error\n    }\n  }\n': typeof types.RefreshItemMetadataDocument
  '\n  mutation PromoteItem($itemId: ID!, $promotedUntil: DateTime) {\n    promoteItem(input: { itemId: $itemId, promotedUntil: $promotedUntil }) {\n      success\n      error\n    }\n  }\n': typeof types.PromoteItemDocument
  '\n  mutation UnpromoteItem($itemId: ID!) {\n    unpromoteItem(input: { itemId: $itemId }) {\n      success\n      error\n    }\n  }\n': typeof types.UnpromoteItemDocument
  '\n  query Search($query: String!, $pivot: SearchPivot, $limit: Int) {\n    search(query: $query, pivot: $pivot, limit: $limit) {\n      id\n      title\n      metadataType\n      score\n      year\n      thumbUri\n      librarySectionId\n    }\n  }\n': typeof types.SearchDocument
  '\n  subscription OnMetadataItemUpdated {\n    onMetadataItemUpdated {\n      id\n      title\n      originalTitle\n      year\n      metadataType\n      thumbUri\n    }\n  }\n': typeof types.OnMetadataItemUpdatedDocument
  '\n  subscription OnJobNotification {\n    onJobNotification {\n      id\n      type\n      librarySectionId\n      librarySectionName\n      description\n      progressPercentage\n      completedItems\n      totalItems\n      isActive\n      timestamp\n    }\n  }\n': typeof types.OnJobNotificationDocument
  '\n  query ServerInfo {\n    serverInfo {\n      versionString\n      isDevelopment\n    }\n  }\n': typeof types.ServerInfoDocument
  '\n  mutation AddLibrarySection($input: AddLibrarySectionInput!) {\n    addLibrarySection(input: $input) {\n      librarySection {\n        id\n        name\n        type\n      }\n    }\n  }\n': typeof types.AddLibrarySectionDocument
  '\n  query AvailableMetadataAgents($libraryType: LibraryType!) {\n    availableMetadataAgents(libraryType: $libraryType) {\n      name\n      displayName\n      description\n      category\n      defaultOrder\n      supportedLibraryTypes\n    }\n  }\n': typeof types.AvailableMetadataAgentsDocument
  '\n  query FileSystemRoots {\n    fileSystemRoots {\n      id\n      label\n      path\n      kind\n      isReadOnly\n    }\n  }\n': typeof types.FileSystemRootsDocument
  '\n  query BrowseDirectory($path: String!) {\n    browseDirectory(path: $path) {\n      currentPath\n      parentPath\n      entries {\n        name\n        path\n        isDirectory\n        isFile\n        isSymbolicLink\n        isSelectable\n      }\n    }\n  }\n': typeof types.BrowseDirectoryDocument
  '\n  query LibrarySection($contentSourceId: ID!) {\n    librarySection(id: $contentSourceId) {\n      id\n      name\n      type\n    }\n  }\n': typeof types.LibrarySectionDocument
  '\n  query LibrarySectionChildren(\n    $contentSourceId: ID!\n    $metadataType: MetadataType!\n    $skip: Int\n    $take: Int\n  ) {\n    librarySection(id: $contentSourceId) {\n      id\n      children(\n        metadataType: $metadataType\n        skip: $skip\n        take: $take\n        order: { title: ASC }\n      ) {\n        items {\n          id\n          title\n          year\n          thumbUri\n          metadataType\n          length\n          viewOffset\n        }\n        pageInfo {\n          hasNextPage\n          hasPreviousPage\n        }\n        totalCount\n      }\n    }\n  }\n': typeof types.LibrarySectionChildrenDocument
  '\n  query LibrarySectionLetterIndex(\n    $contentSourceId: ID!\n    $metadataType: MetadataType!\n  ) {\n    librarySection(id: $contentSourceId) {\n      id\n      letterIndex(metadataType: $metadataType) {\n        letter\n        count\n        firstItemOffset\n      }\n    }\n  }\n': typeof types.LibrarySectionLetterIndexDocument
  '\n  query HomeHubDefinitions {\n    homeHubDefinitions {\n      key\n      type\n      title\n      metadataType\n      widget\n      filterValue\n      librarySectionId\n      contextId\n    }\n  }\n': typeof types.HomeHubDefinitionsDocument
  '\n  query LibraryDiscoverHubDefinitions($librarySectionId: ID!) {\n    libraryDiscoverHubDefinitions(librarySectionId: $librarySectionId) {\n      key\n      type\n      title\n      metadataType\n      widget\n      filterValue\n      librarySectionId\n      contextId\n    }\n  }\n': typeof types.LibraryDiscoverHubDefinitionsDocument
  '\n  query ItemDetailHubDefinitions($itemId: ID!) {\n    itemDetailHubDefinitions(itemId: $itemId) {\n      key\n      type\n      title\n      metadataType\n      widget\n      filterValue\n      librarySectionId\n      contextId\n    }\n  }\n': typeof types.ItemDetailHubDefinitionsDocument
  '\n  query HubItems($input: GetHubItemsInput!) {\n    hubItems(input: $input) {\n      id\n      librarySectionId\n      metadataType\n      title\n      year\n      length\n      viewOffset\n      thumbUri\n      thumbHash\n      artUri\n      artHash\n      logoUri\n      logoHash\n      tagline\n      contentRating\n      summary\n      context\n    }\n  }\n': typeof types.HubItemsDocument
  '\n  query HubPeople($hubType: HubType!, $metadataItemId: ID!) {\n    hubPeople(hubType: $hubType, metadataItemId: $metadataItemId) {\n      id\n      metadataType\n      title\n      thumbUri\n      thumbHash\n      context\n    }\n  }\n': typeof types.HubPeopleDocument
  '\n  query Media($id: ID!) {\n    metadataItem(id: $id) {\n      id\n      librarySectionId\n      title\n      originalTitle\n      thumbUri\n      thumbHash\n      metadataType\n      year\n      length\n      genres\n      tags\n      contentRating\n      viewOffset\n      isPromoted\n    }\n  }\n': typeof types.MediaDocument
  '\n  query ContentSource($contentSourceId: ID!) {\n    librarySection(id: $contentSourceId) {\n      id\n      name\n    }\n  }\n': typeof types.ContentSourceDocument
  '\n  mutation PlaybackHeartbeat($input: PlaybackHeartbeatInput!) {\n    playbackHeartbeat(input: $input) {\n      playbackSessionId\n      capabilityProfileVersion\n      capabilityVersionMismatch\n    }\n  }\n': typeof types.PlaybackHeartbeatDocument
  '\n  mutation DecidePlayback($input: PlaybackDecisionInput!) {\n    decidePlayback(input: $input) {\n      action\n      streamPlanJson\n      nextItemId\n      playbackUrl\n      trickplayUrl\n      capabilityProfileVersion\n      capabilityVersionMismatch\n    }\n  }\n': typeof types.DecidePlaybackDocument
  '\n  mutation PlaybackSeek($input: PlaybackSeekInput!) {\n    playbackSeek(input: $input) {\n      keyframeMs\n      gopDurationMs\n      hasGopIndex\n      originalTargetMs\n    }\n  }\n': typeof types.PlaybackSeekDocument
  '\n  mutation StartPlayback($input: PlaybackStartInput!) {\n    startPlayback(input: $input) {\n      playbackSessionId\n      playlistGeneratorId\n      capabilityProfileVersion\n      streamPlanJson\n      playbackUrl\n      trickplayUrl\n      durationMs\n      capabilityVersionMismatch\n    }\n  }\n': typeof types.StartPlaybackDocument
}
const documents: Documents = {
  '\n  query LibrarySectionsList(\n    $first: Int\n    $after: String\n    $last: Int\n    $before: String\n  ) {\n    librarySections(\n      first: $first\n      after: $after\n      last: $last\n      before: $before\n    ) {\n      nodes {\n        id\n        name\n        type\n      }\n      pageInfo {\n        hasNextPage\n        hasPreviousPage\n        startCursor\n        endCursor\n      }\n    }\n  }\n':
    types.LibrarySectionsListDocument,
  '\n  mutation RemoveLibrarySection($librarySectionId: ID!) {\n    removeLibrarySection(input: { librarySectionId: $librarySectionId }) {\n      success\n      error\n    }\n  }\n':
    types.RemoveLibrarySectionDocument,
  '\n  mutation RefreshLibraryMetadata($librarySectionId: ID!) {\n    refreshLibraryMetadata(input: { librarySectionId: $librarySectionId }) {\n      success\n      error\n    }\n  }\n':
    types.RefreshLibraryMetadataDocument,
  '\n  mutation RefreshItemMetadata(\n    $itemId: ID!\n    $includeChildren: Boolean! = true\n  ) {\n    refreshItemMetadata(\n      input: { itemId: $itemId, includeChildren: $includeChildren }\n    ) {\n      success\n      error\n    }\n  }\n':
    types.RefreshItemMetadataDocument,
  '\n  mutation PromoteItem($itemId: ID!, $promotedUntil: DateTime) {\n    promoteItem(input: { itemId: $itemId, promotedUntil: $promotedUntil }) {\n      success\n      error\n    }\n  }\n':
    types.PromoteItemDocument,
  '\n  mutation UnpromoteItem($itemId: ID!) {\n    unpromoteItem(input: { itemId: $itemId }) {\n      success\n      error\n    }\n  }\n':
    types.UnpromoteItemDocument,
  '\n  query Search($query: String!, $pivot: SearchPivot, $limit: Int) {\n    search(query: $query, pivot: $pivot, limit: $limit) {\n      id\n      title\n      metadataType\n      score\n      year\n      thumbUri\n      librarySectionId\n    }\n  }\n':
    types.SearchDocument,
  '\n  subscription OnMetadataItemUpdated {\n    onMetadataItemUpdated {\n      id\n      title\n      originalTitle\n      year\n      metadataType\n      thumbUri\n    }\n  }\n':
    types.OnMetadataItemUpdatedDocument,
  '\n  subscription OnJobNotification {\n    onJobNotification {\n      id\n      type\n      librarySectionId\n      librarySectionName\n      description\n      progressPercentage\n      completedItems\n      totalItems\n      isActive\n      timestamp\n    }\n  }\n':
    types.OnJobNotificationDocument,
  '\n  query ServerInfo {\n    serverInfo {\n      versionString\n      isDevelopment\n    }\n  }\n':
    types.ServerInfoDocument,
  '\n  mutation AddLibrarySection($input: AddLibrarySectionInput!) {\n    addLibrarySection(input: $input) {\n      librarySection {\n        id\n        name\n        type\n      }\n    }\n  }\n':
    types.AddLibrarySectionDocument,
  '\n  query AvailableMetadataAgents($libraryType: LibraryType!) {\n    availableMetadataAgents(libraryType: $libraryType) {\n      name\n      displayName\n      description\n      category\n      defaultOrder\n      supportedLibraryTypes\n    }\n  }\n':
    types.AvailableMetadataAgentsDocument,
  '\n  query FileSystemRoots {\n    fileSystemRoots {\n      id\n      label\n      path\n      kind\n      isReadOnly\n    }\n  }\n':
    types.FileSystemRootsDocument,
  '\n  query BrowseDirectory($path: String!) {\n    browseDirectory(path: $path) {\n      currentPath\n      parentPath\n      entries {\n        name\n        path\n        isDirectory\n        isFile\n        isSymbolicLink\n        isSelectable\n      }\n    }\n  }\n':
    types.BrowseDirectoryDocument,
  '\n  query LibrarySection($contentSourceId: ID!) {\n    librarySection(id: $contentSourceId) {\n      id\n      name\n      type\n    }\n  }\n':
    types.LibrarySectionDocument,
  '\n  query LibrarySectionChildren(\n    $contentSourceId: ID!\n    $metadataType: MetadataType!\n    $skip: Int\n    $take: Int\n  ) {\n    librarySection(id: $contentSourceId) {\n      id\n      children(\n        metadataType: $metadataType\n        skip: $skip\n        take: $take\n        order: { title: ASC }\n      ) {\n        items {\n          id\n          title\n          year\n          thumbUri\n          metadataType\n          length\n          viewOffset\n        }\n        pageInfo {\n          hasNextPage\n          hasPreviousPage\n        }\n        totalCount\n      }\n    }\n  }\n':
    types.LibrarySectionChildrenDocument,
  '\n  query LibrarySectionLetterIndex(\n    $contentSourceId: ID!\n    $metadataType: MetadataType!\n  ) {\n    librarySection(id: $contentSourceId) {\n      id\n      letterIndex(metadataType: $metadataType) {\n        letter\n        count\n        firstItemOffset\n      }\n    }\n  }\n':
    types.LibrarySectionLetterIndexDocument,
  '\n  query HomeHubDefinitions {\n    homeHubDefinitions {\n      key\n      type\n      title\n      metadataType\n      widget\n      filterValue\n      librarySectionId\n      contextId\n    }\n  }\n':
    types.HomeHubDefinitionsDocument,
  '\n  query LibraryDiscoverHubDefinitions($librarySectionId: ID!) {\n    libraryDiscoverHubDefinitions(librarySectionId: $librarySectionId) {\n      key\n      type\n      title\n      metadataType\n      widget\n      filterValue\n      librarySectionId\n      contextId\n    }\n  }\n':
    types.LibraryDiscoverHubDefinitionsDocument,
  '\n  query ItemDetailHubDefinitions($itemId: ID!) {\n    itemDetailHubDefinitions(itemId: $itemId) {\n      key\n      type\n      title\n      metadataType\n      widget\n      filterValue\n      librarySectionId\n      contextId\n    }\n  }\n':
    types.ItemDetailHubDefinitionsDocument,
  '\n  query HubItems($input: GetHubItemsInput!) {\n    hubItems(input: $input) {\n      id\n      librarySectionId\n      metadataType\n      title\n      year\n      length\n      viewOffset\n      thumbUri\n      thumbHash\n      artUri\n      artHash\n      logoUri\n      logoHash\n      tagline\n      contentRating\n      summary\n      context\n    }\n  }\n':
    types.HubItemsDocument,
  '\n  query HubPeople($hubType: HubType!, $metadataItemId: ID!) {\n    hubPeople(hubType: $hubType, metadataItemId: $metadataItemId) {\n      id\n      metadataType\n      title\n      thumbUri\n      thumbHash\n      context\n    }\n  }\n':
    types.HubPeopleDocument,
  '\n  query Media($id: ID!) {\n    metadataItem(id: $id) {\n      id\n      librarySectionId\n      title\n      originalTitle\n      thumbUri\n      thumbHash\n      metadataType\n      year\n      length\n      genres\n      tags\n      contentRating\n      viewOffset\n      isPromoted\n    }\n  }\n':
    types.MediaDocument,
  '\n  query ContentSource($contentSourceId: ID!) {\n    librarySection(id: $contentSourceId) {\n      id\n      name\n    }\n  }\n':
    types.ContentSourceDocument,
  '\n  mutation PlaybackHeartbeat($input: PlaybackHeartbeatInput!) {\n    playbackHeartbeat(input: $input) {\n      playbackSessionId\n      capabilityProfileVersion\n      capabilityVersionMismatch\n    }\n  }\n':
    types.PlaybackHeartbeatDocument,
  '\n  mutation DecidePlayback($input: PlaybackDecisionInput!) {\n    decidePlayback(input: $input) {\n      action\n      streamPlanJson\n      nextItemId\n      playbackUrl\n      trickplayUrl\n      capabilityProfileVersion\n      capabilityVersionMismatch\n    }\n  }\n':
    types.DecidePlaybackDocument,
  '\n  mutation PlaybackSeek($input: PlaybackSeekInput!) {\n    playbackSeek(input: $input) {\n      keyframeMs\n      gopDurationMs\n      hasGopIndex\n      originalTargetMs\n    }\n  }\n':
    types.PlaybackSeekDocument,
  '\n  mutation StartPlayback($input: PlaybackStartInput!) {\n    startPlayback(input: $input) {\n      playbackSessionId\n      playlistGeneratorId\n      capabilityProfileVersion\n      streamPlanJson\n      playbackUrl\n      trickplayUrl\n      durationMs\n      capabilityVersionMismatch\n    }\n  }\n':
    types.StartPlaybackDocument,
}

/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 *
 *
 * @example
 * ```ts
 * const query = graphql(`query GetUser($id: ID!) { user(id: $id) { name } }`);
 * ```
 *
 * The query argument is unknown!
 * Please regenerate the types.
 */
export function graphql(source: string): unknown

/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  query LibrarySectionsList(\n    $first: Int\n    $after: String\n    $last: Int\n    $before: String\n  ) {\n    librarySections(\n      first: $first\n      after: $after\n      last: $last\n      before: $before\n    ) {\n      nodes {\n        id\n        name\n        type\n      }\n      pageInfo {\n        hasNextPage\n        hasPreviousPage\n        startCursor\n        endCursor\n      }\n    }\n  }\n',
): (typeof documents)['\n  query LibrarySectionsList(\n    $first: Int\n    $after: String\n    $last: Int\n    $before: String\n  ) {\n    librarySections(\n      first: $first\n      after: $after\n      last: $last\n      before: $before\n    ) {\n      nodes {\n        id\n        name\n        type\n      }\n      pageInfo {\n        hasNextPage\n        hasPreviousPage\n        startCursor\n        endCursor\n      }\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  mutation RemoveLibrarySection($librarySectionId: ID!) {\n    removeLibrarySection(input: { librarySectionId: $librarySectionId }) {\n      success\n      error\n    }\n  }\n',
): (typeof documents)['\n  mutation RemoveLibrarySection($librarySectionId: ID!) {\n    removeLibrarySection(input: { librarySectionId: $librarySectionId }) {\n      success\n      error\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  mutation RefreshLibraryMetadata($librarySectionId: ID!) {\n    refreshLibraryMetadata(input: { librarySectionId: $librarySectionId }) {\n      success\n      error\n    }\n  }\n',
): (typeof documents)['\n  mutation RefreshLibraryMetadata($librarySectionId: ID!) {\n    refreshLibraryMetadata(input: { librarySectionId: $librarySectionId }) {\n      success\n      error\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  mutation RefreshItemMetadata(\n    $itemId: ID!\n    $includeChildren: Boolean! = true\n  ) {\n    refreshItemMetadata(\n      input: { itemId: $itemId, includeChildren: $includeChildren }\n    ) {\n      success\n      error\n    }\n  }\n',
): (typeof documents)['\n  mutation RefreshItemMetadata(\n    $itemId: ID!\n    $includeChildren: Boolean! = true\n  ) {\n    refreshItemMetadata(\n      input: { itemId: $itemId, includeChildren: $includeChildren }\n    ) {\n      success\n      error\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  mutation PromoteItem($itemId: ID!, $promotedUntil: DateTime) {\n    promoteItem(input: { itemId: $itemId, promotedUntil: $promotedUntil }) {\n      success\n      error\n    }\n  }\n',
): (typeof documents)['\n  mutation PromoteItem($itemId: ID!, $promotedUntil: DateTime) {\n    promoteItem(input: { itemId: $itemId, promotedUntil: $promotedUntil }) {\n      success\n      error\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  mutation UnpromoteItem($itemId: ID!) {\n    unpromoteItem(input: { itemId: $itemId }) {\n      success\n      error\n    }\n  }\n',
): (typeof documents)['\n  mutation UnpromoteItem($itemId: ID!) {\n    unpromoteItem(input: { itemId: $itemId }) {\n      success\n      error\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  query Search($query: String!, $pivot: SearchPivot, $limit: Int) {\n    search(query: $query, pivot: $pivot, limit: $limit) {\n      id\n      title\n      metadataType\n      score\n      year\n      thumbUri\n      librarySectionId\n    }\n  }\n',
): (typeof documents)['\n  query Search($query: String!, $pivot: SearchPivot, $limit: Int) {\n    search(query: $query, pivot: $pivot, limit: $limit) {\n      id\n      title\n      metadataType\n      score\n      year\n      thumbUri\n      librarySectionId\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  subscription OnMetadataItemUpdated {\n    onMetadataItemUpdated {\n      id\n      title\n      originalTitle\n      year\n      metadataType\n      thumbUri\n    }\n  }\n',
): (typeof documents)['\n  subscription OnMetadataItemUpdated {\n    onMetadataItemUpdated {\n      id\n      title\n      originalTitle\n      year\n      metadataType\n      thumbUri\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  subscription OnJobNotification {\n    onJobNotification {\n      id\n      type\n      librarySectionId\n      librarySectionName\n      description\n      progressPercentage\n      completedItems\n      totalItems\n      isActive\n      timestamp\n    }\n  }\n',
): (typeof documents)['\n  subscription OnJobNotification {\n    onJobNotification {\n      id\n      type\n      librarySectionId\n      librarySectionName\n      description\n      progressPercentage\n      completedItems\n      totalItems\n      isActive\n      timestamp\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  query ServerInfo {\n    serverInfo {\n      versionString\n      isDevelopment\n    }\n  }\n',
): (typeof documents)['\n  query ServerInfo {\n    serverInfo {\n      versionString\n      isDevelopment\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  mutation AddLibrarySection($input: AddLibrarySectionInput!) {\n    addLibrarySection(input: $input) {\n      librarySection {\n        id\n        name\n        type\n      }\n    }\n  }\n',
): (typeof documents)['\n  mutation AddLibrarySection($input: AddLibrarySectionInput!) {\n    addLibrarySection(input: $input) {\n      librarySection {\n        id\n        name\n        type\n      }\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  query AvailableMetadataAgents($libraryType: LibraryType!) {\n    availableMetadataAgents(libraryType: $libraryType) {\n      name\n      displayName\n      description\n      category\n      defaultOrder\n      supportedLibraryTypes\n    }\n  }\n',
): (typeof documents)['\n  query AvailableMetadataAgents($libraryType: LibraryType!) {\n    availableMetadataAgents(libraryType: $libraryType) {\n      name\n      displayName\n      description\n      category\n      defaultOrder\n      supportedLibraryTypes\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  query FileSystemRoots {\n    fileSystemRoots {\n      id\n      label\n      path\n      kind\n      isReadOnly\n    }\n  }\n',
): (typeof documents)['\n  query FileSystemRoots {\n    fileSystemRoots {\n      id\n      label\n      path\n      kind\n      isReadOnly\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  query BrowseDirectory($path: String!) {\n    browseDirectory(path: $path) {\n      currentPath\n      parentPath\n      entries {\n        name\n        path\n        isDirectory\n        isFile\n        isSymbolicLink\n        isSelectable\n      }\n    }\n  }\n',
): (typeof documents)['\n  query BrowseDirectory($path: String!) {\n    browseDirectory(path: $path) {\n      currentPath\n      parentPath\n      entries {\n        name\n        path\n        isDirectory\n        isFile\n        isSymbolicLink\n        isSelectable\n      }\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  query LibrarySection($contentSourceId: ID!) {\n    librarySection(id: $contentSourceId) {\n      id\n      name\n      type\n    }\n  }\n',
): (typeof documents)['\n  query LibrarySection($contentSourceId: ID!) {\n    librarySection(id: $contentSourceId) {\n      id\n      name\n      type\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  query LibrarySectionChildren(\n    $contentSourceId: ID!\n    $metadataType: MetadataType!\n    $skip: Int\n    $take: Int\n  ) {\n    librarySection(id: $contentSourceId) {\n      id\n      children(\n        metadataType: $metadataType\n        skip: $skip\n        take: $take\n        order: { title: ASC }\n      ) {\n        items {\n          id\n          title\n          year\n          thumbUri\n          metadataType\n          length\n          viewOffset\n        }\n        pageInfo {\n          hasNextPage\n          hasPreviousPage\n        }\n        totalCount\n      }\n    }\n  }\n',
): (typeof documents)['\n  query LibrarySectionChildren(\n    $contentSourceId: ID!\n    $metadataType: MetadataType!\n    $skip: Int\n    $take: Int\n  ) {\n    librarySection(id: $contentSourceId) {\n      id\n      children(\n        metadataType: $metadataType\n        skip: $skip\n        take: $take\n        order: { title: ASC }\n      ) {\n        items {\n          id\n          title\n          year\n          thumbUri\n          metadataType\n          length\n          viewOffset\n        }\n        pageInfo {\n          hasNextPage\n          hasPreviousPage\n        }\n        totalCount\n      }\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  query LibrarySectionLetterIndex(\n    $contentSourceId: ID!\n    $metadataType: MetadataType!\n  ) {\n    librarySection(id: $contentSourceId) {\n      id\n      letterIndex(metadataType: $metadataType) {\n        letter\n        count\n        firstItemOffset\n      }\n    }\n  }\n',
): (typeof documents)['\n  query LibrarySectionLetterIndex(\n    $contentSourceId: ID!\n    $metadataType: MetadataType!\n  ) {\n    librarySection(id: $contentSourceId) {\n      id\n      letterIndex(metadataType: $metadataType) {\n        letter\n        count\n        firstItemOffset\n      }\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  query HomeHubDefinitions {\n    homeHubDefinitions {\n      key\n      type\n      title\n      metadataType\n      widget\n      filterValue\n      librarySectionId\n      contextId\n    }\n  }\n',
): (typeof documents)['\n  query HomeHubDefinitions {\n    homeHubDefinitions {\n      key\n      type\n      title\n      metadataType\n      widget\n      filterValue\n      librarySectionId\n      contextId\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  query LibraryDiscoverHubDefinitions($librarySectionId: ID!) {\n    libraryDiscoverHubDefinitions(librarySectionId: $librarySectionId) {\n      key\n      type\n      title\n      metadataType\n      widget\n      filterValue\n      librarySectionId\n      contextId\n    }\n  }\n',
): (typeof documents)['\n  query LibraryDiscoverHubDefinitions($librarySectionId: ID!) {\n    libraryDiscoverHubDefinitions(librarySectionId: $librarySectionId) {\n      key\n      type\n      title\n      metadataType\n      widget\n      filterValue\n      librarySectionId\n      contextId\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  query ItemDetailHubDefinitions($itemId: ID!) {\n    itemDetailHubDefinitions(itemId: $itemId) {\n      key\n      type\n      title\n      metadataType\n      widget\n      filterValue\n      librarySectionId\n      contextId\n    }\n  }\n',
): (typeof documents)['\n  query ItemDetailHubDefinitions($itemId: ID!) {\n    itemDetailHubDefinitions(itemId: $itemId) {\n      key\n      type\n      title\n      metadataType\n      widget\n      filterValue\n      librarySectionId\n      contextId\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  query HubItems($input: GetHubItemsInput!) {\n    hubItems(input: $input) {\n      id\n      librarySectionId\n      metadataType\n      title\n      year\n      length\n      viewOffset\n      thumbUri\n      thumbHash\n      artUri\n      artHash\n      logoUri\n      logoHash\n      tagline\n      contentRating\n      summary\n      context\n    }\n  }\n',
): (typeof documents)['\n  query HubItems($input: GetHubItemsInput!) {\n    hubItems(input: $input) {\n      id\n      librarySectionId\n      metadataType\n      title\n      year\n      length\n      viewOffset\n      thumbUri\n      thumbHash\n      artUri\n      artHash\n      logoUri\n      logoHash\n      tagline\n      contentRating\n      summary\n      context\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  query HubPeople($hubType: HubType!, $metadataItemId: ID!) {\n    hubPeople(hubType: $hubType, metadataItemId: $metadataItemId) {\n      id\n      metadataType\n      title\n      thumbUri\n      thumbHash\n      context\n    }\n  }\n',
): (typeof documents)['\n  query HubPeople($hubType: HubType!, $metadataItemId: ID!) {\n    hubPeople(hubType: $hubType, metadataItemId: $metadataItemId) {\n      id\n      metadataType\n      title\n      thumbUri\n      thumbHash\n      context\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  query Media($id: ID!) {\n    metadataItem(id: $id) {\n      id\n      librarySectionId\n      title\n      originalTitle\n      thumbUri\n      thumbHash\n      metadataType\n      year\n      length\n      genres\n      tags\n      contentRating\n      viewOffset\n      isPromoted\n    }\n  }\n',
): (typeof documents)['\n  query Media($id: ID!) {\n    metadataItem(id: $id) {\n      id\n      librarySectionId\n      title\n      originalTitle\n      thumbUri\n      thumbHash\n      metadataType\n      year\n      length\n      genres\n      tags\n      contentRating\n      viewOffset\n      isPromoted\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  query ContentSource($contentSourceId: ID!) {\n    librarySection(id: $contentSourceId) {\n      id\n      name\n    }\n  }\n',
): (typeof documents)['\n  query ContentSource($contentSourceId: ID!) {\n    librarySection(id: $contentSourceId) {\n      id\n      name\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  mutation PlaybackHeartbeat($input: PlaybackHeartbeatInput!) {\n    playbackHeartbeat(input: $input) {\n      playbackSessionId\n      capabilityProfileVersion\n      capabilityVersionMismatch\n    }\n  }\n',
): (typeof documents)['\n  mutation PlaybackHeartbeat($input: PlaybackHeartbeatInput!) {\n    playbackHeartbeat(input: $input) {\n      playbackSessionId\n      capabilityProfileVersion\n      capabilityVersionMismatch\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  mutation DecidePlayback($input: PlaybackDecisionInput!) {\n    decidePlayback(input: $input) {\n      action\n      streamPlanJson\n      nextItemId\n      playbackUrl\n      trickplayUrl\n      capabilityProfileVersion\n      capabilityVersionMismatch\n    }\n  }\n',
): (typeof documents)['\n  mutation DecidePlayback($input: PlaybackDecisionInput!) {\n    decidePlayback(input: $input) {\n      action\n      streamPlanJson\n      nextItemId\n      playbackUrl\n      trickplayUrl\n      capabilityProfileVersion\n      capabilityVersionMismatch\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  mutation PlaybackSeek($input: PlaybackSeekInput!) {\n    playbackSeek(input: $input) {\n      keyframeMs\n      gopDurationMs\n      hasGopIndex\n      originalTargetMs\n    }\n  }\n',
): (typeof documents)['\n  mutation PlaybackSeek($input: PlaybackSeekInput!) {\n    playbackSeek(input: $input) {\n      keyframeMs\n      gopDurationMs\n      hasGopIndex\n      originalTargetMs\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  mutation StartPlayback($input: PlaybackStartInput!) {\n    startPlayback(input: $input) {\n      playbackSessionId\n      playlistGeneratorId\n      capabilityProfileVersion\n      streamPlanJson\n      playbackUrl\n      trickplayUrl\n      durationMs\n      capabilityVersionMismatch\n    }\n  }\n',
): (typeof documents)['\n  mutation StartPlayback($input: PlaybackStartInput!) {\n    startPlayback(input: $input) {\n      playbackSessionId\n      playlistGeneratorId\n      capabilityProfileVersion\n      streamPlanJson\n      playbackUrl\n      trickplayUrl\n      durationMs\n      capabilityVersionMismatch\n    }\n  }\n']

export function graphql(source: string) {
  return (documents as any)[source] ?? {}
}

export type DocumentType<TDocumentNode extends DocumentNode<any, any>> =
  TDocumentNode extends DocumentNode<infer TType, any> ? TType : never
