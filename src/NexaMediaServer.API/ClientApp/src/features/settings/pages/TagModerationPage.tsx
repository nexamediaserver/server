import { useMutation, useQuery } from '@apollo/client/react'
import { useForm } from '@tanstack/react-form'
import { Plus, Trash2 } from 'lucide-react'
import { useEffect, useState } from 'react'
import { toast } from 'sonner'

import {
  serverSettingsDocument,
  updateServerSettingsDocument,
} from '@/app/graphql/server-settings'
import { Button } from '@/shared/components/ui/button'
import { Input } from '@/shared/components/ui/input'
import { Label } from '@/shared/components/ui/label'
import { Skeleton } from '@/shared/components/ui/skeleton'

import { SettingsPageContainer, SettingsPageHeader } from '../components'

export function TagModerationPage() {
  const { data, loading } = useQuery(serverSettingsDocument)
  const [newAllowedTag, setNewAllowedTag] = useState('')
  const [newBlockedTag, setNewBlockedTag] = useState('')

  const [updateSettings, { loading: updating }] = useMutation(
    updateServerSettingsDocument,
    {
      onCompleted: () => {
        toast.success('Tag moderation settings saved successfully')
      },
      onError: (error) => {
        toast.error(`Failed to save settings: ${error.message}`)
      },
      refetchQueries: [serverSettingsDocument],
    },
  )

  const form = useForm({
    defaultValues: {
      allowedTags: [] as string[],
      blockedTags: [] as string[],
    },
    onSubmit: async ({ value }) => {
      await updateSettings({
        variables: {
          input: {
            allowedTags: value.allowedTags,
            blockedTags: value.blockedTags,
          },
        },
      })
    },
  })

  // Update form when data loads
  useEffect(() => {
    if (data?.serverSettings) {
      form.setFieldValue('allowedTags', data.serverSettings.allowedTags ?? [])
      form.setFieldValue('blockedTags', data.serverSettings.blockedTags ?? [])
    }
  }, [data, form])

  const handleAddAllowedTag = () => {
    const tag = newAllowedTag.trim()
    if (tag) {
      const currentTags = form.getFieldValue('allowedTags')
      if (!currentTags.includes(tag)) {
        form.setFieldValue('allowedTags', [...currentTags, tag])
        setNewAllowedTag('')
      } else {
        toast.error('Tag already exists in allowed list')
      }
    }
  }

  const handleRemoveAllowedTag = (tag: string) => {
    const currentTags = form.getFieldValue('allowedTags')
    form.setFieldValue(
      'allowedTags',
      currentTags.filter((t) => t !== tag),
    )
  }

  const handleAddBlockedTag = () => {
    const tag = newBlockedTag.trim()
    if (tag) {
      const currentTags = form.getFieldValue('blockedTags')
      if (!currentTags.includes(tag)) {
        form.setFieldValue('blockedTags', [...currentTags, tag])
        setNewBlockedTag('')
      } else {
        toast.error('Tag already exists in blocked list')
      }
    }
  }

  const handleRemoveBlockedTag = (tag: string) => {
    const currentTags = form.getFieldValue('blockedTags')
    form.setFieldValue(
      'blockedTags',
      currentTags.filter((t) => t !== tag),
    )
  }

  if (loading) {
    return (
      <SettingsPageContainer maxWidth="md">
        <SettingsPageHeader
          description="Configure allowed and blocked tags for metadata"
          title="Tag Moderation"
        />
        <div className="space-y-4">
          <Skeleton className="h-40 w-full" />
          <Skeleton className="h-40 w-full" />
        </div>
      </SettingsPageContainer>
    )
  }

  return (
    <SettingsPageContainer maxWidth="md">
      <SettingsPageHeader
        description="Configure allowed and blocked tags for metadata. If allowed tags are configured, only those tags will be used. Otherwise, blocked tags will be filtered out."
        title="Tag Moderation"
      />

      <form
        className="space-y-8"
        onSubmit={(e) => {
          e.preventDefault()
          e.stopPropagation()
          void form.handleSubmit()
        }}
      >
        {/* Allowed Tags */}
        <form.Field name="allowedTags">
          {(field) => (
            <div className="space-y-4">
              <div>
                <Label>Allowed Tags (Allowlist)</Label>
                <p className="mt-1 text-sm text-muted-foreground">
                  When configured, only these tags will be used. This takes
                  precedence over the blocked list.
                </p>
              </div>

              <div className="flex gap-2">
                <Input
                  onChange={(e) => {
                    setNewAllowedTag(e.target.value)
                  }}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter') {
                      e.preventDefault()
                      handleAddAllowedTag()
                    }
                  }}
                  placeholder="Enter tag name..."
                  value={newAllowedTag}
                />
                <Button
                  onClick={handleAddAllowedTag}
                  type="button"
                  variant="outline"
                >
                  <Plus className="mr-2 h-4 w-4" />
                  Add
                </Button>
              </div>

              {field.state.value.length > 0 ? (
                <div className="divide-y rounded-md border">
                  {field.state.value.map((tag) => (
                    <div
                      className="flex items-center justify-between px-4 py-3"
                      key={tag}
                    >
                      <span className="text-sm">{tag}</span>
                      <Button
                        onClick={() => {
                          handleRemoveAllowedTag(tag)
                        }}
                        size="sm"
                        type="button"
                        variant="ghost"
                      >
                        <Trash2 className="h-4 w-4 text-destructive" />
                      </Button>
                    </div>
                  ))}
                </div>
              ) : (
                <p className="text-sm text-muted-foreground italic">
                  No allowed tags configured. All tags will be allowed unless
                  blocked.
                </p>
              )}
            </div>
          )}
        </form.Field>

        {/* Blocked Tags */}
        <form.Field name="blockedTags">
          {(field) => (
            <div className="space-y-4">
              <div>
                <Label>Blocked Tags (Blocklist)</Label>
                <p className="mt-1 text-sm text-muted-foreground">
                  These tags will be filtered out from metadata. Only applies if
                  no allowed tags are configured.
                </p>
              </div>

              <div className="flex gap-2">
                <Input
                  onChange={(e) => {
                    setNewBlockedTag(e.target.value)
                  }}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter') {
                      e.preventDefault()
                      handleAddBlockedTag()
                    }
                  }}
                  placeholder="Enter tag name..."
                  value={newBlockedTag}
                />
                <Button
                  onClick={handleAddBlockedTag}
                  type="button"
                  variant="outline"
                >
                  <Plus className="mr-2 h-4 w-4" />
                  Add
                </Button>
              </div>

              {field.state.value.length > 0 ? (
                <div className="divide-y rounded-md border">
                  {field.state.value.map((tag) => (
                    <div
                      className="flex items-center justify-between px-4 py-3"
                      key={tag}
                    >
                      <span className="text-sm">{tag}</span>
                      <Button
                        onClick={() => {
                          handleRemoveBlockedTag(tag)
                        }}
                        size="sm"
                        type="button"
                        variant="ghost"
                      >
                        <Trash2 className="h-4 w-4 text-destructive" />
                      </Button>
                    </div>
                  ))}
                </div>
              ) : (
                <p className="text-sm text-muted-foreground italic">
                  No blocked tags configured.
                </p>
              )}
            </div>
          )}
        </form.Field>

        <Button disabled={updating} type="submit">
          {updating ? 'Saving...' : 'Save Changes'}
        </Button>
      </form>
    </SettingsPageContainer>
  )
}
