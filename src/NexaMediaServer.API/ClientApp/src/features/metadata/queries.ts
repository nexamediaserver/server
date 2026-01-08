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
      artUri
      artHash
      metadataType
      year
      length
      genres
      tags
      contentRating
      viewCount
      viewOffset
      isPromoted
      primaryPerson {
        id
        title
        metadataType
      }
      persons {
        id
        title
        metadataType
      }
      extraFields {
        key
        value
      }
    }
  }
`)

export const ItemDetailFieldDefinitionsQuery = graphql(`
  query ItemDetailFieldDefinitions($itemId: ID!) {
    itemDetailFieldDefinitions(itemId: $itemId) {
      key
      fieldType
      label
      widget
      sortOrder
      customFieldKey
      groupKey
    }
  }
`)

export const FieldDefinitionsForTypeQuery = graphql(`
  query FieldDefinitionsForType($metadataType: MetadataType!) {
    fieldDefinitionsForType(metadataType: $metadataType) {
      key
      fieldType
      label
      widget
      sortOrder
      customFieldKey
      groupKey
    }
  }
`)

export const CustomFieldDefinitionsQuery = graphql(`
  query CustomFieldDefinitions {
    customFieldDefinitions {
      id
      key
      label
      widget
      applicableMetadataTypes
      sortOrder
      isEnabled
    }
  }
`)

// Mutations for custom field management (admin)
export const CreateCustomFieldDefinitionMutation = graphql(`
  mutation CreateCustomFieldDefinition(
    $input: CreateCustomFieldDefinitionInput!
  ) {
    createCustomFieldDefinition(input: $input) {
      id
      key
      label
      widget
      applicableMetadataTypes
      sortOrder
      isEnabled
    }
  }
`)

export const UpdateCustomFieldDefinitionMutation = graphql(`
  mutation UpdateCustomFieldDefinition(
    $input: UpdateCustomFieldDefinitionInput!
  ) {
    updateCustomFieldDefinition(input: $input) {
      id
      key
      label
      widget
      applicableMetadataTypes
      sortOrder
      isEnabled
    }
  }
`)

export const DeleteCustomFieldDefinitionMutation = graphql(`
  mutation DeleteCustomFieldDefinition($id: ID!) {
    deleteCustomFieldDefinition(id: $id)
  }
`)

// Mutation for user field configuration
export const UpdateDetailFieldConfigurationMutation = graphql(`
  mutation UpdateDetailFieldConfiguration(
    $input: UpdateDetailFieldConfigurationInput!
  ) {
    updateDetailFieldConfiguration(input: $input) {
      key
      fieldType
      label
      widget
      sortOrder
      customFieldKey
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
