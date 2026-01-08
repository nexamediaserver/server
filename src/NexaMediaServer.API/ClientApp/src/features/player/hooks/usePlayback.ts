import { useAtom, useSetAtom } from 'jotai'
import { useCallback } from 'react'

import type { Item } from '@/shared/api/graphql/graphql'

import { MetadataType } from '@/shared/api/graphql/graphql'

import {
  isPlaybackActiveAtom,
  lastMaximizedPreferenceAtom,
  persistedPlaybackSessionAtom,
  type PlaybackOriginator,
  type PlaybackState,
  playbackStateAtom,
} from '../store'

/**
 * Hook to control playback state
 */
export interface StartPlaybackParams {
  autoPlay?: boolean
  capabilityProfileVersion: number
  capabilityVersionMismatch?: boolean
  originator: PlaybackOriginator
  playbackSessionId: string
  playbackUrl: string
  playlistGeneratorId: string
  /** Current playlist index (0-based) */
  playlistIndex?: number
  /** Whether repeat mode is enabled */
  playlistRepeat?: boolean
  /** Whether shuffle mode is enabled */
  playlistShuffle?: boolean
  /** Total number of items in the playlist */
  playlistTotalCount?: number
  /**
   * Server-provided duration in milliseconds.
   * Authoritative duration that should be preferred over browser-reported values.
   */
  serverDuration?: number
  streamPlanJson?: string
  trickplayUrl?: null | string
}

export function usePlayback() {
  const [playback, setPlayback] = useAtom(playbackStateAtom)
  const [isActive] = useAtom(isPlaybackActiveAtom)
  const [lastMaximizedPreference, setLastMaximizedPreference] = useAtom(
    lastMaximizedPreferenceAtom,
  )
  const setPersistedPlaybackSession = useSetAtom(persistedPlaybackSessionAtom)

  const resolveMediaType = (
    type: Item['metadataType'],
    streamPlanJson?: string,
  ): PlaybackState['mediaType'] => {
    if (type === MetadataType.Episode) {
      return 'episode'
    }

    if (type === MetadataType.Movie) {
      return 'movie'
    }

    if (type === MetadataType.Track) {
      return 'music'
    }

    if (type === MetadataType.Photo) {
      return 'photo'
    }

    // Fallback: Check streamPlanJson for MediaType if metadataType doesn't resolve
    if (streamPlanJson) {
      try {
        const plan = JSON.parse(streamPlanJson) as { MediaType?: string }
        if (plan.MediaType === 'Photo') {
          return 'photo'
        }

        if (plan.MediaType === 'Audio') {
          return 'music'
        }

        if (plan.MediaType === 'Video') {
          return 'movie' // Default video type
        }
      } catch {
        // Ignore parse errors
      }
    }

    return 'unknown'
  }

  const startPlayback = useCallback(
    ({
      autoPlay = true,
      capabilityProfileVersion,
      capabilityVersionMismatch,
      originator,
      playbackSessionId,
      playbackUrl,
      playlistGeneratorId,
      playlistIndex,
      playlistRepeat,
      playlistShuffle,
      playlistTotalCount,
      serverDuration,
      streamPlanJson,
      trickplayUrl,
    }: StartPlaybackParams) => {
      setPlayback((prev) => ({
        ...prev,
        capabilityProfileVersion,
        capabilityVersionMismatch,
        // Set initial duration from server if available
        ...(serverDuration !== undefined && { duration: serverDuration }),
        isPlaying: autoPlay,
        // Restore the last maximized preference
        maximized: lastMaximizedPreference,
        mediaId: originator.id,
        mediaTitle: originator.title,
        mediaType: resolveMediaType(originator.metadataType, streamPlanJson),
        originator,
        playbackSessionId,
        playbackUrl,
        playlistGeneratorId,
        playlistIndex,
        playlistRepeat,
        playlistShuffle,
        playlistTotalCount,
        serverDuration,
        // Reset stream offset for new playback
        streamOffset: 0,
        streamPlanJson,
        trickplayUrl,
      }))

      setPersistedPlaybackSession({
        currentItemId: originator.id,
        playbackSessionId,
        playlistGeneratorId,
      })
    },
    [setPlayback, lastMaximizedPreference, setPersistedPlaybackSession],
  )

  const stopPlayback = useCallback(() => {
    setPlayback((prev) => {
      // Save the current maximized state before stopping
      setLastMaximizedPreference(prev.maximized)
      return {
        ...prev,
        capabilityProfileVersion: undefined,
        capabilityVersionMismatch: undefined,
        currentTime: 0,
        isPlaying: false,
        maximized: false,
        originator: undefined,
        playbackSessionId: undefined,
        playbackUrl: undefined,
        playlistGeneratorId: undefined,
        playlistIndex: undefined,
        playlistRepeat: undefined,
        playlistShuffle: undefined,
        playlistTotalCount: undefined,
        serverDuration: undefined,
        streamOffset: undefined,
        streamPlanJson: undefined,
        trickplayUrl: undefined,
      }
    })

    setPersistedPlaybackSession(null)
  }, [setPlayback, setLastMaximizedPreference])

  const togglePlayPause = useCallback(() => {
    setPlayback((prev) => ({
      ...prev,
      isPlaying: !prev.isPlaying,
    }))
  }, [setPlayback])

  const updateProgress = useCallback(
    (currentTime: number, duration?: number) => {
      setPlayback((prev) => ({
        ...prev,
        currentTime,
        ...(duration !== undefined && { duration }),
      }))
    },
    [setPlayback],
  )

  const setVolume = useCallback(
    (volume: number) => {
      setPlayback((prev) => ({
        ...prev,
        volume: Math.max(0, Math.min(1, volume)),
      }))
    },
    [setPlayback],
  )

  const toggleMute = useCallback(() => {
    setPlayback((prev) => ({
      ...prev,
      isMuted: !prev.isMuted,
    }))
  }, [setPlayback])

  const updatePlaybackState = useCallback(
    (updates: Partial<PlaybackState>) => {
      setPlayback((prev) => ({
        ...prev,
        ...updates,
      }))
    },
    [setPlayback],
  )

  const toggleMaximize = useCallback(() => {
    setPlayback((prev) => {
      const newMaximized = !prev.maximized
      // Persist the preference immediately
      setLastMaximizedPreference(newMaximized)
      return {
        ...prev,
        maximized: newMaximized,
      }
    })
  }, [setPlayback, setLastMaximizedPreference])

  return {
    isActive,
    playback,
    setVolume,
    startPlayback,
    stopPlayback,
    toggleMaximize,
    toggleMute,
    togglePlayPause,
    updatePlaybackState,
    updateProgress,
  }
}
