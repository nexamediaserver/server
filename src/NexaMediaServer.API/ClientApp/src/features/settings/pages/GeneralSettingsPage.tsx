import { useMutation, useQuery } from '@apollo/client/react'
import { useForm } from '@tanstack/react-form'
import { useEffect } from 'react'
import { toast } from 'sonner'

import {
  serverSettingsDocument,
  updateServerSettingsDocument,
} from '@/app/graphql/server-settings'
import { Button } from '@/shared/components/ui/button'
import { Input } from '@/shared/components/ui/input'
import { Label } from '@/shared/components/ui/label'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/shared/components/ui/select'
import { Skeleton } from '@/shared/components/ui/skeleton'
import { Switch } from '@/shared/components/ui/switch'

import {
  RestartServerButton,
  SettingsPageContainer,
  SettingsPageHeader,
} from '../components'

const LOG_LEVELS = [
  { label: 'Verbose', value: 'Verbose' },
  { label: 'Debug', value: 'Debug' },
  { label: 'Information', value: 'Information' },
  { label: 'Warning', value: 'Warning' },
  { label: 'Error', value: 'Error' },
  { label: 'Fatal', value: 'Fatal' },
]

export function GeneralSettingsPage() {
  const { data, loading } = useQuery(serverSettingsDocument)
  const [updateSettings, { loading: updating }] = useMutation(
    updateServerSettingsDocument,
    {
      onCompleted: () => {
        toast.success('Settings saved successfully')
      },
      onError: (error) => {
        toast.error(`Failed to save settings: ${error.message}`)
      },
      refetchQueries: [serverSettingsDocument],
    },
  )

  const form = useForm({
    defaultValues: {
      logLevel: 'Information',
      serverName: '',
    },
    onSubmit: async ({ value }) => {
      await updateSettings({
        variables: {
          input: {
            logLevel: value.logLevel,
            serverName: value.serverName,
          },
        },
      })
    },
  })

  // Update form when data loads
  useEffect(() => {
    if (data?.serverSettings) {
      form.setFieldValue('serverName', data.serverSettings.serverName)
      form.setFieldValue(
        'logLevel',
        data.serverSettings.logLevel ?? 'Information',
      )
    }
  }, [data, form])

  if (loading) {
    return (
      <SettingsPageContainer maxWidth="sm">
        <SettingsPageHeader
          description="Configure general server options"
          title="General Settings"
        />
        <div className="space-y-4">
          <Skeleton className="h-10 w-full" />
        </div>
      </SettingsPageContainer>
    )
  }

  return (
    <SettingsPageContainer maxWidth="sm">
      <SettingsPageHeader
        description="Configure general server options"
        title="General Settings"
      />

      <form
        className="space-y-6"
        onSubmit={(e) => {
          e.preventDefault()
          e.stopPropagation()
          void form.handleSubmit()
        }}
      >
        <form.Field name="serverName">
          {(field) => (
            <div className="space-y-2">
              <Label htmlFor={field.name}>Server Name</Label>
              <Input
                id={field.name}
                onBlur={field.handleBlur}
                onChange={(e) => {
                  field.handleChange(e.target.value)
                }}
                placeholder="Nexa Media Server"
                value={field.state.value}
              />
              <p className="text-sm text-muted-foreground">
                The display name for your server. This will be shown to users
                when connecting.
              </p>
            </div>
          )}
        </form.Field>

        <form.Field name="logLevel">
          {(field) => (
            <div className="space-y-2">
              <Label htmlFor={field.name}>Logging Level</Label>
              <Select
                onValueChange={field.handleChange}
                value={field.state.value}
              >
                <SelectTrigger id={field.name}>
                  <SelectValue placeholder="Select log level" />
                </SelectTrigger>
                <SelectContent>
                  {LOG_LEVELS.map((level) => (
                    <SelectItem key={level.value} value={level.value}>
                      {level.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <p className="text-sm text-muted-foreground">
                Control the verbosity of server logs. Changes apply immediately
                without restart.
              </p>
            </div>
          )}
        </form.Field>

        <Button disabled={updating} type="submit">
          {updating ? 'Saving...' : 'Save Changes'}
        </Button>

        <div className="border-t pt-4">
          <div className="flex items-center justify-between">
            <div>
              <h3 className="font-medium">Server Control</h3>
              <p className="mt-1 text-sm text-muted-foreground">
                Apply changes that require a server restart
              </p>
            </div>
            <RestartServerButton />
          </div>
        </div>
      </form>
    </SettingsPageContainer>
  )
}
