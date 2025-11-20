import type { ReactFormExtendedApi } from '@tanstack/react-form'

import { useMutation } from '@apollo/client/react'
import { useForm, useStore } from '@tanstack/react-form'
import { Plus } from 'lucide-react'
import { type ReactElement, useId, useState } from 'react'
import IconBook from '~icons/material-symbols/book-2'
import IconPictures from '~icons/material-symbols/image'
import IconMovie from '~icons/material-symbols/movie'
import IconMusic from '~icons/material-symbols/music-note'
import IconGames from '~icons/material-symbols/videogame-asset'

import { graphql } from '@/shared/api/graphql'
import {
  type AddLibrarySectionInput,
  EpisodeSortOrder,
  type LibrarySectionSettingsInput,
  LibraryType,
} from '@/shared/api/graphql/graphql'
import { Button } from '@/shared/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
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
import {
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
} from '@/shared/components/ui/tabs'
import { Textarea } from '@/shared/components/ui/textarea'
import { cn } from '@/shared/lib/utils'

import { ContentTypeIcon } from './ContentTypeIcon'
import { DirectoryBrowserModal } from './DirectoryBrowserModal'

const addLibrarySectionMutation = graphql(`
  mutation AddLibrarySection($input: AddLibrarySectionInput!) {
    addLibrarySection(input: $input) {
      librarySection {
        id
        name
        type
      }
    }
  }
`)

type WizardStep = 'advanced' | 'agents' | 'category' | 'details' | 'folders'

const wizardSteps: {
  description: string
  key: WizardStep
  label: string
}[] = [
  {
    description: 'Tell us what kind of media you want to organize.',
    key: 'category',
    label: 'Content type',
  },
  {
    description: 'Pick the exact library flavor and add the essentials.',
    key: 'details',
    label: 'Library details',
  },
  {
    description: 'Tell us where the files for this library live.',
    key: 'folders',
    label: 'Media folders',
  },
  {
    description: 'Arrange metadata agents so we know which should run first.',
    key: 'agents',
    label: 'Metadata agents',
  },
  {
    description: 'Fine-tune discovery, playback, and agent-specific tweaks.',
    key: 'advanced',
    label: 'Advanced settings',
  },
]

interface CategoryDefinition {
  description: string
  helper: string
  icon: ReactElement
  key: ContentCategoryKey
  label: string
  types: {
    description: string
    label: string
    value: LibraryType
  }[]
}

type ContentCategoryKey = 'audio' | 'books' | 'games' | 'photos' | 'video'

const contentCategories: CategoryDefinition[] = [
  {
    description:
      'Movies, episodic series, concert recordings, and personal clips.',
    helper: 'Best for: movies, shows, and home recordings',
    icon: <IconMovie />,
    key: 'video',
    label: 'Video',
    types: [
      {
        description: 'Feature films, documentaries, and short films.',
        label: 'Movies',
        value: LibraryType.Movies,
      },
      {
        description: 'Series with seasons and episodes.',
        label: 'TV Shows',
        value: LibraryType.TvShows,
      },
      {
        description: 'Official music video releases and live sessions.',
        label: 'Music Videos',
        value: LibraryType.MusicVideos,
      },
      {
        description: 'Phone videos, camcorder footage, and personal events.',
        label: 'Home Videos',
        value: LibraryType.HomeVideos,
      },
    ],
  },
  {
    description: 'Music-first experiences including long-form audio.',
    helper: 'Best for: music, podcasts, audiobooks',
    icon: <IconMusic />,
    key: 'audio',
    label: 'Audio',
    types: [
      {
        description: 'Albums, singles, and compilations.',
        label: 'Music',
        value: LibraryType.Music,
      },
      {
        description: 'Long-form spoken word series.',
        label: 'Podcasts',
        value: LibraryType.Podcasts,
      },
      {
        description: 'Narrated books with chapters and series support.',
        label: 'Audiobooks',
        value: LibraryType.Audiobooks,
      },
    ],
  },
  {
    description: 'Reading-friendly metadata for literature and periodicals.',
    helper: 'Best for: books, comics, manga, magazines',
    icon: <IconBook />,
    key: 'books',
    label: 'Reading',
    types: [
      {
        description: 'Novels, non-fiction, and written literature.',
        label: 'Books',
        value: LibraryType.Books,
      },
      {
        description: 'Graphic novels and comic book runs.',
        label: 'Comics',
        value: LibraryType.Comics,
      },
      {
        description: 'Serialized manga releases.',
        label: 'Manga',
        value: LibraryType.Manga,
      },
      {
        description: 'Weekly or monthly magazine issues.',
        label: 'Magazines',
        value: LibraryType.Magazines,
      },
    ],
  },
  {
    description: 'Photo and art-forward collections.',
    helper: 'Best for: photos, pictures',
    icon: <IconPictures />,
    key: 'photos',
    label: 'Photos & Art',
    types: [
      {
        description: 'Family albums and photography projects.',
        label: 'Photos',
        value: LibraryType.Photos,
      },
      {
        description: 'Wallpapers, concept art, and illustrations.',
        label: 'Pictures',
        value: LibraryType.Pictures,
      },
    ],
  },
  {
    description: 'Interactive experiences and ROM collections.',
    helper: 'Best for: games',
    icon: <IconGames />,
    key: 'games',
    label: 'Games',
    types: [
      {
        description: 'PC, console, or retro game dumps.',
        label: 'Games',
        value: LibraryType.Games,
      },
    ],
  },
]

const videoLibraryTypes = new Set<LibraryType>([
  LibraryType.HomeVideos,
  LibraryType.Movies,
  LibraryType.MusicVideos,
  LibraryType.TvShows,
])

const episodicLibraryTypes = new Set<LibraryType>([LibraryType.TvShows])

interface AgentSettingDraft {
  agentId: string
  key: string
  value: string
}

const emptyAgentSettingDraft: AgentSettingDraft = {
  agentId: '',
  key: '',
  value: '',
}

interface FormValues {
  contentCategory: ContentCategoryKey | null
  contentType: LibraryType | null
  episodeSortOrder: EpisodeSortOrder
  folders: string[]
  hideSeasonsForSingleSeasonSeries: boolean
  metadataAgentOrder: string[]
  metadataAgentSettings: AgentSettingDraft[]
  name: string
  preferredAudioLanguages: string[]
  preferredMetadataLanguage: string
  preferredSubtitleLanguages: string[]
}

const createDefaultFormValues = (): FormValues => ({
  contentCategory: null,
  contentType: null,
  episodeSortOrder: EpisodeSortOrder.AirDate,
  folders: [],
  hideSeasonsForSingleSeasonSeries: true,
  metadataAgentOrder: [],
  metadataAgentSettings: [],
  name: '',
  preferredAudioLanguages: [],
  preferredMetadataLanguage: 'en',
  preferredSubtitleLanguages: [],
})

const parseListField = (value: string): string[] =>
  value
    .split(/[\n,]/)
    .map((entry) => entry.trim())
    .filter(Boolean)

const episodeSortOrderOptions: {
  description: string
  label: string
  value: EpisodeSortOrder
}[] = [
  {
    description: 'Default. Groups by original air date.',
    label: 'Air date',
    value: EpisodeSortOrder.AirDate,
  },
  {
    description: 'Match studio numbering where available.',
    label: 'Production order',
    value: EpisodeSortOrder.Production,
  },
  {
    description: 'Sort strictly by season and episode numbers.',
    label: 'Season · Episode',
    value: EpisodeSortOrder.SeasonEpisode,
  },
]

interface AddContentSourceDialogProps {
  onCreated?: () => void
}

export function AddContentSourceDialog({
  onCreated,
}: AddContentSourceDialogProps) {
  const [open, setOpen] = useState(false)
  const [tab, setTab] = useState<WizardStep>('category')
  const [folderDraft, setFolderDraft] = useState('')
  const [directoryBrowserOpen, setDirectoryBrowserOpen] = useState(false)
  const [agentSettingDraft, setAgentSettingDraft] = useState(
    emptyAgentSettingDraft,
  )
  const foldersId = useId()
  const [addLibrarySection, { error, loading }] = useMutation(
    addLibrarySectionMutation,
  )

  const form = useForm({
    defaultValues: createDefaultFormValues(),
    onSubmit: async ({ value }: { value: FormValues }) => {
      if (!value.contentType) {
        return
      }

      const trimmedName = value.name.trim()
      if (!trimmedName || value.folders.length === 0) {
        return
      }

      const groupedAgentSettings = value.metadataAgentSettings.reduce<
        Record<string, Record<string, string>>
      >((acc, setting) => {
        const agentId = setting.agentId.trim()
        const settingKey = setting.key.trim()
        if (!agentId || !settingKey) {
          return acc
        }
        const settingValue = setting.value.trim()
        acc[agentId] = {
          ...acc[agentId],
          [settingKey]: settingValue,
        }
        return acc
      }, {})

      const metadataAgentSettingsInput = Object.entries(
        groupedAgentSettings,
      ).map(([agentId, settings]) => ({
        key: agentId,
        value: Object.entries(settings).map(([settingKey, settingValue]) => ({
          key: settingKey,
          value: settingValue,
        })),
      }))

      const settings: LibrarySectionSettingsInput = {
        episodeSortOrder: value.episodeSortOrder,
        hideSeasonsForSingleSeasonSeries:
          value.hideSeasonsForSingleSeasonSeries,
        metadataAgentOrder: value.metadataAgentOrder,
        metadataAgentSettings: metadataAgentSettingsInput,
        preferredAudioLanguages: value.preferredAudioLanguages,
        preferredMetadataLanguage:
          value.preferredMetadataLanguage.trim() || 'en',
        preferredSubtitleLanguages: value.preferredSubtitleLanguages,
      }

      const input: AddLibrarySectionInput = {
        name: trimmedName,
        rootPaths: value.folders,
        settings,
        type: value.contentType,
      }

      await addLibrarySection({ variables: { input } })
      resetWizard()
      onCreated?.()
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

  // Subscribe to the latest form values so derived booleans stay in sync
  const formValues = useStore(form.store, (formState) => formState.values)

  function resetWizard() {
    setOpen(false)
    form.reset()
    setFolderDraft('')
    setDirectoryBrowserOpen(false)
    setAgentSettingDraft(emptyAgentSettingDraft)
    setTab('category')
  }

  const activeCategory = contentCategories.find(
    (category) => category.key === formValues.contentCategory,
  )

  const hasCategorySelection = Boolean(formValues.contentCategory)
  const hasLibraryTypeSelection = Boolean(formValues.contentType)
  const hasLibraryName = formValues.name.trim().length > 0
  const hasFolders = formValues.folders.length > 0
  const hasBasicsComplete =
    hasCategorySelection && hasLibraryTypeSelection && hasLibraryName
  const hasDetailsComplete = hasBasicsComplete && hasFolders

  const isVideoLibrary = formValues.contentType
    ? videoLibraryTypes.has(formValues.contentType)
    : false

  const isEpisodicLibrary = formValues.contentType
    ? episodicLibraryTypes.has(formValues.contentType)
    : false

  const stepAvailability: Record<WizardStep, boolean> = {
    advanced: hasDetailsComplete,
    agents: hasDetailsComplete,
    category: true,
    details: hasCategorySelection,
    folders: hasBasicsComplete,
  }

  const currentStepIndex = wizardSteps.findIndex((step) => step.key === tab)
  const previousStepKey =
    currentStepIndex > 0 ? wizardSteps[currentStepIndex - 1].key : undefined
  const nextStepKey =
    currentStepIndex < wizardSteps.length - 1
      ? wizardSteps[currentStepIndex + 1].key
      : undefined

  const canContinue =
    tab === 'category'
      ? hasCategorySelection
      : tab === 'details'
        ? hasBasicsComplete
        : tab === 'folders'
          ? hasDetailsComplete
          : true

  console.log({ canContinue, hasCategorySelection, hasDetailsComplete, tab })

  const disableSubmit = loading || !hasDetailsComplete

  function appendFolder(path: string) {
    const trimmed = path.trim()
    if (!trimmed) {
      return false
    }
    if (formValues.folders.some((existing) => existing === trimmed)) {
      return false
    }
    form.setFieldValue('folders', [...formValues.folders, trimmed])
    return true
  }

  function addFolder() {
    if (appendFolder(folderDraft)) {
      setFolderDraft('')
    }
  }

  function removeFolder(idx: number) {
    form.setFieldValue(
      'folders',
      formValues.folders.filter((_, i) => i !== idx),
    )
  }

  const agentSettingReady =
    Boolean(agentSettingDraft.agentId.trim()) &&
    Boolean(agentSettingDraft.key.trim()) &&
    Boolean(agentSettingDraft.value.trim())

  function addAgentSetting() {
    if (!agentSettingReady) {
      return
    }

    form.setFieldValue('metadataAgentSettings', [
      ...formValues.metadataAgentSettings,
      {
        agentId: agentSettingDraft.agentId.trim(),
        key: agentSettingDraft.key.trim(),
        value: agentSettingDraft.value.trim(),
      },
    ])
    setAgentSettingDraft(emptyAgentSettingDraft)
  }

  function removeAgentSetting(idx: number) {
    form.setFieldValue(
      'metadataAgentSettings',
      formValues.metadataAgentSettings.filter((_, i) => i !== idx),
    )
  }

  const lastFolder =
    formValues.folders.length > 0
      ? formValues.folders[formValues.folders.length - 1]
      : null

  function handleDirectorySelection(path: string) {
    const added = appendFolder(path)
    if (!added) {
      setFolderDraft(path)
    }
    setDirectoryBrowserOpen(false)
  }

  return (
    <>
      <Dialog
        onOpenChange={(isOpen) => {
          setOpen(isOpen)
          if (!isOpen) {
            resetWizard()
          }
        }}
        open={open}
      >
        <DialogTrigger asChild>
          <Button aria-label="Add Library" size="icon" variant="ghost">
            <Plus />
          </Button>
        </DialogTrigger>
        <DialogContent
          className={`
            max-w-4xl gap-0
            lg:min-w-4xl
          `}
        >
          <DialogHeader className="border-b bg-stone-700">
            <DialogTitle>Add Content Source</DialogTitle>
          </DialogHeader>
          <form
            aria-describedby={error ? 'create-cs-error' : undefined}
            noValidate
            onSubmit={(event) => {
              event.preventDefault()
              void form.handleSubmit()
            }}
          >
            {error && (
              <p
                className="rounded bg-red-100 px-2 py-1 text-sm text-red-700"
                id="create-cs-error"
                role="alert"
              >
                {error.message}
              </p>
            )}
            <Tabs
              className={`
                flex min-h-128 w-full flex-row items-stretch gap-0 bg-stone-800
              `}
              onValueChange={(value) => {
                const nextTab = value as WizardStep
                if (!stepAvailability[nextTab]) {
                  return
                }
                setTab(nextTab)
              }}
              value={tab}
            >
              <TabsList
                className={`
                  flex h-auto w-60 shrink flex-col justify-start rounded-none
                  border-r bg-stone-900 p-0 text-left
                `}
              >
                {wizardSteps.map((step, index) => (
                  <TabsTrigger
                    className={`
                      flex h-auto max-h-28 w-full flex-col items-start gap-1
                      rounded-none border-b border-transparent px-4 py-4
                      text-left text-sm whitespace-normal
                      data-disabled:opacity-40
                      data-[state=active]:border-primary/60
                      data-[state=active]:bg-stone-800
                      data-[state=active]:text-primary
                    `}
                    disabled={!stepAvailability[step.key]}
                    key={step.key}
                    value={step.key}
                  >
                    <span
                      className={`
                        text-xs tracking-wide text-stone-400 uppercase
                      `}
                    >
                      Step {index + 1}
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
              <div className={`flex flex-1 flex-col`}>
                <TabsContent
                  className={`
                    flex-1 space-y-6 overflow-x-hidden overflow-y-scroll p-4
                  `}
                  value="category"
                >
                  <div className="space-y-4">
                    <div>
                      <h2 className="text-lg font-semibold">
                        Select the content family
                      </h2>
                      <p className="text-sm text-stone-300">
                        We tailor the remaining steps based on this choice.
                      </p>
                    </div>
                    <form.Field name="contentCategory">
                      {(field) => (
                        <div
                          className={`
                            grid gap-3
                            md:grid-cols-2
                          `}
                        >
                          {contentCategories.map((category) => {
                            const isActive = field.state.value === category.key
                            return (
                              <button
                                aria-pressed={isActive}
                                className={cn(
                                  `
                                    flex flex-col gap-2 rounded-xl border p-4
                                    text-left transition-colors
                                    hover:border-primary/60
                                  `,
                                  isActive
                                    ? 'border-primary ring-2 ring-primary/30'
                                    : 'border-stone-700/70',
                                )}
                                key={category.key}
                                onClick={() => {
                                  field.handleChange(category.key)
                                  form.setFieldValue('contentType', null)
                                }}
                                type="button"
                              >
                                <div className="flex items-center gap-3">
                                  <span
                                    className={`
                                      rounded-full bg-stone-900/70 p-3
                                    `}
                                  >
                                    {category.icon}
                                  </span>
                                  <div>
                                    <p className="text-base font-medium">
                                      {category.label}
                                    </p>
                                    <p className="text-sm text-stone-300">
                                      {category.description}
                                    </p>
                                  </div>
                                </div>
                                <p className="text-xs text-stone-400">
                                  {category.helper}
                                </p>
                              </button>
                            )
                          })}
                        </div>
                      )}
                    </form.Field>
                  </div>
                </TabsContent>
                <TabsContent
                  className={`
                    flex-1 space-y-6 overflow-x-hidden overflow-y-scroll p-4
                  `}
                  value="details"
                >
                  <div className="space-y-4">
                    <div>
                      <h2 className="text-lg font-semibold">
                        Fine-tune the fit
                      </h2>
                      <p className="text-sm text-stone-300">
                        Choose a specific library type and fill in the basics so
                        Nexa can set up scanning and naming correctly.
                      </p>
                    </div>
                    <form.Field name="contentType">
                      {(field) => (
                        <div className="space-y-2">
                          <Label>Library section type</Label>
                          {!activeCategory ? (
                            <p className="text-sm text-stone-400">
                              Pick a content family first to see the available
                              section types.
                            </p>
                          ) : (
                            <div
                              className={`
                                grid gap-3
                                lg:grid-cols-2
                              `}
                            >
                              {activeCategory.types.map((typeOption) => {
                                const isSelected =
                                  field.state.value === typeOption.value
                                return (
                                  <button
                                    aria-pressed={isSelected}
                                    className={cn(
                                      `
                                        flex items-start gap-3 rounded-lg border
                                        p-3 text-left transition-colors
                                        hover:border-primary/60
                                      `,
                                      isSelected
                                        ? `
                                          border-primary ring-2 ring-primary/30
                                        `
                                        : 'border-stone-700/70',
                                    )}
                                    key={typeOption.value}
                                    onClick={() => {
                                      field.handleChange(typeOption.value)
                                    }}
                                    type="button"
                                  >
                                    <ContentTypeIcon
                                      className="mt-1 size-6"
                                      contentType={typeOption.value}
                                    />
                                    <div>
                                      <p className="font-medium">
                                        {typeOption.label}
                                      </p>
                                      <p className="text-sm text-stone-300">
                                        {typeOption.description}
                                      </p>
                                    </div>
                                  </button>
                                )
                              })}
                            </div>
                          )}
                          {field.state.meta.errors[0] && (
                            <p className="text-xs text-red-500" role="alert">
                              {field.state.meta.errors[0]}
                            </p>
                          )}
                        </div>
                      )}
                    </form.Field>
                    <form.Field
                      name="name"
                      validators={{
                        onChange: ({ value }: { value: string }) =>
                          !value.trim() ? 'Name required' : undefined,
                      }}
                    >
                      {(field) => (
                        <div className="space-y-2">
                          <Label htmlFor="library-name">Library name *</Label>
                          <Input
                            aria-describedby={
                              field.state.meta.errors.length
                                ? 'name-error'
                                : undefined
                            }
                            aria-invalid={
                              field.state.meta.errors.length
                                ? 'true'
                                : undefined
                            }
                            id="library-name"
                            onBlur={field.handleBlur}
                            onChange={(e) => {
                              field.handleChange(e.target.value)
                            }}
                            placeholder="My 4K Movies"
                            value={field.state.value}
                          />
                          <p className="text-xs text-stone-400">
                            This is what appears in the sidebar and throughout
                            the app.
                          </p>
                          {field.state.meta.errors[0] && (
                            <p
                              className="text-xs text-red-500"
                              id="name-error"
                              role="alert"
                            >
                              {field.state.meta.errors[0]}
                            </p>
                          )}
                        </div>
                      )}
                    </form.Field>
                    <form.Field name="preferredMetadataLanguage">
                      {(field) => (
                        <div className="space-y-2">
                          <Label htmlFor="metadata-language">
                            Preferred metadata language
                          </Label>
                          <Input
                            id="metadata-language"
                            onChange={(e) => {
                              field.handleChange(e.target.value)
                            }}
                            placeholder="en-US"
                            value={field.state.value}
                          />
                          <p className="text-xs text-stone-400">
                            Use BCP-47 codes such as <code>en</code>,{' '}
                            <code>fr-CA</code>, or <code>es-MX</code>.
                          </p>
                        </div>
                      )}
                    </form.Field>
                  </div>
                </TabsContent>
                <TabsContent
                  className={`
                    flex-1 space-y-6 overflow-x-hidden overflow-y-scroll p-4
                  `}
                  value="folders"
                >
                  <div className="space-y-4">
                    <div>
                      <h2 className="text-lg font-semibold">Media folders</h2>
                      <p className="text-sm text-stone-300">
                        Add every root path Nexa should scan. This tells the
                        server where to start watching for new files.
                      </p>
                    </div>
                    <div aria-labelledby={foldersId} className="space-y-2">
                      <Label className="text-sm font-semibold" id={foldersId}>
                        Root paths
                      </Label>
                      <p className="text-xs text-stone-400">
                        Paste or type a path and press Enter to append it.
                        Include each top-level directory that belongs to this
                        library.
                      </p>
                      <div
                        className={`
                          flex flex-col gap-2
                          sm:flex-row
                        `}
                      >
                        <Input
                          className="w-full"
                          onChange={(event) => {
                            setFolderDraft(event.target.value)
                          }}
                          onKeyDown={(event) => {
                            if (event.key === 'Enter') {
                              event.preventDefault()
                              addFolder()
                            }
                          }}
                          placeholder="/mnt/media/movies"
                          value={folderDraft}
                        />
                        <Button
                          disabled={!folderDraft.trim()}
                          onClick={addFolder}
                          type="button"
                          variant="secondary"
                        >
                          Add folder
                        </Button>
                        <Button
                          onClick={() => {
                            setDirectoryBrowserOpen(true)
                          }}
                          type="button"
                          variant="outline"
                        >
                          Browse server
                        </Button>
                      </div>
                      {formValues.folders.length > 0 && (
                        <ul aria-live="polite" className="space-y-2">
                          {formValues.folders.map((folder, index) => (
                            <li
                              className={`
                                flex items-center justify-between rounded border
                                border-stone-700/70 px-3 py-2 text-sm
                              `}
                              key={`${folder}-${String(index)}`}
                            >
                              <span className="truncate" title={folder}>
                                {folder}
                              </span>
                              <Button
                                aria-label={`Remove folder ${folder}`}
                                onClick={() => {
                                  removeFolder(index)
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
                  </div>
                </TabsContent>
                <TabsContent
                  className={`
                    flex-1 space-y-6 overflow-x-hidden overflow-y-scroll p-4
                  `}
                  value="agents"
                >
                  <div className="space-y-4">
                    <div>
                      <h2 className="text-lg font-semibold">
                        Metadata agent order
                      </h2>
                      <p className="text-sm text-stone-300">
                        We will ask the first agent that matches your library
                        for metadata. Add identifiers if you need a custom
                        order.
                      </p>
                    </div>
                    <form.Field name="metadataAgentOrder">
                      {(field) => (
                        <div className="space-y-2">
                          <Label htmlFor="metadata-agent-order">
                            Execution order (optional)
                          </Label>
                          <Textarea
                            id="metadata-agent-order"
                            onChange={(event) => {
                              field.handleChange(
                                parseListField(event.target.value),
                              )
                            }}
                            placeholder={`local-file\ncloud-discovery`}
                            value={field.state.value.join('\n')}
                          />
                          <p className="text-xs text-stone-400">
                            One agent identifier per line. Leave empty to let
                            Nexa use the default discovery order.
                          </p>
                        </div>
                      )}
                    </form.Field>
                    <div
                      className={`
                        rounded border border-dashed border-stone-700/70 px-4
                        py-3 text-sm text-stone-300
                      `}
                    >
                      Coming soon: drag-and-drop agent ordering with live
                      previews. For now we respect the list above whenever those
                      agents exist on the server.
                    </div>
                  </div>
                </TabsContent>
                <TabsContent
                  className={`
                    flex-1 space-y-6 overflow-x-hidden overflow-y-scroll p-4
                  `}
                  value="advanced"
                >
                  <div className="space-y-4">
                    <div>
                      <h2 className="text-lg font-semibold">
                        Advanced library settings
                      </h2>
                      <p className="text-sm text-stone-300">
                        Dial in playback defaults and optional metadata agent
                        overrides. These values can be edited any time after the
                        library is created.
                      </p>
                    </div>
                    {isEpisodicLibrary && (
                      <div
                        className={`
                          space-y-4 rounded border border-stone-700/70 p-4
                        `}
                      >
                        <form.Field name="hideSeasonsForSingleSeasonSeries">
                          {(field) => (
                            <div
                              className={`
                                flex items-center justify-between gap-4
                              `}
                            >
                              <div>
                                <p className="text-sm font-medium">
                                  Hide seasons for single-season series
                                </p>
                                <p className="text-xs text-stone-400">
                                  Keeps mini-series pages tidy when there is
                                  only one season.
                                </p>
                              </div>
                              <Switch
                                checked={field.state.value}
                                onCheckedChange={(checked) => {
                                  field.handleChange(checked)
                                }}
                              />
                            </div>
                          )}
                        </form.Field>
                        <form.Field name="episodeSortOrder">
                          {(field) => (
                            <div className="space-y-2">
                              <Label>Episode sort order</Label>
                              <Select
                                onValueChange={(value) => {
                                  field.handleChange(value as EpisodeSortOrder)
                                }}
                                value={field.state.value}
                              >
                                <SelectTrigger className="w-full">
                                  <SelectValue placeholder="Choose how episodes are sorted" />
                                </SelectTrigger>
                                <SelectContent>
                                  {episodeSortOrderOptions.map((option) => (
                                    <SelectItem
                                      key={option.value}
                                      value={option.value}
                                    >
                                      <span className="flex flex-col">
                                        <span>{option.label}</span>
                                        <span className="text-xs text-stone-400">
                                          {option.description}
                                        </span>
                                      </span>
                                    </SelectItem>
                                  ))}
                                </SelectContent>
                              </Select>
                            </div>
                          )}
                        </form.Field>
                      </div>
                    )}
                    {isVideoLibrary && (
                      <div
                        className={`
                          space-y-4 rounded border border-stone-700/70 p-4
                        `}
                      >
                        <form.Field name="preferredAudioLanguages">
                          {(field) => (
                            <div className="space-y-2">
                              <Label htmlFor="preferred-audio-languages">
                                Preferred audio languages
                              </Label>
                              <Textarea
                                id="preferred-audio-languages"
                                onChange={(event) => {
                                  field.handleChange(
                                    parseListField(event.target.value),
                                  )
                                }}
                                placeholder={`en\nfr-CA`}
                                value={field.state.value.join('\n')}
                              />
                              <p className="text-xs text-stone-400">
                                One language per line, ordered by preference.
                                Leave empty to follow the player defaults.
                              </p>
                            </div>
                          )}
                        </form.Field>
                        <form.Field name="preferredSubtitleLanguages">
                          {(field) => (
                            <div className="space-y-2">
                              <Label htmlFor="preferred-subtitle-languages">
                                Preferred subtitle languages
                              </Label>
                              <Textarea
                                id="preferred-subtitle-languages"
                                onChange={(event) => {
                                  field.handleChange(
                                    parseListField(event.target.value),
                                  )
                                }}
                                placeholder={`en\nes`}
                                value={field.state.value.join('\n')}
                              />
                              <p className="text-xs text-stone-400">
                                Same format as audio languages. You can revisit
                                this after scanning if unsure.
                              </p>
                            </div>
                          )}
                        </form.Field>
                      </div>
                    )}
                    <div
                      className={`
                        space-y-3 rounded border border-dashed
                        border-stone-700/70 p-4
                      `}
                    >
                      <div>
                        <h3 className="text-sm font-semibold">
                          Metadata agent overrides
                        </h3>
                        <p className="text-xs text-stone-400">
                          Optional key/value hints forwarded to individual
                          agents.
                        </p>
                      </div>
                      <div
                        className={`
                          grid gap-2
                          md:grid-cols-3
                        `}
                      >
                        <Input
                          onChange={(e) => {
                            setAgentSettingDraft((draft) => ({
                              ...draft,
                              agentId: e.target.value,
                            }))
                          }}
                          placeholder="Agent ID"
                          value={agentSettingDraft.agentId}
                        />
                        <Input
                          onChange={(e) => {
                            setAgentSettingDraft((draft) => ({
                              ...draft,
                              key: e.target.value,
                            }))
                          }}
                          placeholder="Setting key"
                          value={agentSettingDraft.key}
                        />
                        <Input
                          onChange={(e) => {
                            setAgentSettingDraft((draft) => ({
                              ...draft,
                              value: e.target.value,
                            }))
                          }}
                          placeholder="Value"
                          value={agentSettingDraft.value}
                        />
                      </div>
                      <div className="flex justify-end">
                        <Button
                          disabled={!agentSettingReady}
                          onClick={addAgentSetting}
                          type="button"
                          variant="secondary"
                        >
                          Add override
                        </Button>
                      </div>
                      {formValues.metadataAgentSettings.length > 0 ? (
                        <ul className="space-y-2">
                          {formValues.metadataAgentSettings.map(
                            (setting, index) => (
                              <li
                                className={`
                                  flex items-center justify-between rounded
                                  border border-stone-700/70 px-3 py-2 text-sm
                                `}
                                key={`${setting.agentId}-${setting.key}-${String(index)}`}
                              >
                                <div>
                                  <p className="font-medium">
                                    {setting.agentId}
                                  </p>
                                  <p className="text-xs text-stone-400">
                                    {setting.key} = {setting.value}
                                  </p>
                                </div>
                                <Button
                                  aria-label={`Remove override for ${setting.agentId}`}
                                  onClick={() => {
                                    removeAgentSetting(index)
                                  }}
                                  size="sm"
                                  type="button"
                                  variant="ghost"
                                >
                                  Remove
                                </Button>
                              </li>
                            ),
                          )}
                        </ul>
                      ) : (
                        <p className="text-xs text-stone-400">
                          No overrides yet. Leave this empty unless a metadata
                          agent requests specific fields.
                        </p>
                      )}
                    </div>
                  </div>
                </TabsContent>
              </div>
            </Tabs>
            <DialogFooter
              className={`mt-auto flex flex-wrap gap-2 bg-stone-700 pt-4`}
            >
              <Button onClick={resetWizard} type="button" variant="secondary">
                Cancel
              </Button>
              {previousStepKey && (
                <Button
                  onClick={() => {
                    setTab(previousStepKey)
                  }}
                  type="button"
                  variant="outline"
                >
                  Back
                </Button>
              )}
              {tab === 'advanced' ? (
                <Button disabled={disableSubmit} type="submit">
                  {loading ? 'Creating…' : 'Create library'}
                </Button>
              ) : (
                <Button
                  disabled={!canContinue || !nextStepKey}
                  onClick={() => {
                    if (nextStepKey) {
                      setTab(nextStepKey)
                    }
                  }}
                  type="button"
                >
                  Continue
                </Button>
              )}
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
      <DirectoryBrowserModal
        initialPath={folderDraft || lastFolder}
        onClose={() => {
          setDirectoryBrowserOpen(false)
        }}
        onSelect={handleDirectorySelection}
        open={directoryBrowserOpen}
      />
    </>
  )
}

export default AddContentSourceDialog
