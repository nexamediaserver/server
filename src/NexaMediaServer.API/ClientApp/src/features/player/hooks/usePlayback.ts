import { useAtom } from 'jotai'
import { useCallback } from 'react'

import type { Item } from '@/shared/api/graphql/graphql'

import { MetadataType } from '@/shared/api/graphql/graphql'

import {
  isPlaybackActiveAtom,
  lastMaximizedPreferenceAtom,
  type PlaybackState,
  playbackStateAtom,
} from '../store'

/**
 * Hook to control playback state
 */
export interface StartPlaybackParams {
  capabilityProfileVersion: number
  capabilityVersionMismatch?: boolean
  originator: Pick<Item, 'id' | 'metadataType' | 'thumbUri' | 'title'>
  playbackSessionId: string
  playbackUrl: string
  playlistGeneratorId: string
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

  const resolveMediaType = (
    type: Item['metadataType'],
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

    return 'unknown'
  }

  const startPlayback = useCallback(
    ({
      capabilityProfileVersion,
      capabilityVersionMismatch,
      originator,
      playbackSessionId,
      playbackUrl,
      playlistGeneratorId,
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
        isPlaying: true,
        // Restore the last maximized preference
        maximized: lastMaximizedPreference,
        mediaId: originator.id,
        mediaTitle: originator.title,
        mediaType: resolveMediaType(originator.metadataType),
        originator,
        playbackSessionId,
        playbackUrl,
        playlistGeneratorId,
        serverDuration,
        // Reset stream offset for new playback
        streamOffset: 0,
        streamPlanJson,
        trickplayUrl,
      }))
    },
    [setPlayback, lastMaximizedPreference],
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
        serverDuration: undefined,
        streamOffset: undefined,
        streamPlanJson: undefined,
        trickplayUrl: undefined,
      }
    })
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
    setPlayback((prev) => ({
      ...prev,
      maximized: !prev.maximized,
    }))
  }, [setPlayback])

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
