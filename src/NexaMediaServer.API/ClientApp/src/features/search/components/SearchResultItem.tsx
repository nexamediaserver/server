import { Link } from '@tanstack/react-router'

import type { SearchQuery } from '@/shared/api/graphql/graphql'

import { MetadataTypeIcon } from '@/domain/components'
import {
  getMetadataTypeLabel,
  isPosterAspect,
} from '@/features/search/lib/utils'
import { Image } from '@/shared/components/Image'
import { cn } from '@/shared/lib/utils'

export type SearchResultData = SearchQuery['search'][number]

export interface SearchResultItemProps {
  /** Additional class names */
  className?: string
  /** Callback when the result is clicked/navigated to */
  onNavigate?: () => void
  /** The search result data */
  result: SearchResultData
}

/**
 * A single search result item displaying thumbnail, title, type, and year.
 * Used in both sidebar search and mobile search sheet.
 */
export function SearchResultItem({
  className,
  onNavigate,
  result,
}: SearchResultItemProps) {
  const posterAspect = isPosterAspect(result.metadataType)

  return (
    <Link
      className={cn(
        `
          flex items-center gap-3 rounded-md p-2 transition-colors
          hover:bg-accent
          active:bg-accent/80
        `,
        className,
      )}
      onClick={onNavigate}
      params={{
        contentSourceId: result.librarySectionId,
        metadataItemId: result.id,
      }}
      to="/section/$contentSourceId/details/$metadataItemId"
    >
      {result.thumbUri ? (
        <Image
          className={cn(
            'shrink-0 rounded bg-muted',
            posterAspect ? 'h-12 w-8' : 'size-10',
          )}
          height={posterAspect ? 48 : 40}
          imageUri={result.thumbUri}
          width={posterAspect ? 32 : 40}
        />
      ) : (
        <div
          className={cn(
            'flex shrink-0 items-center justify-center rounded bg-muted',
            posterAspect ? 'h-12 w-8' : 'size-10',
          )}
        >
          <MetadataTypeIcon
            className="size-5 text-muted-foreground"
            item={{ metadataType: result.metadataType }}
          />
        </div>
      )}
      <div className="flex min-w-0 flex-col">
        <span className="truncate text-sm font-medium">{result.title}</span>
        <span className="truncate text-xs text-muted-foreground">
          {getMetadataTypeLabel(result.metadataType)}
          {result.year ? ` • ${String(result.year)}` : ''}
        </span>
      </div>
    </Link>
  )
}
