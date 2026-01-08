import { Plus, Trash2 } from 'lucide-react'
import { useState } from 'react'

import {
  DetailFieldGroupInput,
  DetailFieldGroupLayoutType,
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

interface FieldGroup {
  groupKey: string
  isCollapsible: boolean
  label: string
  layoutType: DetailFieldGroupLayoutType
  sortOrder: number
}

interface FieldGroupManagerProps {
  groups: FieldGroup[]
  onGroupsChange: (groups: FieldGroup[]) => void
}

const layoutTypeOptions = [
  { label: 'Vertical (Stack)', value: DetailFieldGroupLayoutType.Vertical },
  { label: 'Horizontal (Row)', value: DetailFieldGroupLayoutType.Horizontal },
  { label: 'Grid', value: DetailFieldGroupLayoutType.Grid },
]

export function FieldGroupManager({
  groups,
  onGroupsChange,
}: FieldGroupManagerProps) {
  const [dialogOpen, setDialogOpen] = useState(false)
  const [editingKey, setEditingKey] = useState<null | string>(null)
  const [formData, setFormData] = useState<FieldGroup>({
    groupKey: '',
    isCollapsible: false,
    label: '',
    layoutType: DetailFieldGroupLayoutType.Horizontal,
    sortOrder: groups.length,
  })

  const handleOpenCreate = () => {
    setEditingKey(null)
    setFormData({
      groupKey: '',
      isCollapsible: false,
      label: '',
      layoutType: DetailFieldGroupLayoutType.Horizontal,
      sortOrder: groups.length,
    })
    setDialogOpen(true)
  }

  const handleOpenEdit = (group: FieldGroup) => {
    setEditingKey(group.groupKey)
    setFormData({ ...group })
    setDialogOpen(true)
  }

  const handleSubmit = () => {
    if (editingKey) {
      // Update existing group
      onGroupsChange(
        groups.map((g) => (g.groupKey === editingKey ? formData : g)),
      )
    } else {
      // Create new group
      onGroupsChange([...groups, formData])
    }
    setDialogOpen(false)
  }

  const handleDelete = (groupKey: string) => {
    if (confirm('Are you sure you want to delete this group?')) {
      onGroupsChange(groups.filter((g) => g.groupKey !== groupKey))
    }
  }

  const handleMoveUp = (index: number) => {
    if (index === 0) return
    const newGroups = [...groups]
    ;[newGroups[index - 1], newGroups[index]] = [
      newGroups[index],
      newGroups[index - 1],
    ]
    // Update sort orders
    newGroups.forEach((g, i) => {
      g.sortOrder = i
    })
    onGroupsChange(newGroups)
  }

  const handleMoveDown = (index: number) => {
    if (index === groups.length - 1) return
    const newGroups = [...groups]
    ;[newGroups[index], newGroups[index + 1]] = [
      newGroups[index + 1],
      newGroups[index],
    ]
    // Update sort orders
    newGroups.forEach((g, i) => {
      g.sortOrder = i
    })
    onGroupsChange(newGroups)
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-medium">Field Groups</h3>
        <Button onClick={handleOpenCreate} size="sm" variant="outline">
          <Plus className="mr-2 size-4" />
          Add Group
        </Button>
      </div>

      {groups.length === 0 && (
        <p className="text-sm text-muted-foreground">
          No groups defined. Fields will render in default vertical layout.
        </p>
      )}

      <div className="space-y-2">
        {groups.map((group, index) => (
          <div
            className="flex items-center gap-2 rounded-lg border bg-card p-3"
            key={group.groupKey}
          >
            <div className="flex-1">
              <p className="font-medium">{group.label}</p>
              <p className="text-xs text-muted-foreground">
                Key: {group.groupKey} •{' '}
                {layoutTypeOptions.find((o) => o.value === group.layoutType)
                  ?.label ?? group.layoutType}
                {group.isCollapsible && ' • Collapsible'}
              </p>
            </div>
            <div className="flex gap-1">
              <Button
                disabled={index === 0}
                onClick={() => {
                  handleMoveUp(index)
                }}
                size="sm"
                variant="ghost"
              >
                ↑
              </Button>
              <Button
                disabled={index === groups.length - 1}
                onClick={() => {
                  handleMoveDown(index)
                }}
                size="sm"
                variant="ghost"
              >
                ↓
              </Button>
              <Button
                onClick={() => {
                  handleOpenEdit(group)
                }}
                size="sm"
                variant="ghost"
              >
                Edit
              </Button>
              <Button
                onClick={() => {
                  handleDelete(group.groupKey)
                }}
                size="sm"
                variant="ghost"
              >
                <Trash2 className="size-4 text-destructive" />
              </Button>
            </div>
          </div>
        ))}
      </div>

      <Dialog onOpenChange={setDialogOpen} open={dialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>
              {editingKey ? 'Edit Group' : 'Create Group'}
            </DialogTitle>
          </DialogHeader>

          <div className="space-y-4">
            <div>
              <Label htmlFor="groupKey">Group Key</Label>
              <Input
                disabled={!!editingKey}
                id="groupKey"
                onChange={(e) => {
                  setFormData({ ...formData, groupKey: e.target.value })
                }}
                placeholder="e.g., metadata-row"
                value={formData.groupKey}
              />
              <p className="mt-1 text-xs text-muted-foreground">
                Unique identifier for this group (cannot be changed after
                creation)
              </p>
            </div>

            <div>
              <Label htmlFor="label">Display Label</Label>
              <Input
                id="label"
                onChange={(e) => {
                  setFormData({ ...formData, label: e.target.value })
                }}
                placeholder="e.g., Release Info"
                value={formData.label}
              />
            </div>

            <div>
              <Label htmlFor="layoutType">Layout Type</Label>
              <Select
                onValueChange={(value) => {
                  setFormData({
                    ...formData,
                    layoutType: value as DetailFieldGroupLayoutType,
                  })
                }}
                value={formData.layoutType}
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
              <p className="mt-1 text-xs text-muted-foreground">
                How fields in this group should be arranged
              </p>
            </div>

            <div className="flex items-center space-x-2">
              <Switch
                checked={formData.isCollapsible}
                id="isCollapsible"
                onCheckedChange={(checked) => {
                  setFormData({ ...formData, isCollapsible: checked })
                }}
              />
              <Label className="cursor-pointer" htmlFor="isCollapsible">
                Allow users to collapse this group
              </Label>
            </div>
          </div>

          <DialogFooter>
            <Button
              onClick={() => {
                setDialogOpen(false)
              }}
              variant="outline"
            >
              Cancel
            </Button>
            <Button
              disabled={!formData.groupKey || !formData.label}
              onClick={handleSubmit}
            >
              {editingKey ? 'Update' : 'Create'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
