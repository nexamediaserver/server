import { graphql } from '@/shared/api/graphql'

export const HomeHubDefinitionsQuery = graphql(`
  query HomeHubDefinitions {
    homeHubDefinitions {
      key
      type
      title
      metadataType
      widget
      filterValue
      librarySectionId
      contextId
    }
  }
`)

export const LibraryDiscoverHubDefinitionsQuery = graphql(`
  query LibraryDiscoverHubDefinitions($librarySectionId: ID!) {
    libraryDiscoverHubDefinitions(librarySectionId: $librarySectionId) {
      key
      type
      title
      metadataType
      widget
      filterValue
      librarySectionId
      contextId
    }
  }
`)

export const ItemDetailHubDefinitionsQuery = graphql(`
  query ItemDetailHubDefinitions($itemId: ID!) {
    itemDetailHubDefinitions(itemId: $itemId) {
      key
      type
      title
      metadataType
      widget
      filterValue
      librarySectionId
      contextId
    }
  }
`)

export const HubItemsQuery = graphql(`
  query HubItems($input: GetHubItemsInput!) {
    hubItems(input: $input) {
      id
      librarySectionId
      metadataType
      title
      year
      index
      length
      viewCount
      viewOffset
      thumbUri
      thumbHash
      artUri
      artHash
      logoUri
      logoHash
      tagline
      contentRating
      summary
      context
      parent {
        id
        index
        title
      }
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
    }
  }
`)

export const HubPeopleQuery = graphql(`
  query HubPeople($hubType: HubType!, $metadataItemId: ID!) {
    hubPeople(hubType: $hubType, metadataItemId: $metadataItemId) {
      id
      metadataType
      title
      thumbUri
      thumbHash
      context
    }
  }
`)

export const MetadataItemChildrenQuery = graphql(`
  query MetadataItemChildren($itemId: ID!, $skip: Int, $take: Int) {
    metadataItem(id: $itemId) {
      id
      librarySectionId
      children(skip: $skip, take: $take) {
        items {
          id
          isPromoted
          librarySectionId
          metadataType
          title
          year
          index
          length
          viewCount
          viewOffset
          thumbUri
          thumbHash
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
        }
        pageInfo {
          hasNextPage
          hasPreviousPage
        }
        totalCount
      }
    }
  }
`)
