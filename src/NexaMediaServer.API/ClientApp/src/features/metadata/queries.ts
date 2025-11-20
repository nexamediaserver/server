import { graphql } from '@/shared/api/graphql'

export const MetadataItemQuery = graphql(`
  query Media($id: ID!) {
    metadataItem(id: $id) {
      id
      librarySectionId
      title
      originalTitle
      thumbUri
      thumbHash
      metadataType
      year
      length
      genres
      tags
      contentRating
      viewOffset
      isPromoted
    }
  }
`)

export const ContentSourceQuery = graphql(`
  query ContentSource($contentSourceId: ID!) {
    librarySection(id: $contentSourceId) {
      id
      name
    }
  }
`)
