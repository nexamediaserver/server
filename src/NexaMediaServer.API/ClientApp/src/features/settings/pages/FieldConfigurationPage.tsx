import type { ReactNode } from 'react'

import { useMutation, useQuery } from '@apollo/client/react'
import { Trash2 } from 'lucide-react'
import { useState } from 'react'
import IconAdd from '~icons/material-symbols/add'
import IconEdit from '~icons/material-symbols/edit'

import {
  CreateCustomFieldDefinitionMutation,
  CustomFieldDefinitionsQuery,
  DeleteCustomFieldDefinitionMutation,
  UpdateCustomFieldDefinitionMutation,
} from '@/features/metadata/queries'
import {
  DetailFieldWidgetType,
  MetadataType,
} from '@/shared/api/graphql/graphql'
import { Button } from '@/shared/components/ui/button'
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

const widgetOptions = [
  { label: 'Text', value: DetailFieldWidgetType.Text },
  { label: 'Number', value: DetailFieldWidgetType.Number },
  { label: 'Boolean', value: DetailFieldWidgetType.Boolean },
  { label: 'Date', value: DetailFieldWidgetType.Date },
  { label: 'Link', value: DetailFieldWidgetType.Link },
  { label: 'List', value: DetailFieldWidgetType.List },
  { label: 'Badge', value: DetailFieldWidgetType.Badge },
]

const metadataTypeOptions = [
  { label: 'Movie', value: MetadataType.Movie },
  { label: 'Show', value: MetadataType.Show },
  { label: 'Season', value: MetadataType.Season },
  { label: 'Episode', value: MetadataType.Episode },
  { label: 'Album', value: MetadataType.AlbumRelease },
  { label: 'Track', value: MetadataType.Track },
  { label: 'Person', value: MetadataType.Person },
]

interface CustomFieldFormData {
  applicableMetadataTypes: MetadataType[]
  isEnabled: boolean
  key: string
  label: string
  sortOrder: number
  widget: DetailFieldWidgetType
}

const defaultFormData: CustomFieldFormData = {
  applicableMetadataTypes: [],
  isEnabled: true,
  key: '',
  label: '',
  sortOrder: 100,
  widget: DetailFieldWidgetType.Text,
}

export function FieldConfigurationPage(): ReactNode {
  const [dialogOpen, setDialogOpen] = useState(false)
  const [editingId, setEditingId] = useState<null | string>(null)
  const [formData, setFormData] = useState<CustomFieldFormData>(defaultFormData)

  const { data, loading, refetch } = useQuery(CustomFieldDefinitionsQuery)

  const [createField, { loading: createLoading }] = useMutation(
    CreateCustomFieldDefinitionMutation,
    {
      onCompleted: () => {
        setDialogOpen(false)
        setFormData(defaultFormData)
        void refetch()
      },
    },
  )

  const [updateField, { loading: updateLoading }] = useMutation(
    UpdateCustomFieldDefinitionMutation,
    {
      onCompleted: () => {
        setDialogOpen(false)
        setEditingId(null)
        setFormData(defaultFormData)
        void refetch()
      },
    },
  )

  const [deleteField, { loading: deleteLoading }] = useMutation(
    DeleteCustomFieldDefinitionMutation,
    {
      onCompleted: () => {
        void refetch()
      },
    },
  )

  const handleOpenCreate = () => {
    setEditingId(null)
    setFormData(defaultFormData)
    setDialogOpen(true)
  }

  const handleOpenEdit = (field: {
    applicableMetadataTypes: MetadataType[]
    id: string
    isEnabled: boolean
    key: string
    label: string
    sortOrder: number
    widget: DetailFieldWidgetType
  }) => {
    setEditingId(field.id)
    setFormData({
      applicableMetadataTypes: [...field.applicableMetadataTypes],
      isEnabled: field.isEnabled,
      key: field.key,
      label: field.label,
      sortOrder: field.sortOrder,
      widget: field.widget,
    })
    setDialogOpen(true)
  }

  const handleSubmit = () => {
    if (editingId) {
      void updateField({
        variables: {
          input: {
            applicableMetadataTypes:
              formData.applicableMetadataTypes.length > 0
                ? formData.applicableMetadataTypes
                : null,
            id: editingId,
            isEnabled: formData.isEnabled,
            label: formData.label || null,
            sortOrder: formData.sortOrder,
            widget: formData.widget,
          },
        },
      })
    } else {
      void createField({
        variables: {
          input: {
            applicableMetadataTypes:
              formData.applicableMetadataTypes.length > 0
                ? formData.applicableMetadataTypes
                : null,
            key: formData.key,
            label: formData.label,
            sortOrder: formData.sortOrder,
            widget: formData.widget,
          },
        },
      })
    }
  }

  const handleDelete = (id: string) => {
    if (confirm('Are you sure you want to delete this custom field?')) {
      void deleteField({ variables: { id } })
    }
  }

  const handleMetadataTypeToggle = (type: MetadataType) => {
    setFormData((prev) => {
      const types = prev.applicableMetadataTypes.includes(type)
        ? prev.applicableMetadataTypes.filter((t) => t !== type)
        : [...prev.applicableMetadataTypes, type]
      return { ...prev, applicableMetadataTypes: types }
    })
  }

  const isSaving = createLoading || updateLoading

  const headerActions = (
    <Button onClick={handleOpenCreate}>
      <IconAdd className="mr-2 size-4" />
      Add Custom Field
    </Button>
  )

  return (
    <SettingsPageContainer maxWidth="full">
      <SettingsPageHeader
        actions={headerActions}
        description="Define custom fields that can be added to metadata items"
        title="Custom Fields"
      />

      {loading ? (
        <p className="text-muted-foreground">Loading...</p>
      ) : (
        <div className="rounded-lg border">
          <table className="w-full">
            <thead>
              <tr className="border-b bg-muted/50">
                <th className="px-4 py-3 text-left text-sm font-medium">Key</th>
                <th className="px-4 py-3 text-left text-sm font-medium">
                  Label
                </th>
                <th className="px-4 py-3 text-left text-sm font-medium">
                  Widget
                </th>
                <th className="px-4 py-3 text-left text-sm font-medium">
                  Applies To
                </th>
                <th className="px-4 py-3 text-left text-sm font-medium">
                  Enabled
                </th>
                <th className="px-4 py-3 text-right text-sm font-medium">
                  Actions
                </th>
              </tr>
            </thead>
            <tbody>
              {data?.customFieldDefinitions?.map((field) => (
                <tr className="border-b" key={field.id}>
                  <td className="px-4 py-3 font-mono text-sm">{field.key}</td>
                  <td className="px-4 py-3 text-sm">{field.label}</td>
                  <td className="px-4 py-3 text-sm capitalize">
                    {field.widget.toLowerCase().replace('_', ' ')}
                  </td>
                  <td className="px-4 py-3 text-sm">
                    {field.applicableMetadataTypes.length === 0
                      ? 'All types'
                      : field.applicableMetadataTypes
                          .map((t) => t.replace('_', ' '))
                          .join(', ')}
                  </td>
                  <td className="px-4 py-3">
                    <span
                      className={`
                        inline-flex rounded-full px-2 py-1 text-xs
                        ${
                          field.isEnabled
                            ? 'bg-green-500/20 text-green-400'
                            : 'bg-red-500/20 text-red-400'
                        }
                      `}
                    >
                      {field.isEnabled ? 'Yes' : 'No'}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-right">
                    <div className="flex justify-end gap-2">
                      <Button
                        onClick={() => {
                          handleOpenEdit(field)
                        }}
                        size="icon"
                        variant="ghost"
                      >
                        <IconEdit className="size-4" />
                      </Button>
                      <Button
                        disabled={deleteLoading}
                        onClick={() => {
                          handleDelete(field.id)
                        }}
                        size="icon"
                        variant="ghost"
                      >
                        <Trash2 className="size-4 text-destructive" />
                      </Button>
                    </div>
                  </td>
                </tr>
              ))}
              {(!data?.customFieldDefinitions ||
                data.customFieldDefinitions.length === 0) && (
                <tr>
                  <td
                    className="px-4 py-8 text-center text-muted-foreground"
                    colSpan={6}
                  >
                    No custom fields defined yet.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}

      <Dialog onOpenChange={setDialogOpen} open={dialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>
              {editingId ? 'Edit Custom Field' : 'Create Custom Field'}
            </DialogTitle>
          </DialogHeader>
          <div className="flex flex-col gap-4 py-4">
            <div className="flex flex-col gap-2">
              <Label htmlFor="key">Key</Label>
              <Input
                disabled={!!editingId}
                id="key"
                onChange={(e) => {
                  setFormData((prev) => ({ ...prev, key: e.target.value }))
                }}
                placeholder="custom_field_key"
                value={formData.key}
              />
              <p className="text-xs text-muted-foreground">
                Unique identifier for this field (cannot be changed after
                creation)
              </p>
            </div>
            <div className="flex flex-col gap-2">
              <Label htmlFor="label">Label</Label>
              <Input
                id="label"
                onChange={(e) => {
                  setFormData((prev) => ({ ...prev, label: e.target.value }))
                }}
                placeholder="Display Label"
                value={formData.label}
              />
            </div>
            <div className="flex flex-col gap-2">
              <Label htmlFor="widget">Widget Type</Label>
              <Select
                onValueChange={(value) => {
                  setFormData((prev) => ({
                    ...prev,
                    widget: value as DetailFieldWidgetType,
                  }))
                }}
                value={formData.widget}
              >
                <SelectTrigger id="widget">
                  <SelectValue placeholder="Select widget type" />
                </SelectTrigger>
                <SelectContent>
                  {widgetOptions.map((option) => (
                    <SelectItem key={option.value} value={option.value}>
                      {option.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="flex flex-col gap-2">
              <Label htmlFor="sortOrder">Sort Order</Label>
              <Input
                id="sortOrder"
                onChange={(e) => {
                  setFormData((prev) => ({
                    ...prev,
                    sortOrder: parseInt(e.target.value, 10) || 0,
                  }))
                }}
                type="number"
                value={formData.sortOrder}
              />
            </div>
            <div className="flex flex-col gap-2">
              <Label>Applicable Metadata Types</Label>
              <p className="text-xs text-muted-foreground">
                Leave empty to apply to all types
              </p>
              <div className="flex flex-wrap gap-2">
                {metadataTypeOptions.map((option) => (
                  <button
                    className={`
                      rounded-full border px-3 py-1 text-sm transition-colors
                      ${
                        formData.applicableMetadataTypes.includes(option.value)
                          ? 'border-primary bg-primary text-primary-foreground'
                          : `
                            border-border
                            hover:bg-muted
                          `
                      }
                    `}
                    key={option.value}
                    onClick={() => {
                      handleMetadataTypeToggle(option.value)
                    }}
                    type="button"
                  >
                    {option.label}
                  </button>
                ))}
              </div>
            </div>
            {editingId && (
              <div className="flex items-center gap-2">
                <Switch
                  checked={formData.isEnabled}
                  id="isEnabled"
                  onCheckedChange={(checked) => {
                    setFormData((prev) => ({ ...prev, isEnabled: checked }))
                  }}
                />
                <Label htmlFor="isEnabled">Enabled</Label>
              </div>
            )}
          </div>
          <DialogFooter>
            <Button
              onClick={() => {
                setDialogOpen(false)
              }}
              variant="secondary"
            >
              Cancel
            </Button>
            <Button
              disabled={
                isSaving || !formData.key.trim() || !formData.label.trim()
              }
              onClick={handleSubmit}
            >
              {isSaving ? 'Saving...' : editingId ? 'Update' : 'Create'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </SettingsPageContainer>
  )
}
