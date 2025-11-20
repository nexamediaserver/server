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
import { handleErrorStandalone } from '@/shared/hooks/useErrorHandler'
import { createPlaybackError } from '@/shared/lib/errors'
import { buildPlaybackCapabilityInput } from '@/shared/lib/playbackCapabilities'

import { useGopSeek } from '../hooks/useGopSeek'
import { usePlayback } from '../hooks/usePlayback'
import { type PlaybackState, playbackStateAtom } from '../store'

const decidePlaybackMutation = graphql(`
  mutation DecidePlayback($input: PlaybackDecisionInput!) {
    decidePlayback(input: $input) {
      action
      streamPlanJson
      nextItemId
      playbackUrl
      trickplayUrl
      capabilityProfileVersion
      capabilityVersionMismatch
    }
  }
`)

export interface ShakaPlayerHandle {
  getPlayer: () => null | shaka.Player
  getVideoElement: () => HTMLVideoElement | null
  seek: (time: number) => void
  /** Performs a GoP-aware seek for remux/transcode modes. Returns the actual seek position. */
  seekWithGop: (time: number) => Promise<number>
}

interface ShakaPlayerProps {
  /**
   * CSS class name for the container
   */
  className?: string

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
 */
export const ShakaPlayer = forwardRef<ShakaPlayerHandle, ShakaPlayerProps>(
  ({ className, onError, onPlayerReady }, ref) => {
    const apolloClient = useApolloClient()
    const videoRef = useRef<HTMLVideoElement>(null)
    const containerRef = useRef<HTMLDivElement>(null)
    const playerRef = useRef<null | shaka.Player>(null)

    const [playbackState] = useAtom(playbackStateAtom)
    const playbackStateRef = useRef<PlaybackState>(playbackState)
    const { stopPlayback, updatePlaybackState, updateProgress } = usePlayback()
    const { needsGopSeek, seekToKeyframe } = useGopSeek()

    useEffect(() => {
      playbackStateRef.current = playbackState
    }, [playbackState])

    // Perform a direct seek to the video element
    const performSeek = useCallback((timeInSeconds: number) => {
      if (videoRef.current) {
        videoRef.current.currentTime = timeInSeconds
      }
    }, [])

    // Build a remux-seek URL for reloading the stream from a specific position
    const buildRemuxSeekUrl = useCallback(
      (keyframeMs: number): null | string => {
        const state = playbackStateRef.current
        if (!state.streamPlanJson) return null

        try {
          const plan = JSON.parse(state.streamPlanJson) as {
            Container?: string
            MediaPartId?: number
            Mode?: number | string
            RemuxUrl?: string
          }

          // Only applicable for DirectStream (remux) mode
          // Mode can be numeric (1) or string ('DirectStream')
          const isDirectStream = plan.Mode === 1 || plan.Mode === 'DirectStream'
          if (!isDirectStream || !plan.MediaPartId) {
            return null
          }

          const container = plan.Container ?? 'mp4'
          return `/api/v1/playback/part/${String(plan.MediaPartId)}/remux-seek.${container}?seekMs=${String(Math.round(keyframeMs))}`
        } catch {
          return null
        }
      },
      [],
    )

    // Build a DASH seek URL for reloading the stream from a specific position
    const buildDashSeekUrl = useCallback(
      (keyframeMs: number): null | string => {
        const state = playbackStateRef.current
        if (!state.streamPlanJson) return null

        try {
          const plan = JSON.parse(state.streamPlanJson) as {
            MediaPartId?: number
            Mode?: number | string
          }

          // Only applicable for Transcode (DASH) mode
          // Mode can be numeric (2) or string ('Transcode')
          const isTranscode = plan.Mode === 2 || plan.Mode === 'Transcode'
          if (!isTranscode || !plan.MediaPartId) {
            return null
          }

          return `/api/v1/playback/part/${String(plan.MediaPartId)}/dash-seek/manifest.mpd?seekMs=${String(Math.round(keyframeMs))}`
        } catch {
          return null
        }
      },
      [],
    )

    // Perform a GoP-aware seek for remux/transcode modes
    const performGopSeek = useCallback(
      async (timeInSeconds: number): Promise<number> => {
        const player = playerRef.current
        const video = videoRef.current
        const state = playbackStateRef.current
        const targetMs = timeInSeconds * 1000

        // Query server for optimal keyframe position
        const result = await seekToKeyframe(targetMs)
        const seekTimeSeconds = result.seekTimeMs / 1000

        // Get the actual seekable range from Shaka (more reliable than video.duration for DASH)
        const seekRange = player?.seekRange()
        const seekableEnd = seekRange?.end ?? video?.duration ?? 0

        // Use finite duration for comparison (Infinity means live/progressive stream)
        const browserDuration = Number.isFinite(seekableEnd) ? seekableEnd : 0

        // serverDuration is the actual media duration from the backend (in ms)
        const serverDurationSeconds = (state.serverDuration ?? 0) / 1000

        // Debug: log seek attempt details
        console.log('[ShakaPlayer] Seeking:', {
          browserDuration,
          mode: state.streamPlanJson
            ? (
                JSON.parse(state.streamPlanJson) as {
                  Mode?: number | string
                }
              ).Mode
            : 'unknown',
          seekRange,
          seekTimeSeconds,
          serverDurationSeconds,
          targetMs,
        })

        // Check if we're seeking past the currently available content
        // This can happen with:
        // 1. Remux streams where browser doesn't know full duration
        // 2. DASH streams that are still being transcoded
        // If browser knows less than server, and we're seeking past browser's knowledge, reload
        const isPastBrowserKnowledge =
          browserDuration > 0 &&
          seekTimeSeconds > browserDuration - 1 &&
          serverDurationSeconds > browserDuration + 1

        if (isPastBrowserKnowledge && player && video && needsGopSeek()) {
          // For DirectStream (remux), we can reload the stream from a new position
          const remuxSeekUrl = buildRemuxSeekUrl(result.seekTimeMs)
          if (remuxSeekUrl) {
            // Reload the stream from the seek position
            const wasPlaying = !video.paused

            await player.load(remuxSeekUrl)

            // Set the stream offset so time calculations account for the seek position
            updatePlaybackState({
              currentTime: result.seekTimeMs,
              streamOffset: result.seekTimeMs,
            })

            if (wasPlaying) {
              video.play().catch(() => {
                // Ignore play errors during seek reload
              })
            }

            return seekTimeSeconds
          }

          // For Transcode (DASH) mode, request a new transcode starting from seek position
          const dashSeekUrl = buildDashSeekUrl(result.seekTimeMs)
          if (dashSeekUrl) {
            console.log('[ShakaPlayer] Loading DASH seek URL:', dashSeekUrl)
            const wasPlaying = !video.paused

            // Fetch manifest to get the actual start time from header
            const manifestResponse = await fetch(dashSeekUrl)
            const actualStartTimeMs = Number(
              manifestResponse.headers.get('X-Dash-Start-Time-Ms') ??
                result.seekTimeMs,
            )

            await player.load(dashSeekUrl)

            // Set the stream offset so time calculations account for the seek position
            updatePlaybackState({
              currentTime: actualStartTimeMs,
              streamOffset: actualStartTimeMs,
            })

            if (wasPlaying) {
              video.play().catch(() => {
                // Ignore play errors during seek reload
              })
            }

            return actualStartTimeMs / 1000
          }

          // Fallback: clamp to available range
          console.warn(
            '[ShakaPlayer] Seeking past available content. No seek URL available.',
            { available: browserDuration, requested: seekTimeSeconds },
          )
          const clampedSeek = Math.max(0, browserDuration - 1)
          performSeek(clampedSeek)
          return clampedSeek
        }

        // Within seekable range - just seek directly
        updatePlaybackState({ streamOffset: 0 })
        performSeek(seekTimeSeconds)

        return seekTimeSeconds
      },
      [
        buildDashSeekUrl,
        buildRemuxSeekUrl,
        needsGopSeek,
        performSeek,
        seekToKeyframe,
        updatePlaybackState,
      ],
    )

    // Expose methods to parent via ref
    useImperativeHandle(
      ref,
      () => ({
        getPlayer: () => playerRef.current,
        getVideoElement: () => videoRef.current,
        seek: (time: number) => {
          // Debug: track seek entry
          const gopNeeded = needsGopSeek()
          const streamPlan = playbackStateRef.current.streamPlanJson
          console.log('[ShakaPlayer] seek() called:', {
            gopNeeded,
            parsedPlan: streamPlan ? JSON.parse(streamPlan) : null,
            streamPlanJson: streamPlan,
            time,
          })

          // For simple seek calls, use GoP-aware seeking if beneficial
          if (gopNeeded) {
            void performGopSeek(time)
          } else {
            performSeek(time)
          }
        },
        seekWithGop: async (time: number): Promise<number> => {
          return performGopSeek(time)
        },
      }),
      [needsGopSeek, performGopSeek, performSeek],
    )

    // Initialize Shaka Player
    useEffect(() => {
      if (!videoRef.current || !containerRef.current) return

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

      // Attach player to video element
      void player.attach(videoRef.current).then(() => {
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
    }, [onError, onPlayerReady])

    // Load media when playbackUrl changes
    useEffect(() => {
      const player = playerRef.current
      const video = videoRef.current

      if (!player || !video || !playbackState.playbackUrl) {
        return
      }

      // Load the manifest
      player
        .load(playbackState.playbackUrl)
        .then(() => {
          // Update duration when available
          if (video.duration && !Number.isNaN(video.duration)) {
            updateProgress(0, video.duration * 1000) // Convert to milliseconds
          }

          // Add trickplay thumbnails if available
          const trickplayUrl = playbackState.trickplayUrl
          if (trickplayUrl) {
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
    }, [playbackState.playbackUrl, updateProgress, onError])

    // Sync play/pause state
    useEffect(() => {
      const video = videoRef.current
      const player = playerRef.current
      if (!video) return

      if (playbackState.isPlaying) {
        // Only attempt to play if the player has loaded content
        // This prevents AbortError when play() is called before/during load
        if (player && video.readyState >= HTMLMediaElement.HAVE_CURRENT_DATA) {
          video.play().catch((error: unknown) => {
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
        video.pause()
      }
    }, [playbackState.isPlaying])

    // Sync volume
    useEffect(() => {
      const video = videoRef.current
      if (!video) return

      video.volume = playbackState.volume
    }, [playbackState.volume])

    // Sync mute state
    useEffect(() => {
      const video = videoRef.current
      if (!video) return

      video.muted = playbackState.isMuted
    }, [playbackState.isMuted])

    // Listen to video events and update playback state
    useEffect(() => {
      const video = videoRef.current
      if (!video) return

      const handleTimeUpdate = () => {
        // Update current time and buffered time
        let bufferedEnd = 0
        if (video.buffered.length > 0) {
          // Get the end of the buffered range that contains current time
          for (let i = 0; i < video.buffered.length; i++) {
            if (
              video.buffered.start(i) <= video.currentTime &&
              video.buffered.end(i) >= video.currentTime
            ) {
              bufferedEnd = video.buffered.end(i)
              break
            }
          }
          // If no range contains current time, use the last buffered range
          if (bufferedEnd === 0 && video.buffered.length > 0) {
            bufferedEnd = video.buffered.end(video.buffered.length - 1)
          }
        }
        const durationMs = Number.isFinite(video.duration)
          ? video.duration * 1000
          : 0

        // Account for stream offset when we've reloaded the stream from a seek position
        const streamOffset = playbackStateRef.current.streamOffset ?? 0
        const actualCurrentTime = video.currentTime * 1000 + streamOffset

        updatePlaybackState({
          bufferedTime: bufferedEnd * 1000 + streamOffset,
          currentTime: actualCurrentTime,
          duration: durationMs,
        })
      }

      const handleDurationChange = () => {
        if (!Number.isNaN(video.duration) && Number.isFinite(video.duration)) {
          updatePlaybackState({ duration: video.duration * 1000 })
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
                  currentItemId: latestPlayback.originator.id,
                  playbackSessionId: latestPlayback.playbackSessionId,
                  progressMs: Math.round(latestPlayback.currentTime),
                  status: 'ended',
                },
              },
            })
            .then((response) => {
              const payload = response.data?.decidePlayback
              if (payload) {
                updatePlaybackState({
                  capabilityProfileVersion: payload.capabilityProfileVersion,
                  capabilityVersionMismatch: payload.capabilityVersionMismatch,
                })
              }
            })
            .catch((error: unknown) => {
              handleErrorStandalone(error, {
                context: 'ShakaPlayer.decidePlayback',
                notify: false,
              })
            })
        }

        stopPlayback()
      }

      const handleVolumeChange = () => {
        updatePlaybackState({
          isMuted: video.muted,
          volume: video.volume,
        })
      }

      video.addEventListener('timeupdate', handleTimeUpdate)
      video.addEventListener('durationchange', handleDurationChange)
      video.addEventListener('ended', handleEnded)
      video.addEventListener('volumechange', handleVolumeChange)

      return () => {
        video.removeEventListener('timeupdate', handleTimeUpdate)
        video.removeEventListener('durationchange', handleDurationChange)
        video.removeEventListener('ended', handleEnded)
        video.removeEventListener('volumechange', handleVolumeChange)
      }
    }, [apolloClient, stopPlayback, updatePlaybackState])

    // Seek to specific time when currentTime changes externally
    const lastSeekTimeRef = useRef<number>(0)
    useEffect(() => {
      const video = videoRef.current
      if (!video) return

      // Account for stream offset - playbackState.currentTime is absolute position,
      // but video.currentTime is relative to the current stream start
      const streamOffset = playbackStateRef.current.streamOffset ?? 0
      const targetTime = (playbackState.currentTime - streamOffset) / 1000 // Convert from milliseconds and adjust for offset
      const currentTime = video.currentTime

      // Only seek if the difference is significant (more than 1 second)
      // and not caused by normal playback progression
      // Also ensure target time is non-negative (could be negative if offset is larger due to timing)
      if (targetTime >= 0 && Math.abs(targetTime - currentTime) > 1) {
        const now = Date.now()
        // Debounce seeks to prevent loops
        if (now - lastSeekTimeRef.current > 500) {
          video.currentTime = targetTime
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
        <video autoPlay className="h-full w-full" playsInline ref={videoRef} />
      </div>
    )
  },
)

// Export player instance getter for advanced usage
ShakaPlayer.displayName = 'ShakaPlayer'
