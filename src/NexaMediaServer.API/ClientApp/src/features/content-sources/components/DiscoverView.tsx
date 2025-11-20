import { useQuery } from '@apollo/client/react'

import { HubRow, LibraryDiscoverHubDefinitionsQuery } from '@/features/hubs'
import { QueryErrorDisplay } from '@/shared/components/QueryErrorDisplay'

type DiscoverViewProps = Readonly<{
  librarySectionId: string
}>

export function DiscoverView({ librarySectionId }: DiscoverViewProps) {
  const {
    data: discoverData,
    error: discoverError,
    loading: discoverLoading,
    refetch: refetchDiscover,
  } = useQuery(LibraryDiscoverHubDefinitionsQuery, {
    skip: !librarySectionId,
    variables: { librarySectionId },
  })

  if (discoverLoading) {
    return (
      <div className="flex min-h-[50vh] items-center justify-center">
        <div className="text-muted-foreground">Loading...</div>
      </div>
    )
  }

  if (discoverError) {
    return (
      <QueryErrorDisplay
        error={discoverError}
        onRetry={() => {
          void refetchDiscover()
        }}
        title="Error loading discover hubs"
      />
    )
  }

  const definitions = discoverData?.libraryDiscoverHubDefinitions ?? []

  if (definitions.length === 0) {
    return (
      <div
        className={`
          flex min-h-[50vh] flex-col items-center justify-center gap-4
        `}
      >
        <p className="text-muted-foreground">
          No discover hubs available for this library.
        </p>
      </div>
    )
  }

  return (
    <div className="flex w-full flex-col gap-4 p-8">
      {definitions.map((definition) => (
        <HubRow
          definition={definition}
          key={definition.key}
          librarySectionId={librarySectionId}
        />
      ))}
    </div>
  )
}
