import { useMemo } from 'react'

import type { DetailFieldType } from '@/shared/api/graphql/graphql'

import { SortableList } from '@/shared/components/SortableList'

interface FieldDescriptor {
  fieldType: DetailFieldType
  isCustom: boolean
  key: string
  label: string
}

interface SortableFieldListProps {
  fields: FieldDescriptor[]
  onOrderChange: (newOrder: FieldDescriptor[]) => void
  onToggle: (field: FieldDescriptor) => void
}

export function SortableFieldList({
  fields,
  onOrderChange,
  onToggle,
}: SortableFieldListProps) {
  const renderField = useMemo(
    () => (field: FieldDescriptor) => (
      <div>
        <p className="font-medium">{field.label}</p>
        <p className="text-xs tracking-wide text-muted-foreground uppercase">
          {field.isCustom ? 'Custom field' : field.fieldType}
        </p>
      </div>
    ),
    [],
  )

  return (
    <SortableList
      getEnabled={() => true}
      getId={(field) => field.key}
      items={fields}
      onOrderChange={onOrderChange}
      onToggle={onToggle}
      renderItem={renderField}
    />
  )
}
