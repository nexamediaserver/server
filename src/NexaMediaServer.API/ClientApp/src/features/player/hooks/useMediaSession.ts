import { useEffect, useRef } from 'react'

import type { PlaybackState } from '@/store/playback'

/**
 * Available media session action handlers
 */
export interface MediaSessionActionHandlers {
  /**
   * Handler for next track/episode
   */
  nexttrack?: () => void

  /**
   * Handler for pause action
   */
  pause?: () => void

  /**
   * Handler for play action
   */
  play?: () => void

  /**
   * Handler for previous track/episode
   */
  previoustrack?: () => void

  /**
   * Handler for seek backward action
   */
  seekbackward?: (details: MediaSessionActionDetails) => void

  /**
   * Handler for seek forward action
   */
  seekforward?: (details: MediaSessionActionDetails) => void

  /**
   * Handler for seeking to a specific position
   */
  seekto?: (details: MediaSessionActionDetails) => void

  /**
   * Handler for stop action
   */
  stop?: () => void
}

/**
 * Configuration for media session metadata
 */
export interface MediaSessionConfig {
  /**
   * Album name (for music tracks)
   */
  album?: string

  /**
   * Artist name (for music tracks)
   */
  artist?: string

  /**
   * Array of artwork images
   */
  artwork?: MediaImage[]

  /**
   * Media title
   */
  title: string
}

/**
 * Base interface for media session providers
 * This allows different player types (video, audio, manga, images, etc.) to provide
 * their own media session implementations
 */
export interface MediaSessionProvider {
  /**
   * Get action handlers for this media type
   */
  getActionHandlers(): MediaSessionActionHandlers

  /**
   * Get metadata configuration for this media type
   */
  getMetadata(): MediaSessionConfig

  /**
   * Update position state in the media session
   */
  updatePositionState(state: PlaybackState): void
}

/**
 * Hook to manage Media Session API integration
 * Supports different media types through the MediaSessionProvider interface
 *
 * @param provider - The media session provider for the current player type
 * @param playbackState - Current playback state
 * @param enabled - Whether the media session should be active
 */
export function useMediaSession(
  provider: MediaSessionProvider | null,
  playbackState: PlaybackState,
  enabled = true,
) {
  const metadataRef = useRef<MediaMetadata | null>(null)

  // Set up media session metadata
  useEffect(() => {
    if (
      !enabled ||
      !provider ||
      !playbackState.originator ||
      !('mediaSession' in navigator)
    ) {
      return
    }

    const config = provider.getMetadata()

    // Create and set metadata
    metadataRef.current = new MediaMetadata({
      album: config.album,
      artist: config.artist,
      artwork: config.artwork,
      title: config.title,
    })

    navigator.mediaSession.metadata = metadataRef.current

    return () => {
      // Clear metadata when component unmounts or provider changes
      if ('mediaSession' in navigator) {
        navigator.mediaSession.metadata = null
      }
      metadataRef.current = null
    }
  }, [enabled, provider, playbackState.originator])

  // Set up action handlers
  useEffect(() => {
    if (
      !enabled ||
      !provider ||
      !playbackState.originator ||
      !('mediaSession' in navigator)
    ) {
      return
    }

    const handlers = provider.getActionHandlers()

    // Define supported actions
    const supportedActions: {
      action: MediaSessionAction
      handler:
        | (() => void)
        | ((details: MediaSessionActionDetails) => void)
        | undefined
    }[] = [
      { action: 'play', handler: handlers.play },
      { action: 'pause', handler: handlers.pause },
      { action: 'stop', handler: handlers.stop },
      { action: 'seekbackward', handler: handlers.seekbackward },
      { action: 'seekforward', handler: handlers.seekforward },
      { action: 'seekto', handler: handlers.seekto },
      { action: 'previoustrack', handler: handlers.previoustrack },
      { action: 'nexttrack', handler: handlers.nexttrack },
    ]

    // Set handlers for actions that have implementations
    supportedActions.forEach(({ action, handler }) => {
      if (handler) {
        navigator.mediaSession.setActionHandler(
          action,
          handler as MediaSessionActionHandler,
        )
      }
    })

    return () => {
      // Clear all action handlers when component unmounts
      if ('mediaSession' in navigator) {
        supportedActions.forEach(({ action }) => {
          try {
            navigator.mediaSession.setActionHandler(action, null)
          } catch (error) {
            // Some browsers may not support all actions
            console.debug(
              `Failed to clear action handler for ${action}:`,
              error,
            )
          }
        })
      }
    }
  }, [enabled, provider, playbackState.originator])

  // Update playback state (play/pause)
  useEffect(() => {
    if (
      !enabled ||
      !provider ||
      !playbackState.originator ||
      !('mediaSession' in navigator)
    ) {
      return
    }

    navigator.mediaSession.playbackState = playbackState.isPlaying
      ? 'playing'
      : 'paused'
  }, [enabled, provider, playbackState.isPlaying, playbackState.originator])

  // Update position state
  useEffect(() => {
    if (
      !enabled ||
      !provider ||
      !playbackState.originator ||
      !('mediaSession' in navigator) ||
      playbackState.duration === 0
    ) {
      return
    }

    try {
      provider.updatePositionState(playbackState)
    } catch (error) {
      // Position state may fail in some browsers or conditions
      console.debug('Failed to update position state:', error)
    }
  }, [
    enabled,
    provider,
    playbackState.currentTime,
    playbackState.duration,
    playbackState.originator,
  ])
}
