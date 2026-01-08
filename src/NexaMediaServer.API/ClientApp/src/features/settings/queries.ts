import { graphql } from '@/shared/api/graphql'

export const HubConfigurationQuery = graphql(`
  query HubConfiguration($input: HubConfigurationScopeInput!) {
    hubConfiguration(input: $input) {
      enabledHubTypes
      disabledHubTypes
    }
  }
`)

export const UpdateHubConfigurationMutation = graphql(`
  mutation UpdateHubConfiguration($input: UpdateHubConfigurationInput!) {
    updateHubConfiguration(input: $input) {
      enabledHubTypes
      disabledHubTypes
    }
  }
`)

export const AdminDetailFieldConfigurationQuery = graphql(`
  query AdminDetailFieldConfiguration(
    $input: DetailFieldConfigurationScopeInput!
  ) {
    adminDetailFieldConfiguration(input: $input) {
      metadataType
      librarySectionId
      enabledFieldTypes
      disabledFieldTypes
      disabledCustomFieldKeys
      fieldGroups {
        groupKey
        label
        layoutType
        sortOrder
        isCollapsible
      }
      fieldGroupAssignments {
        key
        value
      }
    }
  }
`)

export const UpdateAdminDetailFieldConfigurationMutation = graphql(`
  mutation UpdateAdminDetailFieldConfiguration(
    $input: UpdateAdminDetailFieldConfigurationInput!
  ) {
    updateAdminDetailFieldConfiguration(input: $input) {
      metadataType
      librarySectionId
      enabledFieldTypes
      disabledFieldTypes
      disabledCustomFieldKeys
      fieldGroups {
        groupKey
        label
        layoutType
        sortOrder
        isCollapsible
      }
      fieldGroupAssignments {
        key
        value
      }
    }
  }
`)

export const AdminLibrarySectionsListQuery = graphql(`
  query AdminLibrarySectionsList {
    librarySections(first: 50, order: [{ name: ASC }]) {
      edges {
        node {
          id
          name
          type
        }
      }
    }
  }
`)
