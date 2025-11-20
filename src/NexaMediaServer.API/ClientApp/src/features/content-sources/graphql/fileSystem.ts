import { graphql } from '@/shared/api/graphql'

export const fileSystemRootsQueryDocument = graphql(`
  query FileSystemRoots {
    fileSystemRoots {
      id
      label
      path
      kind
      isReadOnly
    }
  }
`)

export const browseDirectoryQueryDocument = graphql(`
  query BrowseDirectory($path: String!) {
    browseDirectory(path: $path) {
      currentPath
      parentPath
      entries {
        name
        path
        isDirectory
        isFile
        isSymbolicLink
        isSelectable
      }
    }
  }
`)
