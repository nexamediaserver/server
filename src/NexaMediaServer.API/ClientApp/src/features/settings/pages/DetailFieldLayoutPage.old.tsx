import { useMutation, useQuery } from '@apollo/client/react'
import { ChevronRight, GripVertical, Plus, Trash2 } from 'lucide-react'
import { useMemo, useState } from 'react'

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
        // Load groups from config
        if (data.adminDetailFieldConfiguration?.fieldGroups) {
          setGroups(
            data.adminDetailFieldConfiguration.fieldGroups.map((g) => ({
              groupKey: g.groupKey,
              isCollapsible: g.isCollapsible,
              label: g.label,
              layoutType: g.layoutType,
              sortOrder: g.sortOrder,
            })),
          )
        } else {
          setGroups([])
        }

        // Load field group assignments from config
        if (data.adminDetailFieldConfiguration?.fieldGroupAssignments) {
          const assignments: Record<string, string> = {}
          data.adminDetailFieldConfiguration.fieldGroupAssignments.forEach(
            (pair) => {
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

  // Adapter function to convert complex backend config to simple enabled/disabled arrays
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

  const allFieldsList = useMemo(
    () => [...(enabledFields ?? []), ...(disabledFields ?? [])],
    [enabledFields, disabledFields],
  )

  const handleFieldOrderChange = (newOrder: FieldDescriptor[]) => {
    // Preserve enabled/disabled state during reorder
    const enabledSet = new Set((enabledFields ?? []).map((field) => field.key))
    const newEnabled = newOrder.filter((field) => enabledSet.has(field.key))
    const newDisabled = newOrder.filter((field) => !enabledSet.has(field.key))
    setEnabledFields(newEnabled)
    setDisabledFields(newDisabled)
  }

  const toggleField = (field: FieldDescriptor, isEnabled: boolean) => {
    if (isEnabled) {
      // Enable the field
      setEnabledFields((current) => (current ? [...current, field] : [field]))
      setDisabledFields(
        (current) => current?.filter((f) => f.key !== field.key) ?? [],
      )
    } else {
      // Disable the field
      setDisabledFields((current) => (current ? [...current, field] : [field]))
      setEnabledFields(
        (current) => current?.filter((f) => f.key !== field.key) ?? [],
      )
    }
  }

  const isFieldEnabled = (key: string) =>
    enabledFields?.some((f) => f.key === key) ?? false

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
      <Button disabled={!ready || saving} onClick={handleSave}>
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
        <div className="grid gap-6">
          <Card>
            <CardHeader>
              <CardTitle>Field Groups</CardTitle>
            </CardHeader>
            <CardContent>
              <FieldGroupManager groups={groups} onGroupsChange={setGroups} />
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Fields</CardTitle>
            </CardHeader>
            <CardContent>
              {enabledFields === null && (
                <p className="text-sm text-muted-foreground">Loading…</p>
              )}
              {enabledFields !== null && allFieldsList.length === 0 && (
                <p className="text-sm text-muted-foreground">
                  No fields available
                </p>
              )}
              {enabledFields !== null && allFieldsList.length > 0 && (
                <SortableList
                  getEnabled={(field) => isFieldEnabled(field.key)}
                  getId={(field) => field.key}
                  items={allFieldsList}
                  onOrderChange={handleFieldOrderChange}
                  onToggle={(field, isEnabled) => {
                    toggleField(field, isEnabled)
                  }}
                  renderItem={(field) => (
                    <div className="flex-1 space-y-1">
                      <p className="font-medium">{field.label}</p>
                      <p
                        className={`
                          text-xs tracking-wide text-muted-foreground uppercase
                        `}
                      >
                        {field.isCustom ? 'Custom field' : field.fieldType}
                      </p>
                      {groups.length > 0 && (
                        <Select
                          onValueChange={(value) => {
                            const newAssignments = { ...fieldGroupAssignments }
                            if (value === '__none__') {
                              delete newAssignments[field.key]
                            } else {
                              newAssignments[field.key] = value
                            }
                            setFieldGroupAssignments(newAssignments)
                          }}
                          value={fieldGroupAssignments[field.key] ?? '__none__'}
                        >
                          <SelectTrigger className="h-7 text-xs">
                            <SelectValue placeholder="No group" />
                          </SelectTrigger>
                          <SelectContent>
                            <SelectItem value="__none__">No group</SelectItem>
                            {groups.map((group) => (
                              <SelectItem
                                key={group.groupKey}
                                value={group.groupKey}
                              >
                                {group.label}
                              </SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
                      )}
                    </div>
                  )}
                />
              )}
            </CardContent>
          </Card>
        </div>
      )}
    </SettingsPageContainer>
  )
}

export default DetailFieldLayoutPage
