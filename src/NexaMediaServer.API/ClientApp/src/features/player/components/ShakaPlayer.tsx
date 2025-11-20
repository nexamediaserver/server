import { useAtom } from 'jotai'
import { forwardRef, useEffect, useImperativeHandle, useRef } from 'react'
import shaka from 'shaka-player'

import { playbackStateAtom } from '@/store/playback'

import { usePlayback } from '../hooks/usePlayback'

export interface ShakaPlayerHandle {
  getPlayer: () => null | shaka.Player
  getVideoElement: () => HTMLVideoElement | null
  seek: (time: number) => void
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
    const videoRef = useRef<HTMLVideoElement>(null)
    const containerRef = useRef<HTMLDivElement>(null)
    const playerRef = useRef<null | shaka.Player>(null)

    const [playbackState] = useAtom(playbackStateAtom)
    const { stopPlayback, updatePlaybackState, updateProgress } = usePlayback()

    // Expose methods to parent via ref
    useImperativeHandle(ref, () => ({
      getPlayer: () => playerRef.current,
      getVideoElement: () => videoRef.current,
      seek: (time: number) => {
        if (videoRef.current) {
          videoRef.current.currentTime = time
        }
      },
    }))

    // Initialize Shaka Player
    useEffect(() => {
      if (!videoRef.current || !containerRef.current) return

      // Install polyfills
      shaka.polyfill.installAll()

      // Check if browser is supported
      if (!shaka.Player.isBrowserSupported()) {
        console.error('Browser not supported for Shaka Player')
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
        console.error('Shaka Player Error:', errorEvent.detail)
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

    // Load media when directPlayUrl changes
    useEffect(() => {
      const player = playerRef.current
      const video = videoRef.current

      if (!player || !video || !playbackState.directPlayUrl) {
        return
      }

      // Load the manifest
      player
        .load(playbackState.directPlayUrl)
        .then(() => {
          console.log('Media loaded successfully')

          // Update duration when available
          if (video.duration && !isNaN(video.duration)) {
            updateProgress(0, video.duration * 1000) // Convert to milliseconds
          }

          // Add trickplay thumbnails if available
          const trickplayUrl = playbackState.originator?.trickplayUrl
          if (trickplayUrl) {
            player
              .addThumbnailsTrack(trickplayUrl)
              .then(() => {
                console.log('Trickplay thumbnails loaded successfully')
              })
              .catch((error: unknown) => {
                console.error('Error loading trickplay thumbnails:', error)
              })
          }
        })
        .catch((error: unknown) => {
          console.error('Error loading media:', error)
          if (error && typeof error === 'object' && 'severity' in error) {
            onError?.(error as shaka.util.Error)
          }
        })
    }, [
      playbackState.directPlayUrl,
      playbackState.originator?.trickplayUrl,
      updateProgress,
      onError,
    ])

    // Sync play/pause state
    useEffect(() => {
      const video = videoRef.current
      if (!video) return

      if (playbackState.isPlaying) {
        video.play().catch((error: unknown) => {
          console.error('Error playing video:', error)
        })
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
        updatePlaybackState({
          bufferedTime: bufferedEnd * 1000,
          currentTime: video.currentTime * 1000,
          duration: video.duration * 1000,
        })
      }

      const handleDurationChange = () => {
        if (!isNaN(video.duration)) {
          updatePlaybackState({ duration: video.duration * 1000 })
        }
      }

      const handleEnded = () => {
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
    }, [updateProgress, updatePlaybackState, stopPlayback])

    // Seek to specific time when currentTime changes externally
    const lastSeekTimeRef = useRef<number>(0)
    useEffect(() => {
      const video = videoRef.current
      if (!video) return

      const targetTime = playbackState.currentTime / 1000 // Convert from milliseconds
      const currentTime = video.currentTime

      // Only seek if the difference is significant (more than 1 second)
      // and not caused by normal playback progression
      if (Math.abs(targetTime - currentTime) > 1) {
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
        <video autoPlay className="h-full w-full" playsInline ref={videoRef} />
      </div>
    )
  },
)

// Export player instance getter for advanced usage
ShakaPlayer.displayName = 'ShakaPlayer'
