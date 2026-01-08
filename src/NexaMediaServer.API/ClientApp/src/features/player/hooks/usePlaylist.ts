import { useMutation, useQuery } from '@apollo/client/react'
import { useAtomValue, useSetAtom } from 'jotai'
import { useCallback, useEffect, useRef } from 'react'

import type {
  PlaylistChunkInput,
  PlaylistChunkPayload,
  PlaylistItemPayload,
  PlaylistJumpMutation,
  PlaylistNextMutation,
  PlaylistPreviousMutation,
} from '@/shared/api/graphql/graphql'

import { graphql } from '@/shared/api/graphql'
import { handleErrorStandalone } from '@/shared/hooks/useErrorHandler'

import { playbackStateAtom } from '../store'
import { playlistCacheAtom } from './usePlaylistCache'

type PlaylistNavigationResult =
  | PlaylistJumpMutation['playlistJump']
  | PlaylistNextMutation['playlistNext']
  | PlaylistPreviousMutation['playlistPrevious']

// GraphQL Documents
const PlaylistChunkDocument = graphql(`
  query PlaylistChunk($input: PlaylistChunkInput!) {
    playlistChunk(input: $input) {
      playlistGeneratorId
      items {
        itemEntryId
        itemId
        index
        served
        title
        metadataType
        durationMs
        playbackUrl
        thumbUri
        parentTitle
        subtitle
        primaryPerson {
          id
          title
          metadataType
        }
      }
      currentIndex
      totalCount
      hasMore
      shuffle
      repeat
    }
  }
`)

const PlaylistNextDocument = graphql(`
  mutation PlaylistNext($input: PlaylistNavigateInput!) {
    playlistNext(input: $input) {
      success
      currentItem {
        itemEntryId
        itemId
        index
        served
        title
        metadataType
        durationMs
        thumbUri
        parentTitle
        subtitle
        primaryPerson {
          id
          title
          metadataType
        }
      }
      shuffle
      repeat
      currentIndex
      totalCount
    }
  }
`)

const PlaylistPreviousDocument = graphql(`
  mutation PlaylistPrevious($input: PlaylistNavigateInput!) {
    playlistPrevious(input: $input) {
      success
      currentItem {
        itemEntryId
        itemId
        index
        served
        title
        metadataType
        durationMs
        thumbUri
        parentTitle
        subtitle
        primaryPerson {
          id
          title
          metadataType
        }
      }
      shuffle
      repeat
      currentIndex
      totalCount
    }
  }
`)

const PlaylistJumpDocument = graphql(`
  mutation PlaylistJump($input: PlaylistJumpInput!) {
    playlistJump(input: $input) {
      success
      currentItem {
        itemEntryId
        itemId
        index
        served
        title
        metadataType
        durationMs
        playbackUrl
        thumbUri
        parentTitle
        subtitle
        primaryPerson {
          id
          title
          metadataType
        }
      }
      shuffle
      repeat
      currentIndex
      totalCount
    }
  }
`)

const PlaylistSetShuffleDocument = graphql(`
  mutation PlaylistSetShuffle($input: PlaylistModeInput!) {
    playlistSetShuffle(input: $input) {
      success
      shuffle
      repeat
      currentIndex
      totalCount
    }
  }
`)

const PlaylistSetRepeatDocument = graphql(`
  mutation PlaylistSetRepeat($input: PlaylistModeInput!) {
    playlistSetRepeat(input: $input) {
      success
      shuffle
      repeat
      currentIndex
      totalCount
    }
  }
`)

export interface UsePlaylistOptions {
  /** Number of items to fetch in each chunk */
  chunkSize?: number
}

export interface UsePlaylistResult {
  /** Current playlist chunk data */
  chunk: null | PlaylistChunkPayload
  /** Current item in the playlist */
  currentItem: null | PlaylistItemPayload
  /** Whether there are more items after current position */
  hasNext: boolean
  /** Whether there are items before current position */
  hasPrevious: boolean
  /** Whether repeat mode is enabled */
  isRepeat: boolean
  /** Whether shuffle mode is enabled */
  isShuffle: boolean
  /** Jump to a specific index in the playlist */
  jumpTo: (index: number) => Promise<null | PlaylistNavigationResult>
  /** Whether chunk is loading */
  loading: boolean
  /** Navigate to next item */
  next: () => Promise<null | PlaylistNavigationResult>
  /** Active playlist generator id (from playback state) */
  playlistGeneratorId: null | string
  /** Total number of items in playlist */
  playlistIndex: number
  /** Navigate to previous item */
  previous: () => Promise<null | PlaylistNavigationResult>
  /** Refetch playlist chunk */
  refetch: (variables?: { input: PlaylistChunkInput }) => Promise<unknown>
  /** Toggle repeat mode */
  setRepeat: (enabled: boolean) => Promise<void>
  /** Toggle shuffle mode */
  setShuffle: (enabled: boolean) => Promise<void>
  /** Total items in the playlist */
  totalCount: number
}

/**
 * Hook to manage playlist navigation and state.
 * Provides methods for next/previous navigation, shuffle/repeat toggling,
 * and fetching playlist chunks.
 */
export function usePlaylist(
  options: UsePlaylistOptions = {},
): UsePlaylistResult {
  const { chunkSize = 20 } = options
  const playbackState = useAtomValue(playbackStateAtom)
  const setPlaybackState = useSetAtom(playbackStateAtom)
  const setPlaylistCache = useSetAtom(playlistCacheAtom)
  const lastCacheUpdateRef = useRef<null | string>(null)

  const playlistGeneratorId = playbackState.playlistGeneratorId

  // Fetch current playlist chunk
  // Use cache-and-network to show cached data immediately while fetching fresh data
  const { data, loading, refetch } = useQuery(PlaylistChunkDocument, {
    fetchPolicy: 'cache-and-network',
    skip: !playlistGeneratorId,
    variables: {
      input: {
        limit: chunkSize,
        playlistGeneratorId: playlistGeneratorId ?? '',
        startIndex: Math.max(
          0,
          (playbackState.playlistIndex ?? 0) - Math.floor(chunkSize / 2),
        ),
      },
    },
  })

  // Mutations
  const [nextMutation] = useMutation(PlaylistNextDocument)
  const [previousMutation] = useMutation(PlaylistPreviousDocument)
  const [jumpMutation] = useMutation(PlaylistJumpDocument)
  const [shuffleMutation] = useMutation(PlaylistSetShuffleDocument)
  const [repeatMutation] = useMutation(PlaylistSetRepeatDocument)

  // Only use chunk data if it matches the current playlist generator to avoid stale cache issues
  const chunk =
    data?.playlistChunk &&
    data.playlistChunk.playlistGeneratorId === playlistGeneratorId
      ? data.playlistChunk
      : null
  const currentIndex = playbackState.playlistIndex ?? 0
  const totalCount = playbackState.playlistTotalCount ?? chunk?.totalCount ?? 0

  // Update playback state with chunk data when it arrives
  useEffect(() => {
    if (chunk && chunk.totalCount !== playbackState.playlistTotalCount) {
      setPlaybackState((prev) => ({
        ...prev,
        playlistTotalCount: chunk.totalCount,
      }))
    }
  }, [chunk, playbackState.playlistTotalCount, setPlaybackState])

  // Populate shared playlist cache when chunk data arrives
  // This allows PlaylistDrawer to show items immediately on open
  useEffect(() => {
    if (!chunk || !playlistGeneratorId) return

    const validItems = chunk.items.filter(
      (item): item is PlaylistItemPayload =>
        item !== null && item !== undefined,
    )
    if (validItems.length === 0) return

    // Create a cache key to dedupe updates
    const cacheKey = `${playlistGeneratorId}:${validItems.map((i) => i.index).join(',')}`
    if (lastCacheUpdateRef.current === cacheKey) return
    lastCacheUpdateRef.current = cacheKey

    setPlaylistCache((prev) => {
      const next = new Map(prev)
      const existing = new Map(
        next.get(playlistGeneratorId) ?? new Map<number, PlaylistItemPayload>(),
      )

      for (const item of validItems) {
        existing.set(item.index, item)
      }

      next.set(playlistGeneratorId, existing)
      return next
    })
  }, [chunk, playlistGeneratorId, setPlaylistCache])

  // Find current item in chunk (filter out null items from sparse array)
  const currentItem =
    chunk?.items.find(
      (item): item is PlaylistItemPayload => item?.index === currentIndex,
    ) ?? null

  const updatePlaylistState = useCallback(
    (
      payload: Pick<
        PlaylistNavigationResult,
        'currentIndex' | 'repeat' | 'shuffle' | 'totalCount'
      >,
    ) => {
      setPlaybackState((prev) => ({
        ...prev,
        playlistIndex: payload.currentIndex,
        playlistRepeat: payload.repeat,
        playlistShuffle: payload.shuffle,
        playlistTotalCount: payload.totalCount,
      }))
    },
    [setPlaybackState],
  )

  const next =
    useCallback(async (): Promise<null | PlaylistNavigationResult> => {
      if (!playlistGeneratorId) return null

      try {
        const result = await nextMutation({
          variables: {
            input: { playlistGeneratorId },
          },
        })

        const payload = result.data?.playlistNext
        if (payload?.success) {
          updatePlaylistState(payload)
          return payload
        }
        return null
      } catch (error) {
        handleErrorStandalone(error, { context: 'usePlaylist.next' })
        return null
      }
    }, [playlistGeneratorId, nextMutation, updatePlaylistState])

  const previous =
    useCallback(async (): Promise<null | PlaylistNavigationResult> => {
      if (!playlistGeneratorId) return null

      try {
        const result = await previousMutation({
          variables: {
            input: { playlistGeneratorId },
          },
        })

        const payload = result.data?.playlistPrevious
        if (payload?.success) {
          updatePlaylistState(payload)
          return payload
        }
        return null
      } catch (error) {
        handleErrorStandalone(error, { context: 'usePlaylist.previous' })
        return null
      }
    }, [playlistGeneratorId, previousMutation, updatePlaylistState])

  const jumpTo = useCallback(
    async (index: number): Promise<null | PlaylistNavigationResult> => {
      if (!playlistGeneratorId) return null

      try {
        const result = await jumpMutation({
          variables: {
            input: {
              index,
              playlistGeneratorId,
            },
          },
        })

        const payload = result.data?.playlistJump
        if (payload?.success) {
          updatePlaylistState(payload)

          // Update playback state with new item data for ImageViewer etc.
          if (payload.currentItem) {
            const item = payload.currentItem
            setPlaybackState((prev) => ({
              ...prev,
              mediaId: item.itemId,
              originator: prev.originator
                ? {
                    ...prev.originator,
                    id: item.itemId,
                    parentTitle:
                      item.parentTitle ?? prev.originator.parentTitle,
                    thumbUri: item.thumbUri ?? prev.originator.thumbUri ?? null,
                    title: item.title,
                  }
                : prev.originator,
              playbackUrl: item.playbackUrl ?? prev.playbackUrl,
            }))
          }
          return payload
        }
        return null
      } catch (error) {
        handleErrorStandalone(error, { context: 'usePlaylist.jumpTo' })
        return null
      }
    },
    [playlistGeneratorId, jumpMutation, setPlaybackState, updatePlaylistState],
  )

  const shuffleTimeoutRef = useRef<null | ReturnType<typeof setTimeout>>(null)

  const setShuffle = useCallback(
    async (enabled: boolean): Promise<void> => {
      if (!playlistGeneratorId) return

      // Debounce rapid shuffle toggles
      if (shuffleTimeoutRef.current) {
        clearTimeout(shuffleTimeoutRef.current)
      }

      return new Promise<void>((resolve, reject) => {
        shuffleTimeoutRef.current = setTimeout(async () => {
          try {
            const result = await shuffleMutation({
              variables: {
                input: {
                  enabled,
                  playlistGeneratorId,
                },
              },
            })

            const payload = result.data?.playlistSetShuffle
            if (payload?.success) {
              updatePlaylistState(payload)

              // Clear cached items to force reload with new server order
              setPlaylistCache((prev) => {
                if (!prev.has(playlistGeneratorId)) return prev
                const next = new Map(prev)
                next.delete(playlistGeneratorId)
                return next
              })

              // Refetch visible range (±20 items around current position)
              const currentIdx = payload.currentIndex ?? 0
              const startIndex = Math.max(0, currentIdx - 20)
              await refetch({
                input: {
                  limit: 40,
                  playlistGeneratorId,
                  startIndex,
                },
              })
            }
            resolve()
          } catch (error) {
            handleErrorStandalone(error, { context: 'usePlaylist.setShuffle' })
            reject(error)
          }
        }, 300)
      })
    },
    [
      playlistGeneratorId,
      shuffleMutation,
      updatePlaylistState,
      setPlaylistCache,
      refetch,
    ],
  )

  const setRepeat = useCallback(
    async (enabled: boolean): Promise<void> => {
      if (!playlistGeneratorId) return

      try {
        const result = await repeatMutation({
          variables: {
            input: {
              enabled,
              playlistGeneratorId,
            },
          },
        })

        const payload = result.data?.playlistSetRepeat
        if (payload?.success) {
          updatePlaylistState(payload)
        }
      } catch (error) {
        handleErrorStandalone(error, { context: 'usePlaylist.setRepeat' })
      }
    },
    [playlistGeneratorId, repeatMutation, updatePlaylistState],
  )

  return {
    chunk,
    currentItem,
    hasNext:
      currentIndex < totalCount - 1 || (playbackState.playlistRepeat ?? false),
    hasPrevious: currentIndex > 0,
    isRepeat: playbackState.playlistRepeat ?? chunk?.repeat ?? false,
    isShuffle: playbackState.playlistShuffle ?? chunk?.shuffle ?? false,
    jumpTo,
    loading,
    next,
    playlistGeneratorId: playlistGeneratorId ?? null,
    playlistIndex: currentIndex,
    previous,
    refetch,
    setRepeat,
    setShuffle,
    totalCount,
  }
}
