import { useQuery } from '@apollo/client/react'
import { useDocumentTitle } from '@uidotdev/usehooks'
import { useMemo } from 'react'

import { HomeHubDefinitionsQuery, HubRow } from '@/features/hubs'
import { QueryErrorDisplay } from '@/shared/components/QueryErrorDisplay'
import { useLayoutSlot } from '@/shared/hooks'

/**
 * The home page showing aggregated hubs from all accessible libraries.
 */
export function HomePage() {
  const { data, error, loading, refetch } = useQuery(HomeHubDefinitionsQuery)

  // Memoize header content to prevent useLayoutSlot from recreating on every render
  const headerContent = useMemo(
    () => (
      <div className="flex flex-row items-center gap-2">
        <h1 className="text-base font-semibold">Home</h1>
      </div>
    ),
    [],
  )

  useDocumentTitle('Nexa')

  useLayoutSlot('header', headerContent)

  if (loading) {
    return (
      <div className="flex min-h-[50vh] items-center justify-center">
        <div className="text-muted-foreground">Loading...</div>
      </div>
    )
  }

  if (error) {
    return (
      <QueryErrorDisplay
        error={error}
        onRetry={() => {
          void refetch()
        }}
        title="Error loading hubs"
      />
    )
  }

  const definitions = data?.homeHubDefinitions ?? []

  if (definitions.length === 0) {
    return (
      <div
        className={`
          flex min-h-[50vh] flex-col items-center justify-center gap-4
        `}
      >
        <h1 className="text-2xl font-bold">Welcome to Nexa</h1>
        <p className="text-muted-foreground">
          Add a library to get started with your media collection.
        </p>
      </div>
    )
  }

  return (
    <div className="flex w-full flex-col gap-4 p-8">
      {definitions.map((definition) => (
        <HubRow definition={definition} key={definition.key} />
      ))}
    </div>
  )
}
