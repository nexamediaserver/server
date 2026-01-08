import { useApolloClient } from '@apollo/client/react'
import { useAtomValue } from 'jotai'
import { useCallback } from 'react'

import { graphql } from '@/shared/api/graphql'
import { handleErrorStandalone } from '@/shared/hooks/useErrorHandler'
import { buildPlaybackCapabilityInput } from '@/shared/lib/playbackCapabilities'

import { playbackStateAtom } from '../store'
import { usePlayback } from './usePlayback'
import { useStopPlayback } from './useStopPlayback'

const DecidePlaybackDocument = graphql(`
  mutation DecidePlaybackNavigation($input: PlaybackDecisionInput!) {
    decidePlayback(input: $input) {
      action
      streamPlanJson
      nextItemId
      nextItemTitle
      nextItemOriginalTitle
      nextItemParentTitle
      nextItemThumbUrl
      playbackUrl
      trickplayUrl
      capabilityProfileVersion
      capabilityVersionMismatch
    }
  }
`)

export interface UsePlaylistNavigationResult {
  /** Whether there's a next item available */
  hasNext: boolean
  /** Whether there's a previous item available */
  hasPrevious: boolean
  /** Jump to a specific index in the playlist */
  jumpTo: (index: number) => Promise<void>
  /** Navigate to the next item in the playlist */
  navigateNext: () => Promise<void>
  /** Navigate to the previous item in the playlist */
  navigatePrevious: () => Promise<void>
}

/**
 * Hook to handle playlist navigation (next/previous) for all media types.
 * Uses the decidePlayback mutation to request navigation from the server
 * and updates playback state accordingly.
 */
export function usePlaylistNavigation(): UsePlaylistNavigationResult {
  const apolloClient = useApolloClient()
  const playbackState = useAtomValue(playbackStateAtom)
  const { updatePlaybackState } = usePlayback()
  const { stopPlayback } = useStopPlayback()

  const currentIndex = playbackState.playlistIndex ?? 0
  const totalCount = playbackState.playlistTotalCount ?? 1
  const isRepeat = playbackState.playlistRepeat ?? false

  const hasNext = currentIndex < totalCount - 1 || isRepeat
  const hasPrevious = currentIndex > 0

  const navigate = useCallback(
    async (direction: 'jump' | 'next' | 'previous', jumpIndex?: number) => {
      if (!playbackState.playbackSessionId || !playbackState.originator?.id) {
        return
      }

      try {
        const result = await apolloClient.mutate({
          mutation: DecidePlaybackDocument,
          variables: {
            input: {
              capability: buildPlaybackCapabilityInput(
                playbackState.capabilityProfileVersion,
              ),
              capabilityProfileVersion: playbackState.capabilityProfileVersion,
              currentItemId:
                playbackState.mediaId ?? playbackState.originator.id,
              jumpIndex: direction === 'jump' ? jumpIndex : undefined,
              playbackSessionId: playbackState.playbackSessionId,
              progressMs: Math.round(playbackState.currentTime),
              status: direction,
            },
          },
        })

        const payload = result.data?.decidePlayback
        if (payload) {
          if (
            (payload.action === 'next' ||
              payload.action === 'previous' ||
              payload.action === 'jump') &&
            payload.playbackUrl &&
            payload.nextItemId
          ) {
            // Navigate to the target item
            let newIndex: number
            if (direction === 'jump' && jumpIndex !== undefined) {
              newIndex = jumpIndex
            } else {
              newIndex =
                direction === 'next' ? currentIndex + 1 : currentIndex - 1
            }

            // Update originator with the new track's info for display
            const currentOriginator = playbackState.originator
            updatePlaybackState({
              capabilityProfileVersion: payload.capabilityProfileVersion,
              capabilityVersionMismatch: payload.capabilityVersionMismatch,
              currentTime: 0,
              mediaId: payload.nextItemId,
              originator: {
                id: payload.nextItemId,
                metadataType: currentOriginator.metadataType,
                originalTitle: payload.nextItemOriginalTitle ?? undefined,
                parentThumbUri: currentOriginator.parentThumbUri,
                parentTitle: payload.nextItemParentTitle ?? undefined,
                thumbUri:
                  payload.nextItemThumbUrl ??
                  currentOriginator.thumbUri ??
                  null,
                title: payload.nextItemTitle ?? 'Unknown',
              },
              playbackUrl: payload.playbackUrl,
              playlistIndex: newIndex,
              serverDuration: undefined,
              streamOffset: 0,
              streamPlanJson: payload.streamPlanJson,
              trickplayUrl: payload.trickplayUrl,
            })
          } else if (payload.action === 'stop') {
            // End of playlist
            void stopPlayback()
          }
          // 'stay' action - do nothing, we're already on the right item
        }
      } catch (error) {
        handleErrorStandalone(error, {
          context: `usePlaylistNavigation.${direction}`,
        })
      }
    },
    [
      playbackState.playbackSessionId,
      playbackState.originator,
      playbackState.capabilityProfileVersion,
      playbackState.mediaId,
      playbackState.currentTime,
      apolloClient,
      currentIndex,
      updatePlaybackState,
      stopPlayback,
    ],
  )

  const jumpTo = useCallback(
    (index: number) => navigate('jump', index),
    [navigate],
  )
  const navigateNext = useCallback(() => navigate('next'), [navigate])
  const navigatePrevious = useCallback(() => navigate('previous'), [navigate])

  return {
    hasNext,
    hasPrevious,
    jumpTo,
    navigateNext,
    navigatePrevious,
  }
}
