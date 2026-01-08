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
      playlistIndex
      playlistTotalCount
      shuffle
      repeat
      currentItemId
      currentItemMetadataType
      currentItemTitle
      currentItemOriginalTitle
      currentItemParentTitle
      currentItemThumbUrl
      currentItemParentThumbUrl
    }
  }
`)

/** Options for starting playback */
export interface StartPlaybackOptions {
  /** The item to start playing */
  item: StartableItem
  /**
   * The originator/container ID for playlist-based playback.
   * For album tracks, this is the album ID. For episodes, this is the season/show ID.
   */
  originatorId?: string
  /**
   * The playlist type. Defaults to 'single' for single-item playback.
   * Options: 'single', 'album', 'season', 'show', 'artist', 'library', 'explicit'
   */
  playlistType?: string
  /** Whether repeat mode should be enabled */
  repeat?: boolean
  /** Whether shuffle mode should be enabled */
  shuffle?: boolean
}

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
    async (
      itemOrOptions: StartableItem | StartPlaybackOptions,
    ): Promise<void> => {
      // Support both legacy single-item calls and new options-based calls
      const options: StartPlaybackOptions =
        'item' in itemOrOptions ? itemOrOptions : { item: itemOrOptions }

      const {
        item,
        originatorId,
        playlistType = 'single',
        repeat = false,
        shuffle = false,
      } = options

      try {
        const result = await mutate({
          variables: {
            input: {
              capability: buildPlaybackCapabilityInput(
                playbackState.capabilityProfileVersion,
              ),
              capabilityProfileVersion: playbackState.capabilityProfileVersion,
              itemId: item.id,
              originatorId,
              playlistType,
              repeat,
              shuffle,
            },
          },
        })

        const payload = result.data?.startPlayback
        if (!payload) {
          return
        }

        const originatorMetadataType =
          (payload.currentItemMetadataType as
            | Item['metadataType']
            | undefined) ?? item.metadataType

        const originator: StartableItem & {
          originalTitle?: null | string
          parentThumbUri?: null | string
          parentTitle?: null | string
        } = {
          id: payload.currentItemId ?? item.id,
          metadataType: originatorMetadataType,
          originalTitle: payload.currentItemOriginalTitle,
          parentThumbUri: payload.currentItemParentThumbUrl,
          parentTitle: payload.currentItemParentTitle,
          thumbUri: payload.currentItemThumbUrl ?? item.thumbUri,
          title: payload.currentItemTitle ?? item.title,
        }

        startPlayback({
          capabilityProfileVersion: payload.capabilityProfileVersion,
          capabilityVersionMismatch: payload.capabilityVersionMismatch,
          originator,
          playbackSessionId: String(payload.playbackSessionId),
          playbackUrl: payload.playbackUrl,
          playlistGeneratorId: String(payload.playlistGeneratorId),
          playlistIndex: payload.playlistIndex,
          playlistRepeat: payload.repeat,
          playlistShuffle: payload.shuffle,
          playlistTotalCount: payload.playlistTotalCount,
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
