import { graphql } from '@/shared/api/graphql'

export const updateMetadataItemDocument = graphql(`
  mutation UpdateMetadataItem($input: UpdateMetadataItemInput!) {
    updateMetadataItem(input: $input) {
      success
      error
      item {
        id
        title
        titleSort
        originalTitle
        summary
        tagline
        contentRating
        year
        originallyAvailableAt
        genres
        tags
        lockedFields
        externalIds {
          provider
          value
        }
        extraFields {
          key
          value
        }
      }
    }
  }
`)

export const lockMetadataFieldsDocument = graphql(`
  mutation LockMetadataFields($input: LockMetadataFieldsInput!) {
    lockMetadataFields(input: $input) {
      success
      error
      lockedFields
    }
  }
`)

export const unlockMetadataFieldsDocument = graphql(`
  mutation UnlockMetadataFields($input: UnlockMetadataFieldsInput!) {
    unlockMetadataFields(input: $input) {
      success
      error
      lockedFields
    }
  }
`)

export const metadataItemForEditDocument = graphql(`
  query MetadataItemForEdit($id: ID!) {
    metadataItem(id: $id) {
      id
      metadataType
      title
      titleSort
      originalTitle
      summary
      tagline
      contentRating
      year
      originallyAvailableAt
      genres
      tags
      lockedFields
      externalIds {
        provider
        value
      }
      extraFields {
        key
        value
      }
      thumbUri
      thumbHash
    }
  }
`)
