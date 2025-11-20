import { useMutation } from '@apollo/client/react'
import { useAtomValue } from 'jotai'
import { useCallback } from 'react'

import type {
  Item,
  StartPlaybackMutation,
  StartPlaybackMutationVariables,
} from '@/shared/api/graphql/graphql'

import { graphql } from '@/shared/api/graphql'
import { handleErrorStandalone } from '@/shared/hooks/useErrorHandler'
import { buildPlaybackCapabilityInput } from '@/shared/lib/playbackCapabilities'

import { playbackStateAtom } from '../store'
import { usePlayback } from './usePlayback'

const StartPlaybackDocument = graphql(`
  mutation StartPlayback($input: PlaybackStartInput!) {
    startPlayback(input: $input) {
      playbackSessionId
      playlistGeneratorId
      capabilityProfileVersion
      streamPlanJson
      playbackUrl
      trickplayUrl
      durationMs
      capabilityVersionMismatch
    }
  }
`)

type StartableItem = Pick<Item, 'id' | 'metadataType' | 'thumbUri' | 'title'>

/**
 * Shared startPlayback mutation + local state wiring for multiple callers.
 */
export function useStartPlayback() {
  const { startPlayback } = usePlayback()
  const playbackState = useAtomValue(playbackStateAtom)
  const [mutate, { loading }] = useMutation<
    StartPlaybackMutation,
    StartPlaybackMutationVariables
  >(StartPlaybackDocument)

  const startPlaybackForItem = useCallback(
    async (item: StartableItem): Promise<void> => {
      try {
        const result = await mutate({
          variables: {
            input: {
              capability: buildPlaybackCapabilityInput(
                playbackState.capabilityProfileVersion,
              ),
              capabilityProfileVersion: playbackState.capabilityProfileVersion,
              itemId: item.id,
            },
          },
        })

        const payload = result.data?.startPlayback
        if (!payload) {
          return
        }

        startPlayback({
          capabilityProfileVersion: payload.capabilityProfileVersion,
          capabilityVersionMismatch: payload.capabilityVersionMismatch,
          originator: item,
          playbackSessionId: String(payload.playbackSessionId),
          playbackUrl: payload.playbackUrl,
          playlistGeneratorId: String(payload.playlistGeneratorId),
          serverDuration:
            payload.durationMs !== null && payload.durationMs !== undefined
              ? Number(payload.durationMs)
              : undefined,
          streamPlanJson: payload.streamPlanJson,
          trickplayUrl: payload.trickplayUrl,
        })
      } catch (error) {
        handleErrorStandalone(error, { context: 'useStartPlayback' })
      }
    },
    [mutate, playbackState.capabilityProfileVersion, startPlayback],
  )

  return { startPlaybackForItem, startPlaybackLoading: loading }
}
