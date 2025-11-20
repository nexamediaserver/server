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
  '\n  subscription OnMetadataItemUpdated {\n    onMetadataItemUpdated {\n      id\n      title\n      originalTitle\n      year\n      metadataType\n      thumbUri\n    }\n  }\n': typeof types.OnMetadataItemUpdatedDocument
  '\n  subscription OnJobNotification {\n    onJobNotification {\n      id\n      type\n      librarySectionId\n      librarySectionName\n      description\n      progressPercentage\n      completedItems\n      totalItems\n      isActive\n      timestamp\n    }\n  }\n': typeof types.OnJobNotificationDocument
  '\n  query ServerInfo {\n    serverInfo {\n      versionString\n      isDevelopment\n    }\n  }\n': typeof types.ServerInfoDocument
  '\n  mutation AddLibrarySection($input: AddLibrarySectionInput!) {\n    addLibrarySection(input: $input) {\n      librarySection {\n        id\n        name\n        type\n      }\n    }\n  }\n': typeof types.AddLibrarySectionDocument
  '\n  query FileSystemRoots {\n    fileSystemRoots {\n      id\n      label\n      path\n      kind\n      isReadOnly\n    }\n  }\n': typeof types.FileSystemRootsDocument
  '\n  query BrowseDirectory($path: String!) {\n    browseDirectory(path: $path) {\n      currentPath\n      parentPath\n      entries {\n        name\n        path\n        isDirectory\n        isFile\n        isSymbolicLink\n        isSelectable\n      }\n    }\n  }\n': typeof types.BrowseDirectoryDocument
  '\n  query ContentSourceAll(\n    $contentSourceId: ID!\n    $first: Int\n    $after: String\n    $last: Int\n    $before: String\n  ) {\n    librarySection(id: $contentSourceId) {\n      id\n      name\n      children(\n        first: $first\n        after: $after\n        last: $last\n        before: $before\n        order: { title: ASC }\n      ) {\n        nodes {\n          id\n          title\n          year\n          metadataType\n          thumbUri\n          directPlayUrl\n          trickplayUrl\n        }\n        pageInfo {\n          endCursor\n          startCursor\n          hasNextPage\n          hasPreviousPage\n        }\n        totalCount\n      }\n    }\n  }\n': typeof types.ContentSourceAllDocument
  '\n  query Media($id: ID!) {\n    metadataItem(id: $id) {\n      id\n      title\n      originalTitle\n      thumbUri\n      metadataType\n      directPlayUrl\n      trickplayUrl\n    }\n  }\n': typeof types.MediaDocument
  '\n  query ContentSource($contentSourceId: ID!) {\n    librarySection(id: $contentSourceId) {\n      id\n      name\n    }\n  }\n': typeof types.ContentSourceDocument
}
const documents: Documents = {
  '\n  query LibrarySectionsList(\n    $first: Int\n    $after: String\n    $last: Int\n    $before: String\n  ) {\n    librarySections(\n      first: $first\n      after: $after\n      last: $last\n      before: $before\n    ) {\n      nodes {\n        id\n        name\n        type\n      }\n      pageInfo {\n        hasNextPage\n        hasPreviousPage\n        startCursor\n        endCursor\n      }\n    }\n  }\n':
    types.LibrarySectionsListDocument,
  '\n  subscription OnMetadataItemUpdated {\n    onMetadataItemUpdated {\n      id\n      title\n      originalTitle\n      year\n      metadataType\n      thumbUri\n    }\n  }\n':
    types.OnMetadataItemUpdatedDocument,
  '\n  subscription OnJobNotification {\n    onJobNotification {\n      id\n      type\n      librarySectionId\n      librarySectionName\n      description\n      progressPercentage\n      completedItems\n      totalItems\n      isActive\n      timestamp\n    }\n  }\n':
    types.OnJobNotificationDocument,
  '\n  query ServerInfo {\n    serverInfo {\n      versionString\n      isDevelopment\n    }\n  }\n':
    types.ServerInfoDocument,
  '\n  mutation AddLibrarySection($input: AddLibrarySectionInput!) {\n    addLibrarySection(input: $input) {\n      librarySection {\n        id\n        name\n        type\n      }\n    }\n  }\n':
    types.AddLibrarySectionDocument,
  '\n  query FileSystemRoots {\n    fileSystemRoots {\n      id\n      label\n      path\n      kind\n      isReadOnly\n    }\n  }\n':
    types.FileSystemRootsDocument,
  '\n  query BrowseDirectory($path: String!) {\n    browseDirectory(path: $path) {\n      currentPath\n      parentPath\n      entries {\n        name\n        path\n        isDirectory\n        isFile\n        isSymbolicLink\n        isSelectable\n      }\n    }\n  }\n':
    types.BrowseDirectoryDocument,
  '\n  query ContentSourceAll(\n    $contentSourceId: ID!\n    $first: Int\n    $after: String\n    $last: Int\n    $before: String\n  ) {\n    librarySection(id: $contentSourceId) {\n      id\n      name\n      children(\n        first: $first\n        after: $after\n        last: $last\n        before: $before\n        order: { title: ASC }\n      ) {\n        nodes {\n          id\n          title\n          year\n          metadataType\n          thumbUri\n          directPlayUrl\n          trickplayUrl\n        }\n        pageInfo {\n          endCursor\n          startCursor\n          hasNextPage\n          hasPreviousPage\n        }\n        totalCount\n      }\n    }\n  }\n':
    types.ContentSourceAllDocument,
  '\n  query Media($id: ID!) {\n    metadataItem(id: $id) {\n      id\n      title\n      originalTitle\n      thumbUri\n      metadataType\n      directPlayUrl\n      trickplayUrl\n    }\n  }\n':
    types.MediaDocument,
  '\n  query ContentSource($contentSourceId: ID!) {\n    librarySection(id: $contentSourceId) {\n      id\n      name\n    }\n  }\n':
    types.ContentSourceDocument,
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
  source: '\n  query ContentSourceAll(\n    $contentSourceId: ID!\n    $first: Int\n    $after: String\n    $last: Int\n    $before: String\n  ) {\n    librarySection(id: $contentSourceId) {\n      id\n      name\n      children(\n        first: $first\n        after: $after\n        last: $last\n        before: $before\n        order: { title: ASC }\n      ) {\n        nodes {\n          id\n          title\n          year\n          metadataType\n          thumbUri\n          directPlayUrl\n          trickplayUrl\n        }\n        pageInfo {\n          endCursor\n          startCursor\n          hasNextPage\n          hasPreviousPage\n        }\n        totalCount\n      }\n    }\n  }\n',
): (typeof documents)['\n  query ContentSourceAll(\n    $contentSourceId: ID!\n    $first: Int\n    $after: String\n    $last: Int\n    $before: String\n  ) {\n    librarySection(id: $contentSourceId) {\n      id\n      name\n      children(\n        first: $first\n        after: $after\n        last: $last\n        before: $before\n        order: { title: ASC }\n      ) {\n        nodes {\n          id\n          title\n          year\n          metadataType\n          thumbUri\n          directPlayUrl\n          trickplayUrl\n        }\n        pageInfo {\n          endCursor\n          startCursor\n          hasNextPage\n          hasPreviousPage\n        }\n        totalCount\n      }\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  query Media($id: ID!) {\n    metadataItem(id: $id) {\n      id\n      title\n      originalTitle\n      thumbUri\n      metadataType\n      directPlayUrl\n      trickplayUrl\n    }\n  }\n',
): (typeof documents)['\n  query Media($id: ID!) {\n    metadataItem(id: $id) {\n      id\n      title\n      originalTitle\n      thumbUri\n      metadataType\n      directPlayUrl\n      trickplayUrl\n    }\n  }\n']
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: '\n  query ContentSource($contentSourceId: ID!) {\n    librarySection(id: $contentSourceId) {\n      id\n      name\n    }\n  }\n',
): (typeof documents)['\n  query ContentSource($contentSourceId: ID!) {\n    librarySection(id: $contentSourceId) {\n      id\n      name\n    }\n  }\n']

export function graphql(source: string) {
  return (documents as any)[source] ?? {}
}

export type DocumentType<TDocumentNode extends DocumentNode<any, any>> =
  TDocumentNode extends DocumentNode<infer TType, any> ? TType : never
