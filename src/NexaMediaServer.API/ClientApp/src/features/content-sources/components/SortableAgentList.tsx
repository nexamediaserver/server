import type { DragEndEvent } from '@dnd-kit/core'

import {
  closestCenter,
  DndContext,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
} from '@dnd-kit/core'
import {
  arrayMove,
  SortableContext,
  sortableKeyboardCoordinates,
  useSortable,
  verticalListSortingStrategy,
} from '@dnd-kit/sortable'
import { CSS } from '@dnd-kit/utilities'
import { GripVertical } from 'lucide-react'
import { type ReactElement, useCallback } from 'react'
import IconCloud from '~icons/material-symbols/cloud'
import IconDescription from '~icons/material-symbols/description'
import IconFolder from '~icons/material-symbols/folder'
import IconMusicNote from '~icons/material-symbols/music-note'

import {
  MetadataAgentCategory,
  type MetadataAgentInfo,
} from '@/shared/api/graphql/graphql'
import { Switch } from '@/shared/components/ui/switch'
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

const SortableAgentItem = ({
  item,
  onToggle,
}: Readonly<{
  item: AgentItemState
  onToggle: (name: string, enabled: boolean) => void
}>) => {
  const {
    attributes,
    isDragging,
    listeners,
    setNodeRef,
    transform,
    transition,
  } = useSortable({ id: item.agent.name })

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
  }

  return (
    <div
      className={cn(
        `
          flex items-center gap-3 rounded-lg border border-stone-700/70
          bg-stone-800 p-3 transition-colors
        `,
        isDragging && 'z-50 border-primary/60 shadow-lg',
        !item.enabled && 'opacity-50',
      )}
      ref={setNodeRef}
      style={style}
    >
      <button
        aria-label="Drag to reorder"
        className={`
          cursor-grab touch-none text-stone-500 transition-colors
          hover:text-stone-300
          active:cursor-grabbing
        `}
        type="button"
        {...attributes}
        {...listeners}
      >
        <GripVertical className="size-5" />
      </button>

      <div className="flex flex-1 items-center gap-3">
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
          <p className="truncate text-xs text-stone-400">
            {item.agent.description}
          </p>
        </div>
      </div>

      <Switch
        aria-label={`Enable ${item.agent.displayName}`}
        checked={item.enabled}
        onCheckedChange={(checked) => {
          onToggle(item.agent.name, checked)
        }}
      />
    </div>
  )
}

export function SortableAgentList({
  agents,
  onOrderChange,
  onToggle,
}: Readonly<SortableAgentListProps>) {
  const sensors = useSensors(
    useSensor(PointerSensor),
    useSensor(KeyboardSensor, {
      coordinateGetter: sortableKeyboardCoordinates,
    }),
  )

  const handleDragEnd = useCallback(
    (event: DragEndEvent) => {
      const { active, over } = event

      if (over && active.id !== over.id) {
        const oldIndex = agents.findIndex((a) => a.agent.name === active.id)
        const newIndex = agents.findIndex((a) => a.agent.name === over.id)
        const reordered = arrayMove(agents, oldIndex, newIndex)
        onOrderChange(reordered.map((a) => a.agent.name))
      }
    },
    [agents, onOrderChange],
  )

  const agentIds = agents.map((a) => a.agent.name)

  return (
    <DndContext
      collisionDetection={closestCenter}
      onDragEnd={handleDragEnd}
      sensors={sensors}
    >
      <SortableContext items={agentIds} strategy={verticalListSortingStrategy}>
        <div className="space-y-2">
          {agents.map((item) => (
            <SortableAgentItem
              item={item}
              key={item.agent.name}
              onToggle={onToggle}
            />
          ))}
        </div>
      </SortableContext>
    </DndContext>
  )
}

export type { AgentItemState }
