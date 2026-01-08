import { atom, useAtom } from 'jotai'
import { useCallback, useMemo } from 'react'

import type { PlaylistItemPayload } from '@/shared/api/graphql/graphql'

export const playlistCacheAtom = atom(
  new Map<string, Map<number, PlaylistItemPayload>>(),
)

interface PlaylistCacheResult {
  cachedItems: Map<number, PlaylistItemPayload>
  resetCache: () => void
  updateCache: (items: PlaylistItemPayload[]) => void
}

/**
 * Shared cache for playlist chunks keyed by playlistGeneratorId so the drawer
 * keeps its items across unmounts (e.g., when the sheet closes).
 */
export function usePlaylistCache(
  playlistGeneratorId: null | string,
): PlaylistCacheResult {
  const [cache, setCache] = useAtom(playlistCacheAtom)

  const cachedItems = useMemo(() => {
    if (!playlistGeneratorId) return new Map<number, PlaylistItemPayload>()
    return (
      cache.get(playlistGeneratorId) ?? new Map<number, PlaylistItemPayload>()
    )
  }, [cache, playlistGeneratorId])

  const updateCache = useCallback(
    (items: PlaylistItemPayload[]) => {
      if (!playlistGeneratorId || items.length === 0) return

      setCache((prev) => {
        const next = new Map(prev)
        const existing = new Map(
          next.get(playlistGeneratorId) ??
            new Map<number, PlaylistItemPayload>(),
        )

        for (const item of items) {
          existing.set(item.index, item)
        }

        next.set(playlistGeneratorId, existing)
        return next
      })
    },
    [playlistGeneratorId, setCache],
  )

  const resetCache = useCallback(() => {
    if (!playlistGeneratorId) return

    setCache((prev) => {
      if (!prev.has(playlistGeneratorId)) return prev
      const next = new Map(prev)
      next.delete(playlistGeneratorId)
      return next
    })
  }, [playlistGeneratorId, setCache])

  return { cachedItems, resetCache, updateCache }
}
