import { type ReactElement, useMemo } from 'react'
import IconCloud from '~icons/material-symbols/cloud'
import IconDescription from '~icons/material-symbols/description'
import IconFolder from '~icons/material-symbols/folder'
import IconMusicNote from '~icons/material-symbols/music-note'

import {
  MetadataAgentCategory,
  type MetadataAgentInfo,
} from '@/shared/api/graphql/graphql'
import { SortableList } from '@/shared/components/SortableList'
import { cn } from '@/shared/lib/utils'

interface AgentItemState {
  agent: MetadataAgentInfo
  enabled: boolean
}

interface SortableAgentListProps {
  agents: AgentItemState[]
  onOrderChange: (newOrder: string[]) => void
  onToggle: (name: string, enabled: boolean) => void
}

const getCategoryColor = (category: MetadataAgentCategory): string => {
  switch (category) {
    case MetadataAgentCategory.Embedded:
      return 'bg-purple-500/20 text-purple-400'
    case MetadataAgentCategory.Local:
      return 'bg-blue-500/20 text-blue-400'
    case MetadataAgentCategory.Remote:
      return 'bg-green-500/20 text-green-400'
    case MetadataAgentCategory.Sidecar:
      return 'bg-amber-500/20 text-amber-400'
    default:
      return 'bg-stone-500/20 text-stone-400'
  }
}

const getCategoryIcon = (category: MetadataAgentCategory): ReactElement => {
  switch (category) {
    case MetadataAgentCategory.Embedded:
      return <IconMusicNote className="size-4" />
    case MetadataAgentCategory.Local:
      return <IconFolder className="size-4" />
    case MetadataAgentCategory.Remote:
      return <IconCloud className="size-4" />
    case MetadataAgentCategory.Sidecar:
      return <IconDescription className="size-4" />
    default:
      return <IconFolder className="size-4" />
  }
}

const getCategoryLabel = (category: MetadataAgentCategory): string => {
  switch (category) {
    case MetadataAgentCategory.Embedded:
      return 'Embedded'
    case MetadataAgentCategory.Local:
      return 'Local'
    case MetadataAgentCategory.Remote:
      return 'Remote'
    case MetadataAgentCategory.Sidecar:
      return 'Sidecar'
    default:
      return 'Unknown'
  }
}

export function SortableAgentList({
  agents,
  onOrderChange,
  onToggle,
}: Readonly<SortableAgentListProps>) {
  const renderAgent = useMemo(
    () => (item: AgentItemState) => (
      <div className="flex items-center gap-3">
        <span
          className={cn(
            `
              flex items-center gap-1.5 rounded-full px-2 py-0.5 text-xs
              font-medium
            `,
            getCategoryColor(item.agent.category),
          )}
        >
          {getCategoryIcon(item.agent.category)}
          {getCategoryLabel(item.agent.category)}
        </span>
        <div className="min-w-0 flex-1">
          <p className="truncate font-medium">{item.agent.displayName}</p>
          <p className="truncate text-xs text-muted-foreground">
            {item.agent.description}
          </p>
        </div>
      </div>
    ),
    [],
  )

  return (
    <SortableList
      getEnabled={(item) => item.enabled}
      getId={(item) => item.agent.name}
      items={agents}
      onOrderChange={(newOrder) => {
        onOrderChange(newOrder.map((item) => item.agent.name))
      }}
      onToggle={(item, enabled) => {
        onToggle(item.agent.name, enabled)
      }}
      renderItem={renderAgent}
    />
  )
}

export type { AgentItemState }
