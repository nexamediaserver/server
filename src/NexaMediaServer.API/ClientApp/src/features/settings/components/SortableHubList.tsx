import { useMemo } from 'react'

import type { HubType } from '@/shared/api/graphql/graphql'

import { SortableList } from '@/shared/components/SortableList'

interface HubOption {
  description?: null | string
  title: string
  type: HubType
}

interface SortableHubListProps {
  hubs: HubOption[]
  onOrderChange: (newOrder: HubType[]) => void
  onToggle: (type: HubType) => void
}

export function SortableHubList({
  hubs,
  onOrderChange,
  onToggle,
}: SortableHubListProps) {
  const renderHub = useMemo(
    () => (hub: HubOption) => (
      <div>
        <p className="font-medium">{hub.title}</p>
        {hub.description && (
          <p className="text-sm text-muted-foreground">{hub.description}</p>
        )}
      </div>
    ),
    [],
  )

  return (
    <SortableList
      getEnabled={() => true}
      getId={(hub) => hub.type}
      items={hubs}
      onOrderChange={(newOrder) => {
        onOrderChange(newOrder.map((hub) => hub.type))
      }}
      onToggle={(hub) => {
        onToggle(hub.type)
      }}
      renderItem={renderHub}
    />
  )
}
