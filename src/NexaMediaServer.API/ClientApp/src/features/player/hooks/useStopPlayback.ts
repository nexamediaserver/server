import { gql } from '@apollo/client'
import { useApolloClient } from '@apollo/client/react'
import { useCallback } from 'react'

import { handleErrorStandalone } from '@/shared/hooks/useErrorHandler'

import { usePlayback } from './usePlayback'

const StopPlaybackDocument = gql`
  mutation StopPlayback($input: PlaybackStopInput!) {
    stopPlayback(input: $input) {
      success
    }
  }
`

/**
 * Hook that clears local playback state and notifies the server to remove the session.
 */
export function useStopPlayback() {
  const apolloClient = useApolloClient()
  const { playback, stopPlayback: clearPlayback } = usePlayback()

  const stopPlayback = useCallback(async () => {
    const playbackSessionId = playback.playbackSessionId

    // Clear local state immediately for snappy UI response
    clearPlayback()

    if (!playbackSessionId) {
      return
    }

    try {
      await apolloClient.mutate({
        mutation: StopPlaybackDocument,
        variables: { input: { playbackSessionId } },
      })
    } catch (error) {
      // Non-fatal: the server will clean up stale sessions periodically
      handleErrorStandalone(error, {
        context: 'useStopPlayback',
        notify: false,
      })
    }
  }, [apolloClient, clearPlayback, playback.playbackSessionId])

  return { stopPlayback }
}
