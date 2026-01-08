import { Link } from '@tanstack/react-router'
import { useMemo } from 'react'

import type { Item } from '@/shared/api/graphql/graphql'

import { ITEM_CARD_MAX_WIDTH_PX } from '@/features/content-sources/lib/itemCardSizing'
import { Image } from '@/shared/components/Image'
import { cn } from '@/shared/lib/utils'

type RoleCardProps = Readonly<{
  cardWidthPx?: number
  className?: string
  librarySectionId: string
  renderWidthPx?: number
  role: Pick<Item, 'context' | 'id' | 'thumbHash' | 'thumbUri' | 'title'>
}>

export function RoleCard({
  cardWidthPx = ITEM_CARD_MAX_WIDTH_PX,
  className,
  librarySectionId,
  renderWidthPx,
  role,
}: RoleCardProps) {
  const { context, id, thumbHash, thumbUri, title } = role
  const resolvedWidthPx = useMemo(
    () => renderWidthPx ?? cardWidthPx,
    [cardWidthPx, renderWidthPx],
  )
  const initial = title
    .trim()
    .charAt(0)
    .toUpperCase()
    .replace(/[^A-Z0-9]/i, '?')

  return (
    <Link
      className={cn(
        'group block space-y-2 select-none',
        `
          focus:outline-none
          focus-visible:ring-2 focus-visible:ring-purple-500
        `,
        className,
      )}
      params={{ contentSourceId: librarySectionId, metadataItemId: id }}
      style={{ width: resolvedWidthPx }}
      to="/section/$contentSourceId/details/$metadataItemId"
    >
      <div
        className={cn(
          'relative aspect-square w-full overflow-hidden rounded-full',
          'bg-stone-800',
        )}
      >
        {(() => {
          if (thumbUri) {
            return (
              <Image
                alt={title}
                className={cn(
                  'absolute inset-0 h-full w-full object-cover',
                  'transition-transform duration-300 ease-out',
                )}
                height={resolvedWidthPx}
                imageUri={thumbUri}
                thumbHash={thumbHash ?? undefined}
                width={resolvedWidthPx}
              />
            )
          }

          return (
            <div
              className={cn(
                'absolute inset-0 flex items-center justify-center',
                'bg-stone-800',
              )}
            >
              <span className="text-xl font-semibold text-stone-300">
                {initial}
              </span>
            </div>
          )
        })()}

        <div
          className={cn(
            'pointer-events-none absolute inset-0 rounded-full',
            `
              border-2 border-primary opacity-0 transition-opacity duration-200
              ease-in-out
            `,
            'group-hover:opacity-100',
          )}
        />
      </div>

      <div className="space-y-0.5 px-0.5 text-center">
        <p className="line-clamp-1 font-medium text-foreground">
          {title || 'Unknown'}
        </p>
        {context ? (
          <p className={`line-clamp-1 text-sm text-muted-foreground`}>
            {context}
          </p>
        ) : null}
      </div>
    </Link>
  )
}
