import { useApolloClient } from '@apollo/client/react'
import { useAtomValue } from 'jotai'
import { useCallback, useRef } from 'react'

import { graphql } from '@/shared/api/graphql'
import { handleErrorStandalone } from '@/shared/hooks/useErrorHandler'

import { playbackStateAtom } from '../store'

/**
 * Result of a GoP-aware seek operation.
 */
export interface GopSeekResult {
  /** Duration of the GoP (for display purposes) */
  gopDurationMs: number
  /** The actual position to seek to (keyframe-aligned for remux/transcode) */
  seekTimeMs: number
  /** Whether the seek was aligned to a keyframe */
  wasAligned: boolean
}

/**
 * GraphQL mutation to notify server of a seek operation and get the nearest keyframe.
 */
const PlaybackSeekMutation = graphql(`
  mutation PlaybackSeek($input: PlaybackSeekInput!) {
    playbackSeek(input: $input) {
      keyframeMs
      gopDurationMs
      hasGopIndex
      originalTargetMs
    }
  }
`)

/**
 * Playback mode enum values (matches backend PlaybackMode enum)
 */
const PlaybackMode = {
  DirectPlay: 0,
  DirectStream: 1,
  Transcode: 2,
} as const

/**
 * Parsed stream plan from the JSON stored in playback state.
 */
interface StreamPlan {
  ManifestUrl?: string
  MediaPartId?: number
  // Mode can be numeric (0, 1, 2) or string ('DirectPlay', 'DirectStream', 'Transcode')
  Mode?: number | string
  RemuxUrl?: string
}

/**
 * Hook that provides GoP-aware seeking for transcoding and remuxing streams.
 *
 * When the stream mode is DirectStream (remux) or Transcode, this hook will
 * query the server for the nearest keyframe position before seeking, enabling
 * faster seek feedback.
 *
 * For DirectPlay mode, seeking is done directly without server involvement.
 */
export function useGopSeek() {
  const apolloClient = useApolloClient()
  const playbackState = useAtomValue(playbackStateAtom)

  // Use ref to access latest playback state in callbacks without re-creating them
  const playbackStateRef = useRef(playbackState)
  playbackStateRef.current = playbackState

  /**
   * Parses the stream plan JSON from playback state.
   */
  const getStreamPlan = useCallback((): null | StreamPlan => {
    const json = playbackStateRef.current.streamPlanJson
    if (!json) return null

    try {
      return JSON.parse(json) as StreamPlan
    } catch {
      return null
    }
  }, [])

  /**
   * Determines if the current playback mode benefits from GoP-based seeking.
   * DirectStream (remux) and Transcode modes benefit from keyframe alignment.
   */
  const needsGopSeek = useCallback((): boolean => {
    const plan = getStreamPlan()
    const mode = plan?.Mode
    if (mode === undefined) return false

    // Handle both numeric enum values (from JSON) and string values
    return (
      mode === PlaybackMode.DirectStream ||
      mode === PlaybackMode.Transcode ||
      mode === 'DirectStream' ||
      mode === 'Transcode'
    )
  }, [getStreamPlan])

  /**
   * Gets the nearest keyframe position from the server.
   *
   * @param targetMs - The target seek position in milliseconds
   * @returns The GoP seek result with the aligned position, or null on error
   */
  const getKeyframePosition = useCallback(
    async (targetMs: number): Promise<GopSeekResult | null> => {
      const state = playbackStateRef.current
      const plan = getStreamPlan()

      if (!state.playbackSessionId || !plan?.MediaPartId) {
        return null
      }

      try {
        const result = await apolloClient.mutate({
          mutation: PlaybackSeekMutation,
          variables: {
            input: {
              mediaPartId: plan.MediaPartId,
              playbackSessionId: state.playbackSessionId,
              targetMs: Math.round(targetMs),
            },
          },
        })

        const payload = result.data?.playbackSeek
        if (!payload) {
          return null
        }

        return {
          gopDurationMs: Number(payload.gopDurationMs),
          seekTimeMs: Number(payload.keyframeMs),
          wasAligned: payload.hasGopIndex,
        }
      } catch (error) {
        handleErrorStandalone(error, {
          context: 'useGopSeek.getKeyframePosition',
          notify: false,
        })
        return null
      }
    },
    [apolloClient, getStreamPlan],
  )

  /**
   * Performs a GoP-aware seek operation.
   *
   * For DirectStream/Transcode modes, queries the server for the nearest keyframe
   * and returns the aligned position. For DirectPlay, returns the original target.
   *
   * @param targetMs - The target seek position in milliseconds
   * @returns The position to seek to (possibly keyframe-aligned)
   */
  const seekToKeyframe = useCallback(
    async (targetMs: number): Promise<GopSeekResult> => {
      // If not a mode that benefits from GoP seeking, return original position
      if (!needsGopSeek()) {
        return {
          gopDurationMs: 0,
          seekTimeMs: targetMs,
          wasAligned: false,
        }
      }

      // Query server for nearest keyframe
      const result = await getKeyframePosition(targetMs)

      if (result) {
        return result
      }

      // Fallback to original position on error
      return {
        gopDurationMs: 0,
        seekTimeMs: targetMs,
        wasAligned: false,
      }
    },
    [getKeyframePosition, needsGopSeek],
  )

  return {
    /**
     * Gets the nearest keyframe position from the server without seeking.
     * Useful for preview or planning purposes.
     */
    getKeyframePosition,

    /**
     * Gets the current stream plan parsed from JSON.
     */
    getStreamPlan,

    /**
     * Whether the current playback mode benefits from GoP-based seeking.
     */
    needsGopSeek,

    /**
     * Performs a GoP-aware seek, returning the keyframe-aligned position for
     * remux/transcode modes or the original position for direct play.
     */
    seekToKeyframe,
  }
}
