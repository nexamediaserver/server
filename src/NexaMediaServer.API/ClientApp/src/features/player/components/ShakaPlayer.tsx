import { useApolloClient } from '@apollo/client/react'
import { useAtom } from 'jotai'
import {
  forwardRef,
  useCallback,
  useEffect,
  useImperativeHandle,
  useRef,
} from 'react'
import shaka from 'shaka-player'

import { graphql } from '@/shared/api/graphql'
import { MetadataType } from '@/shared/api/graphql/graphql'
import { handleErrorStandalone } from '@/shared/hooks/useErrorHandler'
import { createPlaybackError } from '@/shared/lib/errors'
import { buildPlaybackCapabilityInput } from '@/shared/lib/playbackCapabilities'

import { usePlayback } from '../hooks/usePlayback'
import { useStopPlayback } from '../hooks/useStopPlayback'
import { type PlaybackState, playbackStateAtom } from '../store'

const decidePlaybackMutation = graphql(`
  mutation DecidePlayback($input: PlaybackDecisionInput!) {
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

export interface ShakaPlayerHandle {
  getMediaElement: () => HTMLAudioElement | HTMLVideoElement | null
  getPlayer: () => null | shaka.Player
  /** @deprecated Use getMediaElement instead */
  getVideoElement: () => HTMLVideoElement | null
  seek: (time: number) => void
}

interface ShakaPlayerProps {
  /**
   * CSS class name for the container
   */
  className?: string

  /**
   * Media type: 'video' or 'audio'. Determines which element to render.
   * Defaults to 'video'.
   */
  mediaType?: 'audio' | 'video'

  /**
   * Callback when an error occurs
   */
  onError?: (error: shaka.util.Error) => void

  /**
   * Callback when the player is ready
   */
  onPlayerReady?: (player: shaka.Player) => void
}

/**
 * ShakaPlayer component that integrates with the Jotai playback store
 * No built-in UI - controls are external
 * Supports both audio and video playback
 *
 * Follows Jellyfin's approach: simple client-side seeking, server handles
 * transcoding offsets and segment generation.
 */
export const ShakaPlayer = forwardRef<ShakaPlayerHandle, ShakaPlayerProps>(
  ({ className, mediaType = 'video', onError, onPlayerReady }, ref) => {
    const apolloClient = useApolloClient()
    const mediaRef = useRef<HTMLAudioElement | HTMLVideoElement>(null)
    const containerRef = useRef<HTMLDivElement>(null)
    const playerRef = useRef<null | shaka.Player>(null)

    const [playbackState] = useAtom(playbackStateAtom)
    const playbackStateRef = useRef<PlaybackState>(playbackState)
    const { updatePlaybackState, updateProgress } = usePlayback()
    const { stopPlayback } = useStopPlayback()

    useEffect(() => {
      playbackStateRef.current = playbackState
    }, [playbackState])

    // Perform a direct seek to the media element
    // Jellyfin approach: simple seeking, server handles transcode offsets
    const performSeek = useCallback((timeInSeconds: number) => {
      if (mediaRef.current) {
        mediaRef.current.currentTime = timeInSeconds
      }
    }, [])

    // Expose methods to parent via ref
    // Jellyfin approach: simple seeking without client-side GoP logic
    useImperativeHandle(
      ref,
      () => ({
        getMediaElement: () => mediaRef.current,
        getPlayer: () => playerRef.current,
        getVideoElement: () =>
          mediaRef.current instanceof HTMLVideoElement
            ? mediaRef.current
            : null,
        seek: (time: number) => {
          performSeek(time)
        },
      }),
      [performSeek],
    )

    // Initialize Shaka Player
    useEffect(() => {
      if (!mediaRef.current || !containerRef.current) return

      // Install polyfills
      shaka.polyfill.installAll()

      // Check if browser is supported
      if (!shaka.Player.isBrowserSupported()) {
        handleErrorStandalone(
          createPlaybackError('Browser not supported for Shaka Player'),
          { context: 'ShakaPlayer' },
        )
        return
      }

      // Create player instance
      const player = new shaka.Player()
      playerRef.current = player

      // Attach player to media element
      void player.attach(mediaRef.current).then(() => {
        // Notify parent component
        onPlayerReady?.(player)
      })

      // Error handler
      const handleError = (event: Event) => {
        const errorEvent = event as unknown as { detail: shaka.util.Error }
        handleErrorStandalone(
          createPlaybackError('Player error', errorEvent.detail),
          { context: 'ShakaPlayer' },
        )
        onError?.(errorEvent.detail)
      }
      player.addEventListener('error', handleError)

      // Cleanup
      return () => {
        player.removeEventListener('error', handleError)
        void player.destroy()
        playerRef.current = null
      }
    }, [onError, onPlayerReady, mediaType])

    // Load media when playbackUrl changes
    useEffect(() => {
      const player = playerRef.current
      const media = mediaRef.current

      if (!player || !media || !playbackState.playbackUrl) {
        return
      }

      // Load the manifest
      player
        .load(playbackState.playbackUrl)
        .then(() => {
          // Update duration when available
          if (media.duration && !Number.isNaN(media.duration)) {
            updateProgress(0, media.duration * 1000) // Convert to milliseconds
          }

          // Add trickplay thumbnails if available (video only)
          const trickplayUrl = playbackState.trickplayUrl
          if (trickplayUrl && mediaType === 'video') {
            player.addThumbnailsTrack(trickplayUrl).catch((error: unknown) => {
              handleErrorStandalone(error, {
                context: 'ShakaPlayer.trickplay',
                notify: false,
              })
            })
          }
        })
        .catch((error: unknown) => {
          // Ignore LOAD_INTERRUPTED errors (code 7002) - these occur when a new load
          // is triggered before the previous one completes, which is expected behavior
          if (
            error &&
            typeof error === 'object' &&
            'code' in error &&
            (error as { code: number }).code === 7002
          ) {
            return
          }

          handleErrorStandalone(
            createPlaybackError('Error loading media', error),
            { context: 'ShakaPlayer' },
          )
          if (error && typeof error === 'object' && 'severity' in error) {
            onError?.(error as shaka.util.Error)
          }
        })
    }, [
      playbackState.playbackUrl,
      updateProgress,
      onError,
      mediaType,
      playbackState.trickplayUrl,
    ])

    // Sync play/pause state
    useEffect(() => {
      const media = mediaRef.current
      const player = playerRef.current
      if (!media) return

      if (playbackState.isPlaying) {
        // Only attempt to play if the player has loaded content
        // This prevents AbortError when play() is called before/during load
        if (player && media.readyState >= HTMLMediaElement.HAVE_CURRENT_DATA) {
          media.play().catch((error: unknown) => {
            // Ignore AbortError - this happens when play() is interrupted by a new load
            // or user navigation, which is expected behavior
            if (error instanceof DOMException && error.name === 'AbortError') {
              return
            }
            handleErrorStandalone(error, {
              context: 'ShakaPlayer.play',
              notify: false,
            })
          })
        }
      } else {
        media.pause()
      }
    }, [playbackState.isPlaying])

    // Ensure autoplay triggers once the media element can play (covers direct play and transcode)
    useEffect(() => {
      const media = mediaRef.current
      if (!media) return

      const handleCanPlay = () => {
        if (!playbackState.isPlaying) return

        media.play().catch((error: unknown) => {
          if (error instanceof DOMException && error.name === 'AbortError') {
            return
          }
          handleErrorStandalone(error, {
            context: 'ShakaPlayer.autoplay',
            notify: false,
          })
        })
      }

      media.addEventListener('canplay', handleCanPlay)

      return () => {
        media.removeEventListener('canplay', handleCanPlay)
      }
    }, [playbackState.isPlaying])

    // Sync volume
    useEffect(() => {
      const media = mediaRef.current
      if (!media) return

      media.volume = playbackState.volume
    }, [playbackState.volume])

    // Sync mute state
    useEffect(() => {
      const media = mediaRef.current
      if (!media) return

      media.muted = playbackState.isMuted
    }, [playbackState.isMuted])

    // Listen to media events and update playback state
    useEffect(() => {
      const media = mediaRef.current
      if (!media) return

      const handleTimeUpdate = () => {
        // Update current time and buffered time
        let bufferedEnd = 0
        if (media.buffered.length > 0) {
          // Get the end of the buffered range that contains current time
          for (let i = 0; i < media.buffered.length; i++) {
            if (
              media.buffered.start(i) <= media.currentTime &&
              media.buffered.end(i) >= media.currentTime
            ) {
              bufferedEnd = media.buffered.end(i)
              break
            }
          }
          // If no range contains current time, use the last buffered range
          if (bufferedEnd === 0 && media.buffered.length > 0) {
            bufferedEnd = media.buffered.end(media.buffered.length - 1)
          }
        }
        const durationMs = Number.isFinite(media.duration)
          ? media.duration * 1000
          : 0

        // Account for stream offset when we've reloaded the stream from a seek position
        const streamOffset = playbackStateRef.current.streamOffset ?? 0
        const actualCurrentTime = media.currentTime * 1000 + streamOffset

        updatePlaybackState({
          bufferedTime: bufferedEnd * 1000 + streamOffset,
          currentTime: actualCurrentTime,
          duration: durationMs,
        })
      }

      const handleDurationChange = () => {
        if (!Number.isNaN(media.duration) && Number.isFinite(media.duration)) {
          updatePlaybackState({ duration: media.duration * 1000 })
        }
      }

      const handleEnded = () => {
        const latestPlayback = playbackStateRef.current

        // Check if we're actually near the end of the server-reported duration.
        // For remuxed/transcoded streams, the browser may incorrectly fire 'ended'
        // when seeking past the (incorrect) browser-reported duration.
        const serverDuration = latestPlayback.serverDuration ?? 0
        const streamOffset = latestPlayback.streamOffset ?? 0
        const currentTime = latestPlayback.currentTime
        const actualPosition = currentTime + streamOffset

        if (serverDuration > 0) {
          // If we're not within 5 seconds of the actual end, this is a false 'ended' event
          // caused by seeking past the browser-reported duration in a remuxed stream.
          // In this case, we should ignore it.
          const timeRemaining = serverDuration - actualPosition
          if (timeRemaining > 5000) {
            // This is a false ended event - the user seeked past browser-reported duration
            // but hasn't actually reached the end of the content
            return
          }
        }

        if (latestPlayback.playbackSessionId && latestPlayback.originator?.id) {
          void apolloClient
            .mutate({
              mutation: decidePlaybackMutation,
              variables: {
                input: {
                  capability: buildPlaybackCapabilityInput(
                    latestPlayback.capabilityProfileVersion,
                  ),
                  capabilityProfileVersion:
                    latestPlayback.capabilityProfileVersion,
                  currentItemId:
                    latestPlayback.mediaId ?? latestPlayback.originator.id,
                  playbackSessionId: latestPlayback.playbackSessionId,
                  progressMs: Math.round(latestPlayback.currentTime),
                  status: 'ended',
                },
              },
            })
            .then((response) => {
              const payload = response.data?.decidePlayback
              if (payload) {
                // Handle playlist navigation based on server action
                if (
                  payload.action === 'next' &&
                  payload.playbackUrl &&
                  payload.nextItemId
                ) {
                  // Advance to the next item in the playlist
                  // Update originator with the new track's info for display
                  const currentOriginator = latestPlayback.originator
                  updatePlaybackState({
                    capabilityProfileVersion: payload.capabilityProfileVersion,
                    capabilityVersionMismatch:
                      payload.capabilityVersionMismatch,
                    currentTime: 0,
                    mediaId: payload.nextItemId,
                    originator: {
                      id: payload.nextItemId,
                      metadataType:
                        currentOriginator?.metadataType ?? MetadataType.Track,
                      originalTitle: payload.nextItemOriginalTitle ?? undefined,
                      parentThumbUri: currentOriginator?.parentThumbUri,
                      parentTitle: payload.nextItemParentTitle ?? undefined,
                      thumbUri:
                        payload.nextItemThumbUrl ??
                        currentOriginator?.thumbUri ??
                        null,
                      title: payload.nextItemTitle ?? 'Unknown',
                    },
                    playbackUrl: payload.playbackUrl,
                    playlistIndex: (latestPlayback.playlistIndex ?? 0) + 1,
                    serverDuration: undefined, // Will be updated when new stream loads
                    streamOffset: 0,
                    streamPlanJson: payload.streamPlanJson,
                    trickplayUrl: payload.trickplayUrl,
                  })
                } else if (payload.action === 'stop') {
                  // End of playlist reached, stop playback
                  void stopPlayback()
                } else {
                  // Default: just update capability info and continue
                  updatePlaybackState({
                    capabilityProfileVersion: payload.capabilityProfileVersion,
                    capabilityVersionMismatch:
                      payload.capabilityVersionMismatch,
                  })
                  void stopPlayback()
                }
              } else {
                void stopPlayback()
              }
            })
            .catch((error: unknown) => {
              handleErrorStandalone(error, {
                context: 'ShakaPlayer.decidePlayback',
                notify: false,
              })
              void stopPlayback()
            })
        } else {
          void stopPlayback()
        }
      }

      const handleVolumeChange = () => {
        updatePlaybackState({
          isMuted: media.muted,
          volume: media.volume,
        })
      }

      media.addEventListener('timeupdate', handleTimeUpdate)
      media.addEventListener('durationchange', handleDurationChange)
      media.addEventListener('ended', handleEnded)
      media.addEventListener('volumechange', handleVolumeChange)

      return () => {
        media.removeEventListener('timeupdate', handleTimeUpdate)
        media.removeEventListener('durationchange', handleDurationChange)
        media.removeEventListener('ended', handleEnded)
        media.removeEventListener('volumechange', handleVolumeChange)
      }
    }, [apolloClient, stopPlayback, updatePlaybackState])

    // Seek to specific time when currentTime changes externally
    const lastSeekTimeRef = useRef<number>(0)
    useEffect(() => {
      const media = mediaRef.current
      if (!media) return

      // Account for stream offset - playbackState.currentTime is absolute position,
      // but media.currentTime is relative to the current stream start
      const streamOffset = playbackStateRef.current.streamOffset ?? 0
      const targetTime = (playbackState.currentTime - streamOffset) / 1000 // Convert from milliseconds and adjust for offset
      const currentTime = media.currentTime

      // Only seek if the difference is significant (more than 1 second)
      // and not caused by normal playback progression
      // Also ensure target time is non-negative (could be negative if offset is larger due to timing)
      if (targetTime >= 0 && Math.abs(targetTime - currentTime) > 1) {
        const now = Date.now()
        // Debounce seeks to prevent loops
        if (now - lastSeekTimeRef.current > 500) {
          media.currentTime = targetTime
          lastSeekTimeRef.current = now
        }
      }
    }, [playbackState.currentTime])

    return (
      <div
        className={className}
        data-shaka-player-cast-receiver-id="YOUR_CAST_RECEIVER_ID"
        data-shaka-player-container
        ref={containerRef}
      >
        {/* Captions track handled by Shaka; disable lint check for missing native track */}
        {mediaType === 'audio' ? (
          <audio
            className="hidden"
            ref={mediaRef as React.RefObject<HTMLAudioElement>}
          />
        ) : (
          <video
            className="h-full w-full"
            playsInline
            ref={mediaRef as React.RefObject<HTMLVideoElement>}
          />
        )}
      </div>
    )
  },
)

// Export player instance getter for advanced usage
ShakaPlayer.displayName = 'ShakaPlayer'
