import type { ReactFormExtendedApi } from '@tanstack/react-form'

import { useLazyQuery, useMutation, useQuery } from '@apollo/client/react'
import { useForm, useStore } from '@tanstack/react-form'
import { Lock, LockOpen } from 'lucide-react'
import { type ReactNode, useCallback, useEffect, useId, useState } from 'react'

import type {
  ExternalIdInput,
  MetadataType,
} from '@/shared/api/graphql/graphql'

import {
  metadataItemForEditDocument,
  updateMetadataItemDocument,
} from '@/app/graphql/metadata-edit'
import { CustomFieldDefinitionsQuery } from '@/features/metadata/queries'
import { DetailFieldWidgetType } from '@/shared/api/graphql/graphql'
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
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
} from '@/shared/components/ui/tabs'
import { Textarea } from '@/shared/components/ui/textarea'
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from '@/shared/components/ui/tooltip'
import { cn } from '@/shared/lib/utils'

// Well-known field names matching MetadataFieldNames on the server
const MetadataFieldNames = {
  Art: 'Art',
  ContentRating: 'ContentRating',
  ExternalIdentifiers: 'ExternalIdentifiers',
  Genres: 'Genres',
  Logo: 'Logo',
  OriginalTitle: 'OriginalTitle',
  ReleaseDate: 'ReleaseDate',
  SortTitle: 'SortTitle',
  Summary: 'Summary',
  Tagline: 'Tagline',
  Tags: 'Tags',
  Thumb: 'Thumb',
  Title: 'Title',
} as const

type EditTab = 'custom-fields' | 'details' | 'external-ids' | 'general' | 'tags'

const editTabs: {
  description: string
  key: EditTab
  label: string
}[] = [
  {
    description: 'Title and basic information.',
    key: 'general',
    label: 'General',
  },
  {
    description: 'Summary, tagline, and content rating.',
    key: 'details',
    label: 'Details',
  },
  {
    description: 'Genres and custom tags.',
    key: 'tags',
    label: 'Tags & Genres',
  },
  {
    description: 'Links to TMDB, IMDb, TVDB, etc.',
    key: 'external-ids',
    label: 'External IDs',
  },
  {
    description: 'Admin-defined custom fields.',
    key: 'custom-fields',
    label: 'Custom Fields',
  },
]

interface ExternalIdDraft {
  provider: string
  value: string
}

interface ExtraFieldDraft {
  key: string
  value: unknown
}

interface FormValues {
  contentRating: string
  externalIds: ExternalIdDraft[]
  extraFields: ExtraFieldDraft[]
  genres: string[]
  lockedFields: string[]
  originalTitle: string
  releaseDate: string
  sortTitle: string
  summary: string
  tagline: string
  tags: string[]
  title: string
}

const createDefaultFormValues = (): FormValues => ({
  contentRating: '',
  externalIds: [],
  extraFields: [],
  genres: [],
  lockedFields: [],
  originalTitle: '',
  releaseDate: '',
  sortTitle: '',
  summary: '',
  tagline: '',
  tags: [],
  title: '',
})

export interface EditMetadataItemDialogProps {
  itemId: string
  onClose: () => void
  onUpdated?: () => void
  open: boolean
}

interface LockableFieldProps {
  children: ReactNode
  fieldName: string
  isLocked: boolean
  label: string
  onToggleLock: (fieldName: string) => void
}

export function EditMetadataItemDialog({
  itemId,
  onClose,
  onUpdated,
  open,
}: Readonly<EditMetadataItemDialogProps>) {
  const [tab, setTab] = useState<EditTab>('general')
  const genresId = useId()
  const tagsId = useId()
  const externalIdsId = useId()

  const [fetchItem, { loading: fetchLoading }] = useLazyQuery(
    metadataItemForEditDocument,
    {
      fetchPolicy: 'network-only',
    },
  )

  const [updateItem, { error: updateError, loading: updateLoading }] =
    useMutation(updateMetadataItemDocument)

  const form = useForm({
    defaultValues: createDefaultFormValues(),
    onSubmit: async ({ value }: { value: FormValues }) => {
      const trimmedTitle = value.title.trim()
      if (!trimmedTitle) {
        return
      }

      const externalIds: ExternalIdInput[] = value.externalIds
        .filter((ext) => ext.provider.trim())
        .map((ext) => ({
          provider: ext.provider.trim(),
          value: ext.value.trim() || null,
        }))

      // Convert extraFields to the input format
      const extraFields = value.extraFields
        .filter((field) => field.key.trim())
        .map((field) => ({
          key: field.key.trim(),
          value: field.value,
        }))

      try {
        const result = await updateItem({
          variables: {
            input: {
              contentRating: value.contentRating.trim() || null,
              externalIds: externalIds.length > 0 ? externalIds : null,
              extraFields: extraFields.length > 0 ? extraFields : null,
              genres: value.genres.length > 0 ? value.genres : null,
              itemId,
              lockedFields:
                value.lockedFields.length > 0 ? value.lockedFields : null,
              originalTitle: value.originalTitle.trim() || null,
              releaseDate: value.releaseDate || null,
              sortTitle: value.sortTitle.trim() || null,
              summary: value.summary.trim() || null,
              tagline: value.tagline.trim() || null,
              tags: value.tags.length > 0 ? value.tags : null,
              title: trimmedTitle,
            },
          },
        })

        if (result.data?.updateMetadataItem.success) {
          onUpdated?.()
          onClose()
        }
      } catch {
        // Error is displayed via updateError
      }
    },
  }) as unknown as ReactFormExtendedApi<
    FormValues,
    undefined,
    undefined,
    undefined,
    undefined,
    undefined,
    undefined,
    undefined,
    undefined,
    undefined,
    undefined,
    undefined
  >

  const formValues = useStore(form.store, (formState) => formState.values)

  // Track metadataType for filtering custom fields
  const [metadataType, setMetadataType] = useState<MetadataType | null>(null)

  // Fetch custom field definitions
  const { data: customFieldsData } = useQuery(CustomFieldDefinitionsQuery, {
    skip: !open,
  })

  // Filter custom fields applicable to this item's metadata type
  const applicableCustomFields =
    customFieldsData?.customFieldDefinitions?.filter(
      (field) =>
        field.isEnabled &&
        (field.applicableMetadataTypes.length === 0 ||
          (metadataType &&
            field.applicableMetadataTypes.includes(metadataType))),
    ) ?? []

  // Load item data when dialog opens
  useEffect(() => {
    if (open && itemId) {
      void fetchItem({ variables: { id: itemId } }).then((result) => {
        const item = result.data?.metadataItem
        if (!item) return

        setMetadataType(item.metadataType)
        form.setFieldValue('title', item.title)
        form.setFieldValue('sortTitle', item.titleSort)
        form.setFieldValue('originalTitle', item.originalTitle)
        form.setFieldValue('summary', item.summary)
        form.setFieldValue('tagline', item.tagline)
        form.setFieldValue('contentRating', item.contentRating)
        form.setFieldValue(
          'releaseDate',
          item.originallyAvailableAt ? String(item.originallyAvailableAt) : '',
        )
        form.setFieldValue('genres', [...item.genres])
        form.setFieldValue('tags', [...item.tags])
        form.setFieldValue('lockedFields', [...item.lockedFields])
        form.setFieldValue(
          'externalIds',
          item.externalIds.map((ext: { provider: string; value: string }) => ({
            provider: ext.provider,
            value: ext.value,
          })),
        )
        // Load extra fields
        form.setFieldValue(
          'extraFields',
          item.extraFields.map((field: { key: string; value: unknown }) => ({
            key: field.key,
            value: field.value,
          })),
        )
      })
    }
  }, [open, itemId, fetchItem, form])

  const handleToggleLock = useCallback(
    (fieldName: string) => {
      const currentLocked = formValues.lockedFields
      if (currentLocked.includes(fieldName)) {
        form.setFieldValue(
          'lockedFields',
          currentLocked.filter((f) => f !== fieldName),
        )
      } else {
        form.setFieldValue('lockedFields', [...currentLocked, fieldName])
      }
    },
    [form, formValues.lockedFields],
  )

  const isFieldLocked = useCallback(
    (fieldName: string) => formValues.lockedFields.includes(fieldName),
    [formValues.lockedFields],
  )

  const handleClose = () => {
    form.reset()
    setTab('general')
    setMetadataType(null)
    onClose()
  }

  const loading = fetchLoading || updateLoading

  // Genre/tag management
  const [genreDraft, setGenreDraft] = useState('')
  const [tagDraft, setTagDraft] = useState('')

  const addGenre = () => {
    const trimmed = genreDraft.trim()
    if (trimmed && !formValues.genres.includes(trimmed)) {
      form.setFieldValue('genres', [...formValues.genres, trimmed])
      setGenreDraft('')
    }
  }

  const removeGenre = (genre: string) => {
    form.setFieldValue(
      'genres',
      formValues.genres.filter((g) => g !== genre),
    )
  }

  const addTag = () => {
    const trimmed = tagDraft.trim()
    if (trimmed && !formValues.tags.includes(trimmed)) {
      form.setFieldValue('tags', [...formValues.tags, trimmed])
      setTagDraft('')
    }
  }

  const removeTag = (tag: string) => {
    form.setFieldValue(
      'tags',
      formValues.tags.filter((t) => t !== tag),
    )
  }

  // External ID management
  const [externalIdDraft, setExternalIdDraft] = useState<ExternalIdDraft>({
    provider: '',
    value: '',
  })

  const addExternalId = () => {
    const provider = externalIdDraft.provider.trim()
    if (provider) {
      // Check if provider already exists
      const existing = formValues.externalIds.find(
        (ext) => ext.provider.toLowerCase() === provider.toLowerCase(),
      )
      if (existing) {
        // Update existing
        form.setFieldValue(
          'externalIds',
          formValues.externalIds.map((ext) =>
            ext.provider.toLowerCase() === provider.toLowerCase()
              ? { provider: ext.provider, value: externalIdDraft.value.trim() }
              : ext,
          ),
        )
      } else {
        // Add new
        form.setFieldValue('externalIds', [
          ...formValues.externalIds,
          { provider, value: externalIdDraft.value.trim() },
        ])
      }
      setExternalIdDraft({ provider: '', value: '' })
    }
  }

  const removeExternalId = (provider: string) => {
    form.setFieldValue(
      'externalIds',
      formValues.externalIds.filter(
        (ext: { provider: string; value: string }) => ext.provider !== provider,
      ),
    )
  }

  return (
    <Dialog
      onOpenChange={(isOpen) => {
        if (!isOpen) handleClose()
      }}
      open={open}
    >
      <DialogContent
        className={`
          max-w-4xl gap-0
          lg:min-w-4xl
        `}
      >
        <DialogHeader className="border-b bg-stone-700">
          <DialogTitle>Edit Metadata</DialogTitle>
        </DialogHeader>
        <form
          aria-describedby={updateError ? 'edit-metadata-error' : undefined}
          noValidate
          onSubmit={(event) => {
            event.preventDefault()
            void form.handleSubmit()
          }}
        >
          {updateError && (
            <p
              className="rounded bg-red-100 px-2 py-1 text-sm text-red-700"
              id="edit-metadata-error"
              role="alert"
            >
              {updateError.message}
            </p>
          )}
          <Tabs
            className={`
              flex min-h-128 w-full flex-row items-stretch gap-0 bg-stone-800
            `}
            onValueChange={(value) => {
              setTab(value as EditTab)
            }}
            value={tab}
          >
            <div className="flex w-60 shrink-0 flex-col border-r bg-stone-900">
              <TabsList
                className={`
                  flex h-auto flex-1 flex-col justify-start rounded-none
                  bg-transparent p-0 text-left
                `}
              >
                {editTabs.map((step, index) => (
                  <TabsTrigger
                    className={`
                      flex h-auto max-h-28 w-full flex-col items-start gap-1
                      rounded-none border-b border-transparent px-4 py-4
                      text-left text-sm whitespace-normal
                      data-[state=active]:border-primary/60
                      data-[state=active]:bg-stone-800
                      data-[state=active]:text-primary
                    `}
                    key={step.key}
                    type="button"
                    value={step.key}
                  >
                    <span
                      className={`
                        text-xs tracking-wide text-stone-400 uppercase
                      `}
                    >
                      Section {index + 1}
                    </span>
                    <span className="block text-base font-semibold">
                      {step.label}
                    </span>
                    <span className="max-w-fit text-xs text-stone-400">
                      {step.description}
                    </span>
                  </TabsTrigger>
                ))}
              </TabsList>
            </div>
            <div className="flex flex-1 flex-col">
              <TabsContent
                className={`
                  flex-1 space-y-6 overflow-x-hidden overflow-y-auto p-4
                `}
                value="general"
              >
                <div className="space-y-4">
                  <div>
                    <h2 className="text-lg font-semibold">
                      General Information
                    </h2>
                    <p className="text-sm text-stone-300">
                      Basic identification details for this item.
                    </p>
                  </div>
                  <form.Field
                    name="title"
                    validators={{
                      onChange: ({ value }: { value: string }) =>
                        value.trim() ? undefined : 'Title is required',
                    }}
                  >
                    {(field) => (
                      <LockableField
                        fieldName={MetadataFieldNames.Title}
                        isLocked={isFieldLocked(MetadataFieldNames.Title)}
                        label="Title *"
                        onToggleLock={handleToggleLock}
                      >
                        <Input
                          aria-invalid={
                            field.state.meta.errors.length ? 'true' : undefined
                          }
                          onBlur={field.handleBlur}
                          onChange={(e) => {
                            field.handleChange(e.target.value)
                          }}
                          placeholder="Enter title"
                          value={field.state.value}
                        />
                        {field.state.meta.errors[0] && (
                          <p className="text-xs text-red-500" role="alert">
                            {field.state.meta.errors[0]}
                          </p>
                        )}
                      </LockableField>
                    )}
                  </form.Field>
                  <form.Field name="sortTitle">
                    {(field) => (
                      <LockableField
                        fieldName={MetadataFieldNames.SortTitle}
                        isLocked={isFieldLocked(MetadataFieldNames.SortTitle)}
                        label="Sort Title"
                        onToggleLock={handleToggleLock}
                      >
                        <Input
                          onChange={(e) => {
                            field.handleChange(e.target.value)
                          }}
                          placeholder="Defaults to title if empty"
                          value={field.state.value}
                        />
                        <p className="text-xs text-stone-400">
                          Used for alphabetical sorting (e.g., "Matrix, The").
                        </p>
                      </LockableField>
                    )}
                  </form.Field>
                  <form.Field name="originalTitle">
                    {(field) => (
                      <LockableField
                        fieldName={MetadataFieldNames.OriginalTitle}
                        isLocked={isFieldLocked(
                          MetadataFieldNames.OriginalTitle,
                        )}
                        label="Original Title"
                        onToggleLock={handleToggleLock}
                      >
                        <Input
                          onChange={(e) => {
                            field.handleChange(e.target.value)
                          }}
                          placeholder="Original language title"
                          value={field.state.value}
                        />
                      </LockableField>
                    )}
                  </form.Field>
                  <form.Field name="releaseDate">
                    {(field) => (
                      <LockableField
                        fieldName={MetadataFieldNames.ReleaseDate}
                        isLocked={isFieldLocked(MetadataFieldNames.ReleaseDate)}
                        label="Release Date & Year"
                        onToggleLock={handleToggleLock}
                      >
                        <Input
                          onChange={(e) => {
                            field.handleChange(e.target.value)
                          }}
                          placeholder="YYYY-MM-DD"
                          type="date"
                          value={field.state.value}
                        />
                      </LockableField>
                    )}
                  </form.Field>
                </div>
              </TabsContent>
              <TabsContent
                className={`
                  flex-1 space-y-6 overflow-x-hidden overflow-y-auto p-4
                `}
                value="details"
              >
                <div className="space-y-4">
                  <div>
                    <h2 className="text-lg font-semibold">Details</h2>
                    <p className="text-sm text-stone-300">
                      Descriptions and content classification.
                    </p>
                  </div>
                  <form.Field name="summary">
                    {(field) => (
                      <LockableField
                        fieldName={MetadataFieldNames.Summary}
                        isLocked={isFieldLocked(MetadataFieldNames.Summary)}
                        label="Summary"
                        onToggleLock={handleToggleLock}
                      >
                        <Textarea
                          className="min-h-32"
                          onChange={(e) => {
                            field.handleChange(e.target.value)
                          }}
                          placeholder="Enter a description or plot summary"
                          value={field.state.value}
                        />
                      </LockableField>
                    )}
                  </form.Field>
                  <form.Field name="tagline">
                    {(field) => (
                      <LockableField
                        fieldName={MetadataFieldNames.Tagline}
                        isLocked={isFieldLocked(MetadataFieldNames.Tagline)}
                        label="Tagline"
                        onToggleLock={handleToggleLock}
                      >
                        <Input
                          onChange={(e) => {
                            field.handleChange(e.target.value)
                          }}
                          placeholder="A catchy tagline or slogan"
                          value={field.state.value}
                        />
                      </LockableField>
                    )}
                  </form.Field>
                  <form.Field name="contentRating">
                    {(field) => (
                      <LockableField
                        fieldName={MetadataFieldNames.ContentRating}
                        isLocked={isFieldLocked(
                          MetadataFieldNames.ContentRating,
                        )}
                        label="Content Rating"
                        onToggleLock={handleToggleLock}
                      >
                        <Input
                          onChange={(e) => {
                            field.handleChange(e.target.value)
                          }}
                          placeholder="e.g., PG-13, R, TV-MA"
                          value={field.state.value}
                        />
                      </LockableField>
                    )}
                  </form.Field>
                </div>
              </TabsContent>
              <TabsContent
                className={`
                  flex-1 space-y-6 overflow-x-hidden overflow-y-auto p-4
                `}
                value="tags"
              >
                <div className="space-y-4">
                  <div>
                    <h2 className="text-lg font-semibold">Tags & Genres</h2>
                    <p className="text-sm text-stone-300">
                      Categorize and organize this item.
                    </p>
                  </div>
                  <LockableField
                    fieldName={MetadataFieldNames.Genres}
                    isLocked={isFieldLocked(MetadataFieldNames.Genres)}
                    label="Genres"
                    onToggleLock={handleToggleLock}
                  >
                    <div aria-labelledby={genresId} className="space-y-2">
                      <div className="flex gap-2">
                        <Input
                          onChange={(e) => {
                            setGenreDraft(e.target.value)
                          }}
                          onKeyDown={(e) => {
                            if (e.key === 'Enter') {
                              e.preventDefault()
                              addGenre()
                            }
                          }}
                          placeholder="Add a genre"
                          value={genreDraft}
                        />
                        <Button
                          disabled={!genreDraft.trim()}
                          onClick={addGenre}
                          type="button"
                          variant="secondary"
                        >
                          Add
                        </Button>
                      </div>
                      {formValues.genres.length > 0 && (
                        <div className="flex flex-wrap gap-2">
                          {formValues.genres.map((genre) => (
                            <span
                              className={`
                                inline-flex items-center gap-1 rounded-full
                                bg-stone-700 px-3 py-1 text-sm
                              `}
                              key={genre}
                            >
                              {genre}
                              <button
                                aria-label={`Remove ${genre}`}
                                className={`
                                  ml-1
                                  hover:text-red-400
                                `}
                                onClick={() => {
                                  removeGenre(genre)
                                }}
                                type="button"
                              >
                                ×
                              </button>
                            </span>
                          ))}
                        </div>
                      )}
                    </div>
                  </LockableField>
                  <LockableField
                    fieldName={MetadataFieldNames.Tags}
                    isLocked={isFieldLocked(MetadataFieldNames.Tags)}
                    label="Tags"
                    onToggleLock={handleToggleLock}
                  >
                    <div aria-labelledby={tagsId} className="space-y-2">
                      <div className="flex gap-2">
                        <Input
                          onChange={(e) => {
                            setTagDraft(e.target.value)
                          }}
                          onKeyDown={(e) => {
                            if (e.key === 'Enter') {
                              e.preventDefault()
                              addTag()
                            }
                          }}
                          placeholder="Add a tag"
                          value={tagDraft}
                        />
                        <Button
                          disabled={!tagDraft.trim()}
                          onClick={addTag}
                          type="button"
                          variant="secondary"
                        >
                          Add
                        </Button>
                      </div>
                      {formValues.tags.length > 0 && (
                        <div className="flex flex-wrap gap-2">
                          {formValues.tags.map((tag) => (
                            <span
                              className={`
                                inline-flex items-center gap-1 rounded-full
                                bg-stone-700 px-3 py-1 text-sm
                              `}
                              key={tag}
                            >
                              {tag}
                              <button
                                aria-label={`Remove ${tag}`}
                                className={`
                                  ml-1
                                  hover:text-red-400
                                `}
                                onClick={() => {
                                  removeTag(tag)
                                }}
                                type="button"
                              >
                                ×
                              </button>
                            </span>
                          ))}
                        </div>
                      )}
                    </div>
                  </LockableField>
                </div>
              </TabsContent>
              <TabsContent
                className={`
                  flex-1 space-y-6 overflow-x-hidden overflow-y-auto p-4
                `}
                value="external-ids"
              >
                <div className="space-y-4">
                  <div>
                    <h2 className="text-lg font-semibold">
                      External Identifiers
                    </h2>
                    <p className="text-sm text-stone-300">
                      Link this item to external databases like TMDB, IMDb, or
                      TVDB.
                    </p>
                  </div>
                  <LockableField
                    fieldName={MetadataFieldNames.ExternalIdentifiers}
                    isLocked={isFieldLocked(
                      MetadataFieldNames.ExternalIdentifiers,
                    )}
                    label="External IDs"
                    onToggleLock={handleToggleLock}
                  >
                    <div aria-labelledby={externalIdsId} className="space-y-3">
                      <div
                        className={`
                          grid gap-2
                          md:grid-cols-3
                        `}
                      >
                        <Input
                          onChange={(e) => {
                            setExternalIdDraft((prev) => ({
                              ...prev,
                              provider: e.target.value,
                            }))
                          }}
                          placeholder="Provider (e.g., tmdb)"
                          value={externalIdDraft.provider}
                        />
                        <Input
                          onChange={(e) => {
                            setExternalIdDraft((prev) => ({
                              ...prev,
                              value: e.target.value,
                            }))
                          }}
                          onKeyDown={(e) => {
                            if (e.key === 'Enter') {
                              e.preventDefault()
                              addExternalId()
                            }
                          }}
                          placeholder="ID value"
                          value={externalIdDraft.value}
                        />
                        <Button
                          disabled={!externalIdDraft.provider.trim()}
                          onClick={addExternalId}
                          type="button"
                          variant="secondary"
                        >
                          {formValues.externalIds.some(
                            (ext) =>
                              ext.provider.toLowerCase() ===
                              externalIdDraft.provider.trim().toLowerCase(),
                          )
                            ? 'Update'
                            : 'Add'}
                        </Button>
                      </div>
                      <p className="text-xs text-stone-400">
                        Common providers: tmdb, imdb, tvdb, musicbrainz
                      </p>
                      {formValues.externalIds.length > 0 && (
                        <ul className="space-y-2">
                          {formValues.externalIds.map((ext) => (
                            <li
                              className={`
                                flex items-center justify-between rounded border
                                border-stone-700 px-3 py-2 text-sm
                              `}
                              key={ext.provider}
                            >
                              <div>
                                <span className="font-medium">
                                  {ext.provider}
                                </span>
                                <span className="ml-2 text-stone-400">
                                  {ext.value || '(no value)'}
                                </span>
                              </div>
                              <Button
                                aria-label={`Remove ${ext.provider}`}
                                onClick={() => {
                                  removeExternalId(ext.provider)
                                }}
                                size="sm"
                                type="button"
                                variant="ghost"
                              >
                                Remove
                              </Button>
                            </li>
                          ))}
                        </ul>
                      )}
                    </div>
                  </LockableField>
                </div>
              </TabsContent>
              <TabsContent
                className="mt-4 flex h-full flex-col gap-4 overflow-hidden"
                value="custom-fields"
              >
                <div className="flex flex-col gap-4 overflow-y-auto">
                  {applicableCustomFields.length === 0 ? (
                    <p className="text-sm text-muted-foreground">
                      No custom fields are defined for this item type.
                    </p>
                  ) : (
                    applicableCustomFields.map((fieldDef) => {
                      const existingValue = formValues.extraFields.find(
                        (f) => f.key === fieldDef.key,
                      )?.value

                      const handleCustomFieldChange = (newValue: unknown) => {
                        const currentFields = [...formValues.extraFields]
                        const existingIndex = currentFields.findIndex(
                          (f) => f.key === fieldDef.key,
                        )
                        if (existingIndex >= 0) {
                          currentFields[existingIndex] = {
                            key: fieldDef.key,
                            value: newValue,
                          }
                        } else {
                          currentFields.push({
                            key: fieldDef.key,
                            value: newValue,
                          })
                        }
                        form.setFieldValue('extraFields', currentFields)
                      }

                      return (
                        <div className="flex flex-col gap-2" key={fieldDef.key}>
                          <Label htmlFor={`custom-field-${fieldDef.key}`}>
                            {fieldDef.label}
                          </Label>
                          {fieldDef.widget === DetailFieldWidgetType.Boolean ? (
                            <div className="flex items-center gap-2">
                              <input
                                checked={Boolean(existingValue)}
                                className="size-4"
                                id={`custom-field-${fieldDef.key}`}
                                onChange={(e) => {
                                  handleCustomFieldChange(e.target.checked)
                                }}
                                type="checkbox"
                              />
                              <span className="text-sm text-muted-foreground">
                                {existingValue ? 'Yes' : 'No'}
                              </span>
                            </div>
                          ) : fieldDef.widget ===
                            DetailFieldWidgetType.Number ? (
                            <Input
                              id={`custom-field-${fieldDef.key}`}
                              onChange={(e) => {
                                handleCustomFieldChange(
                                  e.target.value
                                    ? Number(e.target.value)
                                    : null,
                                )
                              }}
                              placeholder={fieldDef.label}
                              type="number"
                              value={
                                existingValue !== null &&
                                existingValue !== undefined
                                  ? String(existingValue)
                                  : ''
                              }
                            />
                          ) : (
                            <Input
                              id={`custom-field-${fieldDef.key}`}
                              onChange={(e) => {
                                handleCustomFieldChange(e.target.value || null)
                              }}
                              placeholder={fieldDef.label}
                              type="text"
                              value={
                                existingValue !== null &&
                                existingValue !== undefined
                                  ? String(existingValue)
                                  : ''
                              }
                            />
                          )}
                        </div>
                      )
                    })
                  )}
                </div>
              </TabsContent>
            </div>
          </Tabs>
          <DialogFooter
            className={`
              mt-auto flex flex-wrap gap-2 bg-stone-700 pt-4
              md:justify-between
            `}
          >
            <Button onClick={handleClose} type="button" variant="secondary">
              Cancel
            </Button>
            <Button disabled={loading} type="submit">
              {loading ? 'Saving…' : 'Save Changes'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}

function LockableField({
  children,
  fieldName,
  isLocked,
  label,
  onToggleLock,
}: Readonly<LockableFieldProps>) {
  const id = useId()
  return (
    <div className="space-y-2">
      <div className="flex items-center justify-between">
        <Label htmlFor={id}>{label}</Label>
        <Tooltip>
          <TooltipTrigger asChild>
            <Button
              aria-label={isLocked ? `Unlock ${label}` : `Lock ${label}`}
              className={cn(
                'h-6 w-6',
                isLocked ? 'text-amber-500' : 'text-stone-400',
              )}
              onClick={() => {
                onToggleLock(fieldName)
              }}
              size="icon"
              type="button"
              variant="ghost"
            >
              {isLocked ? (
                <Lock className="size-4" />
              ) : (
                <LockOpen className="size-4" />
              )}
            </Button>
          </TooltipTrigger>
          <TooltipContent>
            {isLocked
              ? 'Locked: Metadata agents will not overwrite this field'
              : 'Unlocked: Metadata agents may update this field'}
          </TooltipContent>
        </Tooltip>
      </div>
      {children}
    </div>
  )
}

export default EditMetadataItemDialog
