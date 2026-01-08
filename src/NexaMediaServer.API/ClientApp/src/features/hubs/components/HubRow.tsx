import {
  type HubDefinition,
  HubWidgetType,
  type MetadataType,
} from '@/shared/api/graphql/graphql'

import { HubGridRow } from './HubGridRow'
import { HubHeroCarousel } from './HubHeroCarousel'
import { HubItemRow } from './HubItemRow'
import { HubPeopleRow } from './HubPeopleRow'
import { HubTimelineRow } from './HubTimelineRow'
import { HubTracklistRow } from './HubTracklistRow'

type HubRowProps = Readonly<{
  definition: HubDefinition
  librarySectionId?: string
  metadataItemId?: string
}>

/**
 * Renders the appropriate hub row component based on widget type and metadata type.
 */
export function HubRow({
  definition,
  librarySectionId,
  metadataItemId,
}: HubRowProps) {
  // People hubs always use slider regardless of widget setting
  if (definition.metadataType === ('PERSON' as MetadataType)) {
    // People hubs require a metadata item ID
    if (!metadataItemId && !definition.contextId) {
      return null
    }

    return (
      <HubPeopleRow
        definition={definition}
        librarySectionId={librarySectionId ?? definition.librarySectionId ?? ''}
        metadataItemId={metadataItemId ?? definition.contextId ?? ''}
      />
    )
  }

  // Route to appropriate widget based on widget type
  switch (definition.widget) {
    case HubWidgetType.Grid:
      return (
        <HubGridRow
          definition={definition}
          librarySectionId={librarySectionId}
          metadataItemId={metadataItemId}
        />
      )
    case HubWidgetType.Hero:
      return (
        <HubHeroCarousel
          definition={definition}
          librarySectionId={librarySectionId}
          metadataItemId={metadataItemId}
        />
      )
    case HubWidgetType.Timeline:
      return (
        <HubTimelineRow
          definition={definition}
          librarySectionId={librarySectionId}
          metadataItemId={metadataItemId}
        />
      )
    case HubWidgetType.Tracklist:
      return (
        <HubTracklistRow
          definition={definition}
          librarySectionId={librarySectionId}
          metadataItemId={metadataItemId}
        />
      )
    case HubWidgetType.Slider:
    default:
      return (
        <HubItemRow
          definition={definition}
          librarySectionId={librarySectionId}
          metadataItemId={metadataItemId}
        />
      )
  }
}
