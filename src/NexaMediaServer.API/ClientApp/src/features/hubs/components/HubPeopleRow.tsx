import { useQuery } from '@apollo/client/react'

import type { HubDefinition } from '@/shared/api/graphql/graphql'

import { RoleSlider } from '@/features/metadata/components/RoleSlider'

import { HubPeopleQuery } from '../queries'

type HubPeopleRowProps = Readonly<{
  definition: Pick<HubDefinition, 'contextId' | 'key' | 'title' | 'type'>
  librarySectionId: string
  metadataItemId: string
}>

/**
 * Renders a hub row with people (cast/crew) fetched based on the hub definition.
 * Hides itself if no people are available.
 */
export function HubPeopleRow({
  definition,
  librarySectionId,
  metadataItemId,
}: HubPeopleRowProps) {
  const { data, loading } = useQuery(HubPeopleQuery, {
    variables: {
      hubType: definition.type,
      metadataItemId: metadataItemId,
    },
  })

  // Don't render anything while loading or if no people
  if (loading || !data?.hubPeople.length) {
    return null
  }

  return (
    <div className="min-w-0">
      <RoleSlider
        heading={definition.title}
        librarySectionId={librarySectionId}
        roles={data.hubPeople}
      />
    </div>
  )
}
