import type { ReactNode } from 'react'

import { useQuery } from '@apollo/client/react'

import type { MediaQuery } from '@/shared/api/graphql/graphql'

import { ItemDetailFieldDefinitionsQuery } from '@/features/metadata/queries'
import { DetailFieldType } from '@/shared/api/graphql/graphql'

import { ActionsField } from './ActionsField'
import { DetailFieldRenderer } from './DetailFieldRenderer'

interface DetailFieldsSectionProps {
  contentSourceId: string
  hideActions?: boolean
  metadataItem: MetadataItem
  onEditClick: () => void
  onPlayClick: () => void
  playDisabled?: boolean
}

type MetadataItem = NonNullable<MediaQuery['metadataItem']>

/**
 * Renders the dynamic fields section for item detail pages.
 * Fetches field definitions and renders each field based on its type, widget, and group.
 */
export function DetailFieldsSection({
  contentSourceId,
  hideActions,
  metadataItem,
  onEditClick,
  onPlayClick,
  playDisabled,
}: DetailFieldsSectionProps): ReactNode {
  const { data: fieldData } = useQuery(ItemDetailFieldDefinitionsQuery, {
    skip: !metadataItem.id,
    variables: { itemId: metadataItem.id },
  })

  const definitions = fieldData?.itemDetailFieldDefinitions ?? []

  // Helper to check if a field has a value
  const hasFieldValue = (definition: (typeof definitions)[number]): boolean => {
    // Actions field is always visible (if not hidden)
    if (definition.fieldType === DetailFieldType.Actions) {
      return !hideActions
    }

    const value = getFieldValue(definition, metadataItem)

    if (value === null || value === undefined) {
      return false
    }

    if (Array.isArray(value) && value.length === 0) {
      return false
    }

    return true
  }

  // Helper to get field value
  const getFieldValue = (
    definition: (typeof definitions)[number],
    item: MetadataItem,
  ): unknown => {
    switch (definition.fieldType) {
      case DetailFieldType.ContentRating:
        return item.contentRating
      case DetailFieldType.Custom:
        if (definition.customFieldKey) {
          const field = item.extraFields.find(
            (f) => f.key === definition.customFieldKey,
          )
          return field?.value
        }
        return null
      case DetailFieldType.Genres:
        return item.genres
      case DetailFieldType.OriginalTitle:
        return item.originalTitle
      case DetailFieldType.Runtime:
        return item.length
      case DetailFieldType.Tags:
        return item.tags
      case DetailFieldType.Title:
        return item.title
      case DetailFieldType.Year:
        return item.year
      default:
        return null
    }
  }

  // Filter fields that should be shown
  const visibleFields = definitions.filter((d) => hasFieldValue(d))

  // Group fields by their groupKey
  const groupedFields = visibleFields.reduce<
    Record<string, typeof definitions>
  >((acc, definition) => {
    const groupKey = definition.groupKey ?? '__ungrouped__'
    if (!acc[groupKey]) {
      acc[groupKey] = []
    }
    acc[groupKey].push(definition)
    return acc
  }, {})

  // Create an array of renderable items (either a group or a single field)
  // Each item has a sortOrder to determine its position
  const renderableItems: (
    | { field: (typeof definitions)[number]; sortOrder: number; type: 'field' }
    | {
        fields: typeof definitions
        groupKey: string
        sortOrder: number
        type: 'group'
      }
  )[] = []

  // Add groups
  Object.entries(groupedFields).forEach(([groupKey, fields]) => {
    if (groupKey === '__ungrouped__') {
      // Add each ungrouped field individually (already filtered for hasFieldValue)
      fields.forEach((field) => {
        renderableItems.push({
          field,
          sortOrder: field.sortOrder,
          type: 'field',
        })
      })
    } else {
      // Only add the group if it has at least one field with a value (already filtered)
      if (fields.length > 0) {
        const minSortOrder = Math.min(...fields.map((f) => f.sortOrder))
        renderableItems.push({
          fields,
          groupKey,
          sortOrder: minSortOrder,
          type: 'group',
        })
      }
    }
  })

  // Sort all items by sortOrder
  renderableItems.sort((a, b) => a.sortOrder - b.sortOrder)

  // Helper to render a single field
  const renderField = (definition: (typeof definitions)[number]) => {
    // Actions field gets special rendering
    if (definition.fieldType === DetailFieldType.Actions) {
      return (
        <ActionsField
          isPromoted={metadataItem.isPromoted ?? false}
          itemId={metadataItem.id}
          key={definition.key}
          onEditClick={onEditClick}
          onPlayClick={onPlayClick}
          playDisabled={playDisabled}
        />
      )
    }

    // Custom fields get special rendering with label
    if (definition.fieldType === DetailFieldType.Custom) {
      return (
        <div className="flex flex-row gap-2" key={definition.key}>
          <span className="text-sm text-muted-foreground">
            {definition.label}:
          </span>
          <DetailFieldRenderer
            contentSourceId={contentSourceId}
            customFieldKey={definition.customFieldKey}
            extraFields={metadataItem.extraFields}
            fieldType={definition.fieldType}
            label={definition.label}
            metadataItem={metadataItem}
            widget={definition.widget}
          />
        </div>
      )
    }

    return (
      <DetailFieldRenderer
        contentSourceId={contentSourceId}
        customFieldKey={definition.customFieldKey}
        extraFields={metadataItem.extraFields}
        fieldType={definition.fieldType}
        key={definition.key}
        label={definition.label}
        metadataItem={metadataItem}
        widget={definition.widget}
      />
    )
  }

  return (
    <div
      className={`
        flex flex-col gap-2
        md:gap-4
      `}
    >
      {/* Render all items (groups and fields) in order */}
      {renderableItems.map((item, index) => {
        if (item.type === 'group') {
          const sortedFields = item.fields.sort(
            (a, b) => a.sortOrder - b.sortOrder,
          )

          // Determine layout class based on field types in the group
          const hasHorizontalFields = item.fields.some(
            (f) =>
              f.fieldType === DetailFieldType.Year ||
              f.fieldType === DetailFieldType.Runtime ||
              f.fieldType === DetailFieldType.ContentRating,
          )

          const layoutClass = hasHorizontalFields
            ? 'flex flex-row gap-4'
            : 'flex flex-col gap-1'

          return (
            <div
              className={layoutClass}
              key={`group-${item.groupKey}-${index}`}
            >
              {sortedFields.map((definition) => renderField(definition))}
            </div>
          )
        }

        // Render individual field
        return (
          <div key={`field-${item.field.key}-${index}`}>
            {renderField(item.field)}
          </div>
        )
      })}
    </div>
  )
}
