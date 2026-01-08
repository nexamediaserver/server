import { gql } from '@apollo/client'
import { useApolloClient } from '@apollo/client/react'
import { useAtomValue, useSetAtom } from 'jotai'
import { useEffect, useRef } from 'react'

import { MediaDocument, MetadataType } from '@/shared/api/graphql/graphql'
import { handleErrorStandalone } from '@/shared/hooks/useErrorHandler'
import { buildPlaybackCapabilityInput } from '@/shared/lib/playbackCapabilities'

import { persistedPlaybackSessionAtom, playbackStateAtom } from '../store'
import { usePlayback } from './usePlayback'

const ResumePlaybackDocument = gql`
  mutation ResumePlayback($input: PlaybackResumeInput!) {
    resumePlayback(input: $input) {
      playbackSessionId
      currentItemId
      playlistGeneratorId
      playheadMs
      state
      capabilityProfileVersion
      capabilityVersionMismatch
      streamPlanJson
      playbackUrl
      trickplayUrl
      durationMs
    }
  }
`

/**
 * Resumes playback on initial load when a persisted playback session exists.
 */
export function useResumePlaybackOnLoad() {
  const apolloClient = useApolloClient()
  const persistedSession = useAtomValue(persistedPlaybackSessionAtom)
  const setPersistedSession = useSetAtom(persistedPlaybackSessionAtom)
  const playbackState = useAtomValue(playbackStateAtom)
  const { startPlayback, updatePlaybackState } = usePlayback()
  const attemptedRef = useRef(false)

  useEffect(() => {
    if (attemptedRef.current) return
    if (playbackState.isPlaying) return
    if (!persistedSession?.playbackSessionId) return

    attemptedRef.current = true

    const resume = async () => {
      try {
        const resumeResult = await apolloClient.mutate({
          mutation: ResumePlaybackDocument,
          variables: {
            input: {
              capability: buildPlaybackCapabilityInput(
                playbackState.capabilityProfileVersion,
              ),
              capabilityProfileVersion:
                playbackState.capabilityProfileVersion ?? undefined,
              playbackSessionId: persistedSession.playbackSessionId,
            },
          },
        })

        const payload = resumeResult.data?.resumePlayback
        if (!payload) {
          setPersistedSession(null)
          return
        }

        const itemResult = await apolloClient.query({
          fetchPolicy: 'network-only',
          query: MediaDocument,
          variables: { id: payload.currentItemId },
        })

        const item = itemResult.data?.metadataItem
        if (!item) {
          setPersistedSession(null)
          return
        }

        startPlayback({
          autoPlay: false,
          capabilityProfileVersion: payload.capabilityProfileVersion,
          capabilityVersionMismatch: payload.capabilityVersionMismatch,
          originator: {
            id: item.id,
            metadataType: item.metadataType ?? MetadataType.Movie,
            thumbUri: item.thumbUri ?? undefined,
            title: item.title,
          },
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

        updatePlaybackState({
          capabilityProfileVersion: payload.capabilityProfileVersion,
          capabilityVersionMismatch: payload.capabilityVersionMismatch,
          currentTime: Number(payload.playheadMs ?? 0),
          isPlaying: false,
          streamOffset: 0,
        })
      } catch (error) {
        handleErrorStandalone(error, {
          context: 'useResumePlaybackOnLoad',
          notify: false,
        })
        setPersistedSession(null)
      }
    }

    void resume()
  }, [
    apolloClient,
    playbackState.capabilityProfileVersion,
    playbackState.isPlaying,
    persistedSession?.playbackSessionId,
    setPersistedSession,
    startPlayback,
    updatePlaybackState,
  ])
}
