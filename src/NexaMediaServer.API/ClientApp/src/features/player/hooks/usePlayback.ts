import { useAtom } from 'jotai'
import { useCallback } from 'react'

import type { Item } from '@/shared/api/graphql/graphql'
import type { PlaybackState } from '@/store/playback'

import { MetadataType } from '@/shared/api/graphql/graphql'
import {
  isPlaybackActiveAtom,
  lastMaximizedPreferenceAtom,
  playbackStateAtom,
} from '@/store/playback'

/**
 * Hook to control playback state
 */
export function usePlayback() {
  const [playback, setPlayback] = useAtom(playbackStateAtom)
  const [isActive] = useAtom(isPlaybackActiveAtom)
  const [lastMaximizedPreference, setLastMaximizedPreference] = useAtom(
    lastMaximizedPreferenceAtom,
  )

  const startPlayback = useCallback(
    (
      directPlayUrl: string,
      originator: Pick<Item, 'id' | 'metadataType' | 'thumbUri' | 'title'>,
    ) => {
      setPlayback((prev) => ({
        ...prev,
        directPlayUrl,
        isPlaying: true,
        // Restore the last maximized preference
        maximized: lastMaximizedPreference,
        mediaId: originator.id,
        mediaTitle: originator.title,
        mediaType:
          originator.metadataType === MetadataType.Episode
            ? 'episode'
            : originator.metadataType === MetadataType.Movie
              ? 'movie'
              : 'music',
        originator,
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
        currentTime: 0,
        directPlayUrl: undefined,
        isPlaying: false,
        maximized: false,
        originator: undefined,
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
