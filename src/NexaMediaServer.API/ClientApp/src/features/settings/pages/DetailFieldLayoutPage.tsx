import type { DragEndEvent } from '@dnd-kit/core'

import { useMutation, useQuery } from '@apollo/client/react'
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
import { ChevronRight, GripVertical, Plus, Trash2 } from 'lucide-react'
import { useCallback, useEffect, useMemo, useState } from 'react'

import { FieldDefinitionsForTypeQuery } from '@/features/metadata/queries'
import { useConfigurationDefaults } from '@/features/settings/hooks'
import {
  DetailFieldGroupLayoutType,
  DetailFieldType,
  MetadataType,
} from '@/shared/api/graphql/graphql'
import { Button } from '@/shared/components/ui/button'
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from '@/shared/components/ui/card'
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/shared/components/ui/dialog'
import { Input } from '@/shared/components/ui/input'
import { Label } from '@/shared/components/ui/label'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/shared/components/ui/select'
import { Switch } from '@/shared/components/ui/switch'
import { cn } from '@/shared/lib/utils'

import { SettingsPageContainer, SettingsPageHeader } from '../components'
import {
  AdminDetailFieldConfigurationQuery,
  AdminLibrarySectionsListQuery,
  UpdateAdminDetailFieldConfigurationMutation,
} from '../queries'

interface FieldDescriptor {
  fieldType: DetailFieldType
  groupKey?: null | string
  isCustom: boolean
  key: string
  label: string
}

interface FieldGroup {
  groupKey: string
  isCollapsible: boolean
  label: string
  layoutType: DetailFieldGroupLayoutType
  sortOrder: number
}

type LayoutItem =
  | { enabled: boolean; field: FieldDescriptor; type: 'field' }
  | {
      expanded: boolean
      fields: FieldDescriptor[]
      group: FieldGroup
      type: 'group'
    }

const layoutTypeOptions = [
  { label: 'Vertical (Stack)', value: DetailFieldGroupLayoutType.Vertical },
  { label: 'Horizontal (Row)', value: DetailFieldGroupLayoutType.Horizontal },
  { label: 'Grid', value: DetailFieldGroupLayoutType.Grid },
]

interface SortableLayoutItemProps {
  enabled: boolean
  enabledFields: FieldDescriptor[]
  item: LayoutItem
  onDeleteGroup: (groupKey: string) => void
  onEditGroup: (group: FieldGroup) => void
  onMoveFieldToGroup: (fieldKey: string, groupKey: null | string) => void
  onToggleExpanded: (groupKey: string) => void
  onToggleField: (fieldKey: string) => void
}

export function DetailFieldLayoutPage() {
  const [metadataType, setMetadataType] = useState<MetadataType | undefined>()
  const [libraryId, setLibraryId] = useState<string | undefined>()
  const [groups, setGroups] = useState<FieldGroup[]>([])
  const [fieldGroupAssignments, setFieldGroupAssignments] = useState<
    Record<string, string>
  >({})
  const [layoutItems, setLayoutItems] = useState<LayoutItem[]>([])
  const [groupDialogOpen, setGroupDialogOpen] = useState(false)
  const [editingGroupKey, setEditingGroupKey] = useState<null | string>(null)
  const [groupFormData, setGroupFormData] = useState<FieldGroup>({
    groupKey: '',
    isCollapsible: false,
    label: '',
    layoutType: DetailFieldGroupLayoutType.Horizontal,
    sortOrder: 0,
  })

  const metadataTypeOptions = useMemo(
    () =>
      Object.values(MetadataType).filter(
        (type) => type !== MetadataType.Unknown,
      ),
    [],
  )

  const { data: librariesData } = useQuery(AdminLibrarySectionsListQuery)

  const { data: definitionData } = useQuery(FieldDefinitionsForTypeQuery, {
    skip: !metadataType,
    variables: { metadataType: metadataType ?? MetadataType.Movie },
  })

  const { data: configData, refetch } = useQuery(
    AdminDetailFieldConfigurationQuery,
    {
      onCompleted: (data) => {
        // eslint-disable-next-line @typescript-eslint/no-unsafe-member-access
        if (data.adminDetailFieldConfiguration?.fieldGroups) {
          setGroups(
            // eslint-disable-next-line @typescript-eslint/no-unsafe-argument, @typescript-eslint/no-unsafe-call, @typescript-eslint/no-unsafe-member-access
            data.adminDetailFieldConfiguration.fieldGroups.map(
              // eslint-disable-next-line @typescript-eslint/no-explicit-any
              (g: any) => ({
                // eslint-disable-next-line @typescript-eslint/no-unsafe-assignment, @typescript-eslint/no-unsafe-member-access
                groupKey: g.groupKey,
                // eslint-disable-next-line @typescript-eslint/no-unsafe-assignment, @typescript-eslint/no-unsafe-member-access
                isCollapsible: g.isCollapsible,
                // eslint-disable-next-line @typescript-eslint/no-unsafe-assignment, @typescript-eslint/no-unsafe-member-access
                label: g.label,
                // eslint-disable-next-line @typescript-eslint/no-unsafe-assignment, @typescript-eslint/no-unsafe-member-access
                layoutType: g.layoutType,
                // eslint-disable-next-line @typescript-eslint/no-unsafe-assignment, @typescript-eslint/no-unsafe-member-access
                sortOrder: g.sortOrder,
              }),
            ),
          )
        } else {
          setGroups([])
        }

        // eslint-disable-next-line @typescript-eslint/no-unsafe-member-access
        if (data.adminDetailFieldConfiguration?.fieldGroupAssignments) {
          const assignments: Record<string, string> = {}
          // eslint-disable-next-line @typescript-eslint/no-unsafe-call, @typescript-eslint/no-unsafe-member-access
          data.adminDetailFieldConfiguration.fieldGroupAssignments.forEach(
            // eslint-disable-next-line @typescript-eslint/no-explicit-any
            (pair: any) => {
              // eslint-disable-next-line @typescript-eslint/no-unsafe-assignment, @typescript-eslint/no-unsafe-member-access
              assignments[pair.key] = pair.value
            },
          )
          setFieldGroupAssignments(assignments)
        } else {
          setFieldGroupAssignments({})
        }
      },
      skip: !metadataType,
      variables: {
        input: {
          librarySectionId: libraryId ?? null,
          metadataType: metadataType ?? MetadataType.Movie,
        },
      },
    },
  )

  const [updateConfig, { loading: saving }] = useMutation(
    UpdateAdminDetailFieldConfigurationMutation,
  )

  const allFields: FieldDescriptor[] = useMemo(() => {
    if (!definitionData || !metadataType) return []
    return definitionData.fieldDefinitionsForType.map((definition) => ({
      fieldType: definition.fieldType,
      groupKey: definition.groupKey,
      isCustom: !!definition.customFieldKey,
      key: definition.customFieldKey ?? definition.fieldType,
      label: definition.label,
    }))
  }, [definitionData, metadataType])

  const adaptedConfigData = useMemo(() => {
    if (!configData) return configData
    const config = configData.adminDetailFieldConfiguration
    if (!config) return null

    const enabledByConfig = new Set(config.enabledFieldTypes)
    const disabledTypes = new Set(config.disabledFieldTypes)
    const disabledCustomKeys = new Set(config.disabledCustomFieldKeys)

    const enabled = allFields.filter((field) => {
      if (field.isCustom)
        return (
          enabledByConfig.has(field.fieldType) &&
          !disabledCustomKeys.has(field.key)
        )
      return enabledByConfig.has(field.fieldType)
    })

    const disabled = allFields.filter((field) => {
      if (field.isCustom) return disabledCustomKeys.has(field.key)
      return disabledTypes.has(field.fieldType)
    })

    return { disabled, enabled }
  }, [configData, allFields])

  const {
    disabled: disabledFields,
    enabled: enabledFields,
    setDisabled: setDisabledFields,
    setEnabled: setEnabledFields,
  } = useConfigurationDefaults<FieldDescriptor>({
    availableOptions: allFields,
    configData: adaptedConfigData,
    isEqual: (a, b) => a.key === b.key,
    resetDependencies: [metadataType, libraryId],
    trackHidden: false,
  })

  // Build layout items whenever fields or groups change
  const computedLayoutItems = useMemo(() => {
    if (!enabledFields || !disabledFields) {
      return []
    }

    const allFieldsList = [...enabledFields, ...disabledFields]
    const enabledSet = new Set(enabledFields.map((f) => f.key))
    const items: LayoutItem[] = []

    // Group fields by their groupKey
    const fieldsByGroup = new Map<null | string, FieldDescriptor[]>()
    allFieldsList.forEach((field) => {
      const groupKey = fieldGroupAssignments[field.key] ?? null
      const groupFields = fieldsByGroup.get(groupKey) ?? []
      groupFields.push(field)
      fieldsByGroup.set(groupKey, groupFields)
    })

    // Add ungrouped fields
    const ungroupedFields = fieldsByGroup.get(null) ?? []
    ungroupedFields.forEach((field) => {
      items.push({
        enabled: enabledSet.has(field.key),
        field,
        type: 'field',
      })
    })

    // Add groups with their fields
    groups.forEach((group) => {
      const groupFields = fieldsByGroup.get(group.groupKey) ?? []
      items.push({
        expanded: false,
        fields: groupFields,
        group,
        type: 'group',
      })
    })

    return items
  }, [enabledFields, disabledFields, groups, fieldGroupAssignments])

  // Sync computed items to state for mutation operations
  useEffect(() => {
    setLayoutItems(computedLayoutItems)
  }, [computedLayoutItems])

  const ready = !!metadataType && enabledFields !== null

  const handleSave = async () => {
    if (!metadataType || !enabledFields || !disabledFields) return

    const enabledTypes = new Set<DetailFieldType>()
    const disabledTypes = new Set<DetailFieldType>()
    const disabledCustomKeys = new Set<string>()

    enabledFields.forEach((field) => {
      if (field.isCustom) {
        enabledTypes.add(field.fieldType)
        return
      }
      enabledTypes.add(field.fieldType)
    })

    disabledFields.forEach((field) => {
      if (field.isCustom) {
        disabledCustomKeys.add(field.key)
        enabledTypes.add(field.fieldType)
        return
      }
      disabledTypes.add(field.fieldType)
    })

    const payload = {
      disabledCustomFieldKeys: Array.from(disabledCustomKeys),
      disabledFieldTypes: Array.from(disabledTypes),
      enabledFieldTypes: Array.from(enabledTypes),
      fieldGroupAssignments,
      fieldGroups:
        groups.length > 0
          ? groups.map((g) => ({
              groupKey: g.groupKey,
              isCollapsible: g.isCollapsible,
              label: g.label,
              layoutType: g.layoutType,
              sortOrder: g.sortOrder,
            }))
          : null,
      librarySectionId: libraryId ?? null,
      metadataType,
    }

    await updateConfig({ variables: { input: payload } })
    await refetch()
  }

  const toggleField = (fieldKey: string) => {
    const field = allFields.find((f) => f.key === fieldKey)
    if (!field) return

    const isEnabled = enabledFields?.some((f) => f.key === fieldKey) ?? false

    if (isEnabled) {
      setDisabledFields((current) => (current ? [...current, field] : [field]))
      setEnabledFields(
        (current) => current?.filter((f) => f.key !== fieldKey) ?? [],
      )
    } else {
      setEnabledFields((current) => (current ? [...current, field] : [field]))
      setDisabledFields(
        (current) => current?.filter((f) => f.key !== fieldKey) ?? [],
      )
    }
  }

  const moveFieldToGroup = (fieldKey: string, groupKey: null | string) => {
    if (groupKey === null) {
      // eslint-disable-next-line @typescript-eslint/no-unused-vars
      const { [fieldKey]: _, ...rest } = fieldGroupAssignments
      setFieldGroupAssignments(rest)
    } else {
      setFieldGroupAssignments({
        ...fieldGroupAssignments,
        [fieldKey]: groupKey,
      })
    }
  }

  const handleOpenCreateGroup = () => {
    setEditingGroupKey(null)
    setGroupFormData({
      groupKey: '',
      isCollapsible: false,
      label: '',
      layoutType: DetailFieldGroupLayoutType.Horizontal,
      sortOrder: groups.length,
    })
    setGroupDialogOpen(true)
  }

  const handleOpenEditGroup = (group: FieldGroup) => {
    setEditingGroupKey(group.groupKey)
    setGroupFormData({ ...group })
    setGroupDialogOpen(true)
  }

  const handleSubmitGroup = () => {
    if (editingGroupKey) {
      setGroups(
        groups.map((g) => (g.groupKey === editingGroupKey ? groupFormData : g)),
      )
    } else {
      setGroups([...groups, groupFormData])
    }
    setGroupDialogOpen(false)
  }

  const handleDeleteGroup = (groupKey: string) => {
    if (confirm('Are you sure you want to delete this group?')) {
      setGroups(groups.filter((g) => g.groupKey !== groupKey))
      // Remove assignments for this group
      const newAssignments = Object.fromEntries(
        Object.entries(fieldGroupAssignments).filter(
          ([, value]) => value !== groupKey,
        ),
      )
      setFieldGroupAssignments(newAssignments)
    }
  }

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
        const oldIndex = layoutItems.findIndex(
          (item) =>
            (item.type === 'field'
              ? `field-${item.field.key}`
              : `group-${item.group.groupKey}`) === active.id,
        )
        const newIndex = layoutItems.findIndex(
          (item) =>
            (item.type === 'field'
              ? `field-${item.field.key}`
              : `group-${item.group.groupKey}`) === over.id,
        )
        const newItems = arrayMove(layoutItems, oldIndex, newIndex)
        setLayoutItems(newItems)

        // Update group sortOrders
        const newGroups = [...groups]
        newItems.forEach((item, idx) => {
          if (item.type === 'group') {
            item.group.sortOrder = idx
            const groupIndex = newGroups.findIndex(
              (g) => g.groupKey === item.group.groupKey,
            )
            if (groupIndex !== -1) {
              newGroups[groupIndex] = { ...item.group }
            }
          }
        })
        setGroups(newGroups)
      }
    },
    [layoutItems, groups],
  )

  const toggleGroupExpanded = (groupKey: string) => {
    const newItems = layoutItems.map((item) => {
      if (item.type === 'group' && item.group.groupKey === groupKey) {
        return { ...item, expanded: !item.expanded }
      }
      return item
    })
    setLayoutItems(newItems)
  }

  const getItemId = (item: LayoutItem): string => {
    return item.type === 'field'
      ? `field-${item.field.key}`
      : `group-${item.group.groupKey}`
  }

  const itemIds = layoutItems.map(getItemId)

  const libraryOptions = useMemo(
    () => librariesData?.librarySections?.edges?.map((edge) => edge.node) ?? [],
    [librariesData],
  )

  const headerActions = (
    <>
      <Select
        onValueChange={(value) => {
          setMetadataType(value as MetadataType)
        }}
        value={metadataType ?? ''}
      >
        <SelectTrigger className="w-48">
          <SelectValue placeholder="Pick item type" />
        </SelectTrigger>
        <SelectContent>
          {metadataTypeOptions.map((type) => (
            <SelectItem key={type} value={type}>
              {type}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
      <Select
        onValueChange={(value) => {
          setLibraryId(value === '__all__' ? undefined : value)
        }}
        value={libraryId ?? '__all__'}
      >
        <SelectTrigger className="w-48">
          <SelectValue placeholder="All libraries" />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="__all__">All libraries</SelectItem>
          {libraryOptions.map((library) => (
            <SelectItem key={library.id} value={library.id}>
              {library.name}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
      <Button disabled={!ready || saving} onClick={() => void handleSave()}>
        {saving ? 'Saving...' : 'Save'}
      </Button>
    </>
  )

  return (
    <SettingsPageContainer maxWidth="sm">
      <SettingsPageHeader
        actions={headerActions}
        description="Configure which fields appear on item detail pages and their order"
        title="Detail Field Layout"
      />

      {!ready && (
        <p className="text-muted-foreground">Select an item type to edit.</p>
      )}

      {ready && (
        <Card>
          <CardHeader>
            <div className="flex items-center justify-between">
              <CardTitle>Fields & Groups</CardTitle>
              <Button
                onClick={handleOpenCreateGroup}
                size="sm"
                variant="outline"
              >
                <Plus className="mr-2 size-4" />
                Add Group
              </Button>
            </div>
          </CardHeader>
          <CardContent>
            {layoutItems.length === 0 && (
              <p className="text-sm text-muted-foreground">
                No fields available
              </p>
            )}
            {layoutItems.length > 0 && (
              <DndContext
                collisionDetection={closestCenter}
                onDragEnd={handleDragEnd}
                sensors={sensors}
              >
                <SortableContext
                  items={itemIds}
                  strategy={verticalListSortingStrategy}
                >
                  <div className="space-y-2">
                    {layoutItems.map((item) => {
                      const itemId = getItemId(item)
                      return (
                        <SortableLayoutItem
                          enabled={item.type === 'field' ? item.enabled : true}
                          enabledFields={enabledFields ?? []}
                          item={item}
                          key={itemId}
                          onDeleteGroup={handleDeleteGroup}
                          onEditGroup={handleOpenEditGroup}
                          onMoveFieldToGroup={moveFieldToGroup}
                          onToggleExpanded={toggleGroupExpanded}
                          onToggleField={toggleField}
                        />
                      )
                    })}
                  </div>
                </SortableContext>
              </DndContext>
            )}
          </CardContent>
        </Card>
      )}

      <Dialog onOpenChange={setGroupDialogOpen} open={groupDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>
              {editingGroupKey ? 'Edit Group' : 'Create Group'}
            </DialogTitle>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="label">Label</Label>
              <Input
                id="label"
                onChange={(e) => {
                  setGroupFormData({ ...groupFormData, label: e.target.value })
                }}
                placeholder="Release Info"
                value={groupFormData.label}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="groupKey">Group Key</Label>
              <Input
                disabled={!!editingGroupKey}
                id="groupKey"
                onChange={(e) => {
                  setGroupFormData({
                    ...groupFormData,
                    groupKey: e.target.value,
                  })
                }}
                placeholder="release-info"
                value={groupFormData.groupKey}
              />
              {editingGroupKey && (
                <p className="text-xs text-muted-foreground">
                  Key cannot be changed after creation
                </p>
              )}
            </div>
            <div className="space-y-2">
              <Label htmlFor="layoutType">Layout Type</Label>
              <Select
                onValueChange={(value) => {
                  setGroupFormData({
                    ...groupFormData,
                    layoutType: value as DetailFieldGroupLayoutType,
                  })
                }}
                value={groupFormData.layoutType}
              >
                <SelectTrigger id="layoutType">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {layoutTypeOptions.map((option) => (
                    <SelectItem key={option.value} value={option.value}>
                      {option.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="flex items-center space-x-2">
              <Switch
                checked={groupFormData.isCollapsible}
                id="isCollapsible"
                onCheckedChange={(checked) => {
                  setGroupFormData({ ...groupFormData, isCollapsible: checked })
                }}
              />
              <Label htmlFor="isCollapsible">Collapsible</Label>
            </div>
          </div>
          <DialogFooter>
            <Button
              onClick={() => {
                setGroupDialogOpen(false)
              }}
              variant="outline"
            >
              Cancel
            </Button>
            <Button
              disabled={!groupFormData.label || !groupFormData.groupKey}
              onClick={handleSubmitGroup}
            >
              {editingGroupKey ? 'Update' : 'Create'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </SettingsPageContainer>
  )
}

function SortableLayoutItem({
  enabled,
  enabledFields,
  item,
  onDeleteGroup,
  onEditGroup,
  onMoveFieldToGroup,
  onToggleExpanded,
  onToggleField,
}: SortableLayoutItemProps) {
  const itemId =
    item.type === 'field'
      ? `field-${item.field.key}`
      : `group-${item.group.groupKey}`

  const {
    attributes,
    isDragging,
    listeners,
    setNodeRef,
    transform,
    transition,
  } = useSortable({ id: itemId })

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
  }

  if (item.type === 'field') {
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
        <div className="flex-1">
          <p className="font-medium">{item.field.label}</p>
          <p className="text-xs text-muted-foreground">
            {item.field.isCustom ? 'Custom field' : item.field.fieldType}
          </p>
        </div>
        <Switch
          checked={enabled}
          onCheckedChange={() => {
            onToggleField(item.field.key)
          }}
        />
      </div>
    )
  }

  // Group item
  return (
    <div ref={setNodeRef} style={style}>
      <div
        className={cn(
          `
            flex items-center gap-3 rounded-lg border border-primary/30
            bg-primary/5 p-3 transition-colors
          `,
          isDragging && 'z-50 border-primary/60 shadow-lg',
        )}
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
        <Button
          onClick={() => {
            onToggleExpanded(item.group.groupKey)
          }}
          size="sm"
          variant="ghost"
        >
          <ChevronRight
            className={`
              size-4 transition-transform
              ${item.expanded ? 'rotate-90' : ''}
            `}
          />
        </Button>
        <div className="flex-1">
          <p className="font-medium">{item.group.label}</p>
          <p className="text-xs text-muted-foreground">
            {item.fields.length} fields •{' '}
            {
              layoutTypeOptions.find((o) => o.value === item.group.layoutType)
                ?.label
            }
            {item.group.isCollapsible && ' • Collapsible'}
          </p>
        </div>
        <div className="flex gap-1">
          <Button
            onClick={() => {
              onEditGroup(item.group)
            }}
            size="sm"
            variant="ghost"
          >
            Edit
          </Button>
          <Button
            onClick={() => {
              onDeleteGroup(item.group.groupKey)
            }}
            size="sm"
            variant="ghost"
          >
            <Trash2 className="size-4 text-destructive" />
          </Button>
        </div>
      </div>

      {item.expanded && (
        <div className="mt-2 ml-8 space-y-2">
          {item.fields.map((field) => {
            const isEnabled = enabledFields.some((f) => f.key === field.key)
            return (
              <div
                className={cn(
                  `flex items-center gap-2 rounded-lg border bg-card p-2`,
                  !isEnabled && 'opacity-50',
                )}
                key={field.key}
              >
                <div className="flex-1">
                  <p className="text-sm font-medium">{field.label}</p>
                </div>
                <Button
                  onClick={() => {
                    onMoveFieldToGroup(field.key, null)
                  }}
                  size="sm"
                  variant="ghost"
                >
                  Remove
                </Button>
                <Switch
                  checked={isEnabled}
                  onCheckedChange={() => {
                    onToggleField(field.key)
                  }}
                />
              </div>
            )
          })}
          {item.fields.length === 0 && (
            <p className="text-sm text-muted-foreground">
              No fields in this group. Drag fields here or use the dropdown on
              ungrouped fields.
            </p>
          )}
        </div>
      )}
    </div>
  )
}

export default DetailFieldLayoutPage
