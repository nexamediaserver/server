import { useMutation, useQuery } from '@apollo/client/react'
import { useForm } from '@tanstack/react-form'
import { Download, Plus, Trash2, Upload } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
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

interface GenreMapping {
  from: string
  to: string
}

export function GenreMappingsPage() {
  const { data, loading } = useQuery(serverSettingsDocument)
  const [newFrom, setNewFrom] = useState('')
  const [newTo, setNewTo] = useState('')
  const fileInputRef = useRef<HTMLInputElement>(null)

  const [updateSettings, { loading: updating }] = useMutation(
    updateServerSettingsDocument,
    {
      onCompleted: () => {
        toast.success('Genre mappings saved successfully')
      },
      onError: (error) => {
        toast.error(`Failed to save settings: ${error.message}`)
      },
      refetchQueries: [serverSettingsDocument],
    },
  )

  const form = useForm({
    defaultValues: {
      genreMappings: {} as Record<string, string>,
    },
    onSubmit: async ({ value }) => {
      await updateSettings({
        variables: {
          input: {
            genreMappings: value.genreMappings,
          },
        },
      })
    },
  })

  // Update form when data loads
  useEffect(() => {
    if (data?.serverSettings) {
      // Convert KeyValuePair array to object
      const mappingsObject = (data.serverSettings.genreMappings ?? []).reduce<
        Record<string, string>
      >((acc, pair) => {
        acc[pair.key] = pair.value
        return acc
      }, {})
      form.setFieldValue('genreMappings', mappingsObject)
    }
  }, [data, form])

  const mappings: GenreMapping[] = Object.entries(
    form.getFieldValue('genreMappings'),
  ).map(([from, to]) => ({ from, to }))

  const handleAddMapping = () => {
    const fromValue = newFrom.trim()
    const toValue = newTo.trim()

    if (!fromValue || !toValue) {
      toast.error('Both "From" and "To" values are required')
      return
    }

    const currentMappings = form.getFieldValue('genreMappings')

    if (currentMappings[fromValue]) {
      toast.error(`Mapping for "${fromValue}" already exists`)
      return
    }

    form.setFieldValue('genreMappings', {
      ...currentMappings,
      [fromValue]: toValue,
    })

    setNewFrom('')
    setNewTo('')
  }

  const handleRemoveMapping = (from: string) => {
    const currentMappings = form.getFieldValue('genreMappings')
    const { [from]: _, ...rest } = currentMappings
    form.setFieldValue('genreMappings', rest)
  }

  const handleUpdateMapping = (from: string, newTo: string) => {
    const currentMappings = form.getFieldValue('genreMappings')
    form.setFieldValue('genreMappings', {
      ...currentMappings,
      [from]: newTo,
    })
  }

  const handleExportJson = () => {
    const mappings = form.getFieldValue('genreMappings')
    const json = JSON.stringify(mappings, null, 2)
    const blob = new Blob([json], { type: 'application/json' })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = 'genre-mappings.json'
    document.body.appendChild(a)
    a.click()
    document.body.removeChild(a)
    URL.revokeObjectURL(url)
    toast.success('Genre mappings exported')
  }

  const handleImportJson = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file) return

    const reader = new FileReader()
    reader.onload = (event) => {
      try {
        const json = JSON.parse(event.target?.result as string)

        if (typeof json !== 'object' || json === null) {
          throw new Error('Invalid JSON format')
        }

        // Validate all values are strings
        for (const [key, value] of Object.entries(json)) {
          if (typeof key !== 'string' || typeof value !== 'string') {
            throw new Error('All keys and values must be strings')
          }
        }

        form.setFieldValue('genreMappings', json)
        toast.success('Genre mappings imported successfully')
      } catch (error) {
        toast.error(
          `Failed to import mappings: ${error instanceof Error ? error.message : 'Invalid JSON'}`,
        )
      }
    }
    reader.readAsText(file)

    // Reset file input
    if (fileInputRef.current) {
      fileInputRef.current.value = ''
    }
  }

  if (loading) {
    return (
      <SettingsPageContainer maxWidth="lg">
        <SettingsPageHeader
          description="Configure genre name normalization mappings"
          title="Genre Mappings"
        />
        <div className="space-y-4">
          <Skeleton className="h-40 w-full" />
        </div>
      </SettingsPageContainer>
    )
  }

  return (
    <SettingsPageContainer maxWidth="lg">
      <SettingsPageHeader
        description="Map genre names to normalized values. For example, 'Sci-Fi' → 'Science Fiction'."
        title="Genre Mappings"
      />

      <form
        className="space-y-6"
        onSubmit={(e) => {
          e.preventDefault()
          e.stopPropagation()
          void form.handleSubmit()
        }}
      >
        {/* Import/Export Buttons */}
        <div className="flex gap-2">
          <Button onClick={handleExportJson} type="button" variant="outline">
            <Download className="mr-2 h-4 w-4" />
            Export JSON
          </Button>
          <Button
            onClick={() => {
              fileInputRef.current?.click()
            }}
            type="button"
            variant="outline"
          >
            <Upload className="mr-2 h-4 w-4" />
            Import JSON
          </Button>
          <input
            accept=".json"
            className="hidden"
            onChange={handleImportJson}
            ref={fileInputRef}
            type="file"
          />
        </div>

        {/* Add New Mapping */}
        <form.Field name="genreMappings">
          {() => (
            <div className="space-y-4">
              <div>
                <Label>Add Genre Mapping</Label>
                <p className="mt-1 text-sm text-muted-foreground">
                  Map original genre names to normalized values.
                </p>
              </div>

              <div className="flex gap-2">
                <div className="flex-1">
                  <Input
                    onChange={(e) => {
                      setNewFrom(e.target.value)
                    }}
                    onKeyDown={(e) => {
                      if (e.key === 'Enter') {
                        e.preventDefault()
                        handleAddMapping()
                      }
                    }}
                    placeholder="From (e.g., Sci-Fi)"
                    value={newFrom}
                  />
                </div>
                <span className="flex items-center text-muted-foreground">
                  →
                </span>
                <div className="flex-1">
                  <Input
                    onChange={(e) => {
                      setNewTo(e.target.value)
                    }}
                    onKeyDown={(e) => {
                      if (e.key === 'Enter') {
                        e.preventDefault()
                        handleAddMapping()
                      }
                    }}
                    placeholder="To (e.g., Science Fiction)"
                    value={newTo}
                  />
                </div>
                <Button
                  onClick={handleAddMapping}
                  type="button"
                  variant="outline"
                >
                  <Plus className="mr-2 h-4 w-4" />
                  Add
                </Button>
              </div>

              {/* Mappings Table */}
              {mappings.length > 0 ? (
                <div className="rounded-md border">
                  <div
                    className={`
                      grid grid-cols-[1fr,auto,1fr,auto] gap-4 border-b
                      bg-muted/50 px-4 py-3 text-sm font-medium
                    `}
                  >
                    <div>From Genre</div>
                    <div />
                    <div>To Genre</div>
                    <div />
                  </div>
                  <div className="divide-y">
                    {mappings.map((mapping) => (
                      <div
                        className={`
                          grid grid-cols-[1fr,auto,1fr,auto] items-center gap-4
                          px-4 py-3
                        `}
                        key={mapping.from}
                      >
                        <div className="font-mono text-sm">{mapping.from}</div>
                        <span className="text-muted-foreground">→</span>
                        <Input
                          className="h-8"
                          onChange={(e) => {
                            handleUpdateMapping(mapping.from, e.target.value)
                          }}
                          value={mapping.to}
                        />
                        <Button
                          onClick={() => {
                            handleRemoveMapping(mapping.from)
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
                </div>
              ) : (
                <p className="text-sm text-muted-foreground italic">
                  No genre mappings configured.
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
