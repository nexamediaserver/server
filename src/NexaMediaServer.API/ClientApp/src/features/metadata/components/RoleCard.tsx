import { Link } from '@tanstack/react-router'
import { useEffect, useMemo, useState } from 'react'

import type { Role } from '@/shared/api/graphql/graphql'

import { ITEM_CARD_MAX_WIDTH_PX } from '@/features/content-sources/lib/itemCardSizing'
import { getImageTranscodeUrl } from '@/shared/lib/images'
import { cn } from '@/shared/lib/utils'

type RoleCardProps = Readonly<{
  cardWidthPx?: number
  className?: string
  librarySectionId: string
  renderWidthPx?: number
  role: Pick<Role, 'name' | 'personId' | 'relationship' | 'thumbUrl'>
}>

export function RoleCard({
  cardWidthPx = ITEM_CARD_MAX_WIDTH_PX,
  className,
  librarySectionId,
  renderWidthPx,
  role,
}: RoleCardProps) {
  const { name, personId, relationship, thumbUrl } = role
  const resolvedWidthPx = useMemo(
    () => renderWidthPx ?? cardWidthPx,
    [cardWidthPx, renderWidthPx],
  )
  const initial = name
    .trim()
    .charAt(0)
    .toUpperCase()
    .replace(/[^A-Z0-9]/i, '?')

  const [imageUrl, setImageUrl] = useState<string | undefined>(undefined)

  useEffect(() => {
    let cancelled = false
    if (!thumbUrl) {
      setImageUrl(undefined)
      return () => {
        cancelled = true
      }
    }

    void getImageTranscodeUrl(thumbUrl, {
      height: Math.round(resolvedWidthPx),
      quality: 90,
      width: Math.round(resolvedWidthPx),
    }).then((url) => {
      if (!cancelled) {
        setImageUrl(url)
      }
    })

    return () => {
      cancelled = true
    }
  }, [thumbUrl, resolvedWidthPx])

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
      params={{ contentSourceId: librarySectionId, metadataItemId: personId }}
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
          const displayUrl = imageUrl ?? thumbUrl
          if (displayUrl) {
            return (
              <img
                alt={name}
                className={cn(
                  'absolute inset-0 h-full w-full object-cover',
                  'transition-transform duration-300 ease-out',
                )}
                loading="lazy"
                src={displayUrl}
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
          {name || 'Unknown'}
        </p>
        {relationship ? (
          <p className={`line-clamp-1 text-sm text-muted-foreground`}>
            {relationship}
          </p>
        ) : null}
      </div>
    </Link>
  )
}
