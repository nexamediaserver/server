import { graphql } from '@/shared/api/graphql'

export const onMetadataItemUpdated = graphql(`
  subscription OnMetadataItemUpdated {
    onMetadataItemUpdated {
      id
      title
      originalTitle
      year
      metadataType
      thumbUri
    }
  }
`)

export const onJobNotification = graphql(`
  subscription OnJobNotification {
    onJobNotification {
      id
      type
      librarySectionId
      librarySectionName
      description
      progressPercentage
      completedItems
      totalItems
      isActive
      timestamp
    }
  }
`)
