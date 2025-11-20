import type { HubDefinition } from '@/shared/api/graphql/graphql'

type HubTimelineRowProps = Readonly<{
  definition: Pick<HubDefinition, 'key' | 'title' | 'type'>
  librarySectionId?: string
  metadataItemId?: string
}>

/**
 * Placeholder component for the Timeline hub widget.
 * Renders items in a vertical timeline from most recent to least recent.
 */
export function HubTimelineRow({ definition }: HubTimelineRowProps) {
  return (
    <div className="min-w-0 py-4">
      <h2 className="mb-4 text-lg font-semibold text-foreground">
        {definition.title}
      </h2>
      <div
        className={`
          rounded-lg border border-dashed border-muted-foreground/30 bg-muted/20
          p-8 text-center
        `}
      >
        <p className="text-muted-foreground">Timeline widget coming soon</p>
        <p className="mt-2 text-xs text-muted-foreground/60">
          Hub type: {definition.type}
        </p>
      </div>
    </div>
  )
}
