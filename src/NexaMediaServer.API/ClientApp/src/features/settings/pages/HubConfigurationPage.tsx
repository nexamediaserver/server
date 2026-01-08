import { useMutation, useQuery } from '@apollo/client/react'
import { useEffect, useMemo, useState } from 'react'

import {
  HomeHubDefinitionsQuery,
  LibraryDiscoverHubDefinitionsQuery,
} from '@/features/hubs/queries'
import { useConfigurationDefaults } from '@/features/settings/hooks'
import { HubContext, HubType, MetadataType } from '@/shared/api/graphql/graphql'
import { SortableList } from '@/shared/components/SortableList'
import { Button } from '@/shared/components/ui/button'
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from '@/shared/components/ui/card'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/shared/components/ui/select'

import { SettingsPageContainer, SettingsPageHeader } from '../components'
import {
  AdminLibrarySectionsListQuery,
  HubConfigurationQuery,
  UpdateHubConfigurationMutation,
} from '../queries'

interface HubOption {
  description?: null | string
  title: string
  type: HubType
}

const formatHubType = (type: HubType) =>
  type
    .toLowerCase()
    .split('_')
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join(' ')

export function HubConfigurationPage() {
  const [context, setContext] = useState<HubContext>(HubContext.Home)
  const [libraryId, setLibraryId] = useState<string | undefined>()
  const [metadataType, setMetadataType] = useState<MetadataType | undefined>()
  const metadataTypeOptions = useMemo(
    () =>
      Object.values(MetadataType).filter(
        (type) => type !== MetadataType.Unknown,
      ),
    [],
  )

  const { data: librariesData } = useQuery(AdminLibrarySectionsListQuery)

  const shouldFetchDiscoverDefinitions =
    context === HubContext.LibraryDiscover && !!libraryId
  const discoverDefinitionQuery = useQuery(LibraryDiscoverHubDefinitionsQuery, {
    skip: !shouldFetchDiscoverDefinitions,
    variables: { librarySectionId: libraryId ?? '' },
  })

  const homeDefinitionQuery = useQuery(HomeHubDefinitionsQuery, {
    skip: context !== HubContext.Home,
  })

  const {
    data: configurationData,
    loading: loadingConfiguration,
    refetch,
  } = useQuery(HubConfigurationQuery, {
    skip: context === HubContext.ItemDetail && !metadataType,
    variables: {
      input: {
        context,
        librarySectionId:
          context !== HubContext.Home ? (libraryId ?? null) : null,
        metadataType: context === HubContext.ItemDetail ? metadataType : null,
      },
    },
  })

  const [updateConfiguration, { loading: saving }] = useMutation(
    UpdateHubConfigurationMutation,
  )

  const availableHubOptions: HubOption[] = useMemo(() => {
    if (context === HubContext.Home) {
      return (homeDefinitionQuery.data?.homeHubDefinitions ?? []).map(
        (definition) => ({
          description: null,
          title: definition.title,
          type: definition.type,
        }),
      )
    }

    if (context === HubContext.LibraryDiscover) {
      return (
        discoverDefinitionQuery.data?.libraryDiscoverHubDefinitions ?? []
      ).map((definition) => ({
        description: null,
        title: definition.title,
        type: definition.type,
      }))
    }

    if (context === HubContext.ItemDetail && metadataType) {
      return Object.values(HubType).map((type) => ({
        title: formatHubType(type),
        type,
      }))
    }

    return []
  }, [
    context,
    homeDefinitionQuery.data,
    discoverDefinitionQuery.data,
    metadataType,
  ])

  // Memoize the array of hub types to prevent infinite re-renders
  const availableHubTypes = useMemo(
    () => availableHubOptions.map((option) => option.type),
    [availableHubOptions],
  )

  const {
    disabled,
    enabled,
    hiddenDisabled,
    hiddenEnabled,
    setDisabled,
    setEnabled,
  } = useConfigurationDefaults<HubType>({
    availableOptions: availableHubTypes,
    configData: configurationData?.hubConfiguration
      ? {
          disabled: configurationData.hubConfiguration.disabledHubTypes,
          enabled: configurationData.hubConfiguration.enabledHubTypes,
        }
      : configurationData === undefined
        ? undefined
        : null,
    resetDependencies: [context, metadataType, libraryId],
    trackHidden: true,
  })

  useEffect(() => {
    if (context !== HubContext.ItemDetail) {
      setMetadataType(undefined)
    }
    if (context === HubContext.Home) {
      setLibraryId(undefined)
    }
  }, [context])

  const libraryOptions = useMemo(
    () => librariesData?.librarySections?.edges?.map((edge) => edge.node) ?? [],
    [librariesData],
  )

  const allHubTypes = useMemo(
    () => [...(enabled ?? []), ...(disabled ?? [])],
    [enabled, disabled],
  )

  const handleHubOrderChange = (newOrder: HubType[]) => {
    // Preserve enabled/disabled state during reorder
    const enabledSet = new Set(enabled ?? [])
    const newEnabled = newOrder.filter((type) => enabledSet.has(type))
    const newDisabled = newOrder.filter((type) => !enabledSet.has(type))
    setEnabled(newEnabled)
    setDisabled(newDisabled)
  }

  const toggleHub = (type: HubType, isEnabled: boolean) => {
    if (isEnabled) {
      // Enable the hub
      setEnabled((current) => (current ? [...current, type] : [type]))
      setDisabled((current) => current?.filter((t) => t !== type) ?? [])
    } else {
      // Disable the hub
      setDisabled((current) => (current ? [...current, type] : [type]))
      setEnabled((current) => current?.filter((t) => t !== type) ?? [])
    }
  }

  const handleSave = async () => {
    if (!enabled || !disabled) return

    const payload = {
      context,
      disabledHubTypes: [...disabled, ...hiddenDisabled],
      enabledHubTypes: [...enabled, ...hiddenEnabled],
      librarySectionId:
        context !== HubContext.Home ? (libraryId ?? null) : null,
      metadataType:
        context === HubContext.ItemDetail ? (metadataType ?? null) : null,
    }

    await updateConfiguration({ variables: { input: payload } })
    await refetch()
  }

  const hubLookup = new Map(
    availableHubOptions.map((option) => [option.type, option]),
  )
  const hasHiddenTypes = hiddenEnabled.length > 0 || hiddenDisabled.length > 0

  const readyForSave =
    (context === HubContext.Home ||
      (context === HubContext.LibraryDiscover && !!libraryId) ||
      context === HubContext.ItemDetail) &&
    (context !== HubContext.ItemDetail || metadataType)

  const allItems = allHubTypes
    .map((type) => hubLookup.get(type))
    .filter((option): option is HubOption => !!option)

  const isHubEnabled = (type: HubType) => enabled?.includes(type) ?? false

  const headerActions = (
    <>
      <Select
        onValueChange={(value) => {
          setContext(value as HubContext)
        }}
        value={context}
      >
        <SelectTrigger className="w-44">
          <SelectValue placeholder="Context" />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value={HubContext.Home}>Home</SelectItem>
          <SelectItem value={HubContext.LibraryDiscover}>
            Discover (Library)
          </SelectItem>
          <SelectItem value={HubContext.ItemDetail}>Item Detail</SelectItem>
        </SelectContent>
      </Select>
      {(context === HubContext.LibraryDiscover ||
        context === HubContext.ItemDetail) && (
        <Select
          onValueChange={(value) => {
            setLibraryId(value === '__all__' ? undefined : value)
          }}
          value={libraryId ?? '__all__'}
        >
          <SelectTrigger className="w-52">
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
      )}
      {context === HubContext.ItemDetail && (
        <Select
          onValueChange={(value) => {
            setMetadataType((value as MetadataType) || undefined)
          }}
          value={metadataType ?? ''}
        >
          <SelectTrigger className="w-52">
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
      )}
      <Button disabled={!readyForSave || saving} onClick={handleSave}>
        {saving ? 'Saving...' : 'Save'}
      </Button>
    </>
  )

  return (
    <SettingsPageContainer maxWidth="sm">
      <SettingsPageHeader
        actions={headerActions}
        description="Configure which hubs are shown and their order"
        title="Hub Configuration"
      />

      <Card>
        <CardHeader>
          <CardTitle>Hubs</CardTitle>
        </CardHeader>
        <CardContent>
          {(loadingConfiguration || enabled === null) && (
            <p className="text-sm text-muted-foreground">Loading…</p>
          )}
          {!loadingConfiguration &&
            enabled !== null &&
            allItems.length === 0 && (
              <p className="text-sm text-muted-foreground">No hubs available</p>
            )}
          {enabled !== null && allItems.length > 0 && (
            <SortableList
              getEnabled={(hub) => isHubEnabled(hub.type)}
              getId={(hub) => hub.type}
              items={allItems}
              onOrderChange={(newOrder) => {
                handleHubOrderChange(newOrder.map((hub) => hub.type))
              }}
              onToggle={(hub, isEnabled) => {
                toggleHub(hub.type, isEnabled)
              }}
              renderItem={(hub) => (
                <div>
                  <p className="font-medium">{hub.title}</p>
                  {hub.description && (
                    <p className="text-sm text-muted-foreground">
                      {hub.description}
                    </p>
                  )}
                </div>
              )}
            />
          )}
          {hasHiddenTypes && enabled !== null && (
            <p className="mt-4 text-sm text-muted-foreground">
              Hidden hub types present in configuration will be kept when
              saving.
            </p>
          )}
        </CardContent>
      </Card>
    </SettingsPageContainer>
  )
}

export default HubConfigurationPage
