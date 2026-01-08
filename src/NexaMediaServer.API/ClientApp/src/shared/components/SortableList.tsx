import type { DragEndEvent } from '@dnd-kit/core'
import type { ReactNode } from 'react'

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
import { useCallback } from 'react'

import { Switch } from '@/shared/components/ui/switch'
import { cn } from '@/shared/lib/utils'

interface SortableItemProps<TItem> {
  children: ReactNode
  enabled: boolean
  id: string
  item: TItem
  onToggle: (item: TItem, enabled: boolean) => void
}

interface SortableListProps<TItem> {
  getEnabled: (item: TItem) => boolean
  getId: (item: TItem) => string
  items: TItem[]
  onOrderChange: (newOrder: TItem[]) => void
  onToggle: (item: TItem, enabled: boolean) => void
  renderItem: (item: TItem) => ReactNode
}

export function SortableList<TItem>({
  getEnabled,
  getId,
  items,
  onOrderChange,
  onToggle,
  renderItem,
}: SortableListProps<TItem>) {
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
        const oldIndex = items.findIndex((item) => getId(item) === active.id)
        const newIndex = items.findIndex((item) => getId(item) === over.id)
        const reordered = arrayMove(items, oldIndex, newIndex)
        onOrderChange(reordered)
      }
    },
    [items, onOrderChange, getId],
  )

  const itemIds = items.map((item) => getId(item))

  return (
    <DndContext
      collisionDetection={closestCenter}
      onDragEnd={handleDragEnd}
      sensors={sensors}
    >
      <SortableContext items={itemIds} strategy={verticalListSortingStrategy}>
        <div className="space-y-2">
          {items.map((item) => {
            const itemId = getId(item)
            const enabled = getEnabled(item)

            return (
              <SortableItem
                enabled={enabled}
                id={itemId}
                item={item}
                key={itemId}
                onToggle={onToggle}
              >
                {renderItem(item)}
              </SortableItem>
            )
          })}
        </div>
      </SortableContext>
    </DndContext>
  )
}

function SortableItem<TItem>({
  children,
  enabled,
  id,
  item,
  onToggle,
}: SortableItemProps<TItem>) {
  const {
    attributes,
    isDragging,
    listeners,
    setNodeRef,
    transform,
    transition,
  } = useSortable({ id })

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
  }

  return (
    <div
      className={cn(
        `
          flex items-center gap-3 rounded-lg border bg-card p-3
          transition-colors
        `,
        isDragging && 'z-50 border-primary/60 shadow-lg',
        !enabled && 'opacity-50',
      )}
      ref={setNodeRef}
      style={style}
    >
      <button
        aria-label="Drag to reorder"
        className={`
          cursor-grab touch-none text-muted-foreground transition-colors
          hover:text-foreground
          active:cursor-grabbing
        `}
        type="button"
        {...attributes}
        {...listeners}
      >
        <GripVertical className="size-5" />
      </button>

      <div className="min-w-0 flex-1">{children}</div>

      <Switch
        aria-label="Toggle item"
        checked={enabled}
        onCheckedChange={(checked) => {
          onToggle(item, checked)
        }}
      />
    </div>
  )
}
