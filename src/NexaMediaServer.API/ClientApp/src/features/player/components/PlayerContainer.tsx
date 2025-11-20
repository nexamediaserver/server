import type { ReactNode } from 'react'

import { useApolloClient } from '@apollo/client/react'
import { useIdle, useThrottle } from '@uidotdev/usehooks'
import { useAtom } from 'jotai'
import { Duration } from 'luxon'
import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import IconForward10 from '~icons/material-symbols/forward-10'
import IconFullscreen from '~icons/material-symbols/fullscreen'
import IconChevronDown from '~icons/material-symbols/keyboard-arrow-down'
import IconChevronUp from '~icons/material-symbols/keyboard-arrow-up'
import IconPause from '~icons/material-symbols/pause'
import IconPlay from '~icons/material-symbols/play-arrow'
import IconReplay10 from '~icons/material-symbols/replay-10'
import IconNext from '~icons/material-symbols/skip-next'
import IconPrevious from '~icons/material-symbols/skip-previous'
import IconStop from '~icons/material-symbols/stop'
import IconVolumeDown from '~icons/material-symbols/volume-down'
import IconVolumeMute from '~icons/material-symbols/volume-mute'
import IconVolumeOff from '~icons/material-symbols/volume-off'
import IconVolumeUp from '~icons/material-symbols/volume-up'

import { graphql } from '@/shared/api/graphql'
import { Button } from '@/shared/components/ui/button'
import { Slider } from '@/shared/components/ui/slider'
import { useKeyboardShortcuts } from '@/shared/hooks'
import { handleErrorStandalone } from '@/shared/hooks/useErrorHandler'
import { buildPlaybackCapabilityInput } from '@/shared/lib/playbackCapabilities'
import { cn } from '@/shared/lib/utils'

import { createPlayerShortcuts } from '../config/keyboardShortcuts'
import { useMediaSession } from '../hooks/useMediaSession'
import { usePlayback } from '../hooks/usePlayback'
import { VideoMediaSessionProvider } from '../providers/VideoMediaSessionProvider'
import { type PlaybackState, playbackStateAtom } from '../store'
import { ShakaPlayer, type ShakaPlayerHandle } from './ShakaPlayer'

const PlaybackHeartbeatMutation = graphql(`
  mutation PlaybackHeartbeat($input: PlaybackHeartbeatInput!) {
    playbackHeartbeat(input: $input) {
      playbackSessionId
      capabilityProfileVersion
      capabilityVersionMismatch
    }
  }
`)

interface PlaybackHeartbeatResult {
  playbackHeartbeat?: null | {
    capabilityProfileVersion: number
    capabilityVersionMismatch: boolean
  }
}

/**
 * PlayerContainer component similar to Plex's web client.
 * Shows a fixed player bar at the bottom when playback is active.
 */
export function PlayerContainer(): ReactNode {
  const [playback] = useAtom(playbackStateAtom)
  const {
    setVolume,
    stopPlayback,
    toggleMaximize,
    toggleMute,
    togglePlayPause,
    updatePlaybackState,
  } = usePlayback()
  const apolloClient = useApolloClient()
  const playerRef = useRef<ShakaPlayerHandle>(null)
  const containerRef = useRef<HTMLDivElement>(null)
  const [tooltipVisible, setTooltipVisible] = useState(false)

  usePlaybackHeartbeat(playback, apolloClient, updatePlaybackState)

  // Track user idle state (3 seconds of inactivity)
  const isIdle = useIdle(3000)

  // Show controls when not maximized or when user is active (not idle)
  const showControls = !playback.maximized || !isIdle

  const {
    bufferedTimeMs: safeBufferedTime,
    currentTimeMs: safeCurrentTime,
    durationMs: safeDuration,
  } = useMemo(() => {
    const video = playerRef.current?.getVideoElement()

    // Prefer server-provided duration as it's authoritative and correct
    // for remuxed/transcoded streams where browser may report incorrect values
    const serverDurationMs =
      playback.serverDuration !== undefined &&
      Number.isFinite(playback.serverDuration) &&
      playback.serverDuration > 0
        ? playback.serverDuration
        : undefined

    const videoDurationMs =
      video && Number.isFinite(video.duration) && video.duration > 0
        ? video.duration * 1000
        : undefined

    const shakaDurationMs = (() => {
      const player = playerRef.current?.getPlayer()
      const end = player?.seekRange().end
      return end !== undefined && Number.isFinite(end) && end > 0
        ? end * 1000
        : undefined
    })()

    const stateDuration =
      Number.isFinite(playback.duration) && playback.duration > 0
        ? playback.duration
        : undefined

    // Priority: server duration > state duration > video element > shaka player
    const durationMs =
      serverDurationMs ??
      stateDuration ??
      videoDurationMs ??
      shakaDurationMs ??
      0

    let currentTimeMs = 0
    if (Number.isFinite(playback.currentTime) && playback.currentTime >= 0) {
      currentTimeMs = playback.currentTime
    } else if (
      video &&
      Number.isFinite(video.currentTime) &&
      video.currentTime >= 0
    ) {
      currentTimeMs = video.currentTime * 1000
    }

    const bufferedTimeMs =
      Number.isFinite(playback.bufferedTime) && playback.bufferedTime >= 0
        ? playback.bufferedTime
        : 0

    return { bufferedTimeMs, currentTimeMs, durationMs }
  }, [
    playback.serverDuration,
    playback.duration,
    playback.currentTime,
    playback.bufferedTime,
  ])

  // Use separate state for immediate UI updates and throttled thumbnail fetching
  const [tooltipPosition, setTooltipPosition] = useState({
    percentage: 0,
    time: 0,
  })

  // Throttle the tooltip position for thumbnail fetching to limit API call frequency
  // Throttle allows periodic updates during movement, unlike debounce which waits for movement to stop
  const throttledTooltipPosition = useThrottle(tooltipPosition, 150)

  const [thumbnail, setThumbnail] = useState<null | {
    height: number
    imageHeight: number
    imageUrl: string
    imageWidth: number
    positionX: number
    positionY: number
    width: number
  }>(null)
  const progressBarRef = useRef<HTMLInputElement>(null)
  const tooltipRef = useRef<HTMLDivElement>(null)

  const toggleFullscreen = () => {
    if (document.fullscreenElement) {
      // Exit fullscreen
      document.exitFullscreen().catch((err: unknown) => {
        handleErrorStandalone(err, {
          context: 'PlayerContainer.fullscreen',
          notify: false,
        })
      })
    } else {
      // Enter fullscreen on the entire document
      document.documentElement.requestFullscreen().catch((err: unknown) => {
        handleErrorStandalone(err, {
          context: 'PlayerContainer.fullscreen',
          notify: false,
        })
      })
    }
  }

  // Notify Shaka Player when fullscreen state changes
  useEffect(() => {
    const handleFullscreenChange = () => {
      const player = playerRef.current?.getPlayer()
      if (player) {
        // This ensures Shaka's UI (if any) updates for fullscreen
        player.dispatchEvent(new Event('fullscreenchange'))
      }
    }

    document.addEventListener('fullscreenchange', handleFullscreenChange)

    return () => {
      document.removeEventListener('fullscreenchange', handleFullscreenChange)
    }
  }, [])

  // Exit fullscreen when maximized state becomes false
  useEffect(() => {
    if (!playback.maximized && document.fullscreenElement) {
      document.exitFullscreen().catch((err: unknown) => {
        handleErrorStandalone(err, {
          context: 'PlayerContainer.fullscreen',
          notify: false,
        })
      })
    }
  }, [playback.maximized])

  // Fetch thumbnail based on throttled position to limit API call frequency
  useEffect(() => {
    if (!tooltipVisible || safeDuration === 0 || !playback.originator) {
      return
    }

    const player = playerRef.current?.getPlayer()
    if (player && throttledTooltipPosition.time > 0) {
      const timeInSeconds = throttledTooltipPosition.time / 1000
      player
        .getThumbnails(null, timeInSeconds)
        .then((thumbnailData) => {
          if (thumbnailData?.uris[0]) {
            setThumbnail({
              height: thumbnailData.height,
              imageHeight: thumbnailData.imageHeight,
              imageUrl: thumbnailData.uris[0],
              imageWidth: thumbnailData.imageWidth,
              positionX: thumbnailData.positionX,
              positionY: thumbnailData.positionY,
              width: thumbnailData.width,
            })
          } else {
            setThumbnail(null)
          }
        })
        .catch((error: unknown) => {
          handleErrorStandalone(error, {
            context: 'PlayerContainer.thumbnail',
            notify: false,
          })
          setThumbnail(null)
        })
    }
  }, [
    throttledTooltipPosition,
    tooltipVisible,
    safeDuration,
    playback.originator,
  ])

  const handleProgressChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    if (!playerRef.current || safeDuration === 0) return

    const newTime = event.currentTarget.valueAsNumber

    // Seek to the new time (in seconds)
    playerRef.current.seek(newTime / 1000)
  }

  const handleRewind10 = useCallback(() => {
    if (!playerRef.current) return
    const currentTimeSeconds = playback.currentTime / 1000
    const newTime = Math.max(0, currentTimeSeconds - 10)
    playerRef.current.seek(newTime)
  }, [playback.currentTime])

  const handleFastForward10 = useCallback(() => {
    if (!playerRef.current) return
    const currentTimeSeconds = playback.currentTime / 1000
    const durationSeconds = safeDuration / 1000
    const newTime = Math.min(durationSeconds, currentTimeSeconds + 10)
    playerRef.current.seek(newTime)
  }, [playback.currentTime, safeDuration])

  const handleSkipBack10Minutes = useCallback(() => {
    if (!playerRef.current) return
    const currentTimeSeconds = playback.currentTime / 1000
    const newTime = Math.max(0, currentTimeSeconds - 600) // 600 seconds = 10 minutes
    playerRef.current.seek(newTime)
  }, [playback.currentTime])

  const handleJumpForward10Minutes = useCallback(() => {
    if (!playerRef.current) return
    const currentTimeSeconds = playback.currentTime / 1000
    const durationSeconds = safeDuration / 1000
    const newTime = Math.min(durationSeconds, currentTimeSeconds + 600) // 600 seconds = 10 minutes
    playerRef.current.seek(newTime)
  }, [playback.currentTime, safeDuration])

  const handleSeek = useCallback((timeInSeconds: number) => {
    if (!playerRef.current) return
    playerRef.current.seek(timeInSeconds)
  }, [])

  // Create Media Session provider for video playback
  const mediaSessionProvider = useMemo(() => {
    if (!playback.originator) return null

    return new VideoMediaSessionProvider(playback, {
      onPause: togglePlayPause,
      onPlay: togglePlayPause,
      onSeek: handleSeek,
      onSeekBackward: handleRewind10,
      onSeekForward: handleFastForward10,
      onStop: stopPlayback,
    })
  }, [
    playback,
    togglePlayPause,
    handleSeek,
    handleRewind10,
    handleFastForward10,
    stopPlayback,
  ])

  // Integrate Media Session API
  useMediaSession(mediaSessionProvider, playback, !!playback.originator)

  // Configure player-specific keyboard shortcuts (only when playback is active)
  const playerShortcuts = useMemo(
    () =>
      createPlayerShortcuts(
        {
          forward10Seconds: handleFastForward10,
          jumpForward10Minutes: handleJumpForward10Minutes,
          rewind10Seconds: handleRewind10,
          skipBack10Minutes: handleSkipBack10Minutes,
          togglePlayPause,
        },
        {
          isPlayerMaximized: () => playback.maximized,
        },
      ),
    [
      handleFastForward10,
      handleJumpForward10Minutes,
      handleRewind10,
      handleSkipBack10Minutes,
      playback.maximized,
      togglePlayPause,
    ],
  )

  // Register player shortcuts (only active when playback is active)
  useKeyboardShortcuts(playerShortcuts, !!playback.originator)

  const handleProgressMouseMove = (e: React.MouseEvent<HTMLDivElement>) => {
    if (!progressBarRef.current || safeDuration === 0) return

    const rect = progressBarRef.current.getBoundingClientRect()
    const mouseX = e.clientX - rect.left
    const percentage = mouseX / rect.width
    const targetTime = percentage * safeDuration

    // Update tooltip position immediately for smooth UI feedback
    setTooltipPosition({ percentage, time: targetTime })
  }

  // Calculate clamped tooltip position to prevent it from going off-screen
  const getClampedTooltipPosition = () => {
    if (!progressBarRef.current || !tooltipRef.current) {
      return {
        left: `${String(tooltipPosition.percentage * 100)}%`,
        transform: 'translateX(-50%)',
      }
    }

    const progressRect = progressBarRef.current.getBoundingClientRect()
    const tooltipRect = tooltipRef.current.getBoundingClientRect()

    // Calculate the desired center position of the tooltip
    const desiredLeft =
      progressRect.left + tooltipPosition.percentage * progressRect.width

    // Calculate tooltip half-width
    const tooltipHalfWidth = tooltipRect.width / 2

    // Padding from screen edges
    const edgePadding = 8

    // Calculate clamped position
    const minLeft = edgePadding + tooltipHalfWidth
    const maxLeft = globalThis.innerWidth - edgePadding - tooltipHalfWidth
    const clampedLeft = Math.max(minLeft, Math.min(maxLeft, desiredLeft))

    // Calculate the offset needed
    const offset = clampedLeft - progressRect.left
    const offsetPercentage = (offset / progressRect.width) * 100

    // Calculate max width to prevent overflow
    const leftEdge = clampedLeft - tooltipHalfWidth
    const rightEdge = clampedLeft + tooltipHalfWidth

    let maxWidth: string | undefined
    if (leftEdge < edgePadding) {
      // Constrain from left edge
      maxWidth = `${String((clampedLeft - edgePadding) * 2)}px`
    } else if (rightEdge > globalThis.innerWidth - edgePadding) {
      // Constrain from right edge
      maxWidth = `${String((globalThis.innerWidth - edgePadding - clampedLeft) * 2)}px`
    }

    return {
      left: `${String(offsetPercentage)}%`,
      maxWidth,
      transform: 'translateX(-50%)',
    }
  }

  // Convert linear slider value to exponential volume (x³ curve)
  const linearToVolume = (linear: number): number => {
    return Math.pow(linear, 3)
  }

  // Convert exponential volume to linear slider value
  const volumeToLinear = (volume: number): number => {
    return Math.pow(volume, 1 / 3)
  }

  const handleVolumeChange = (values: number[]) => {
    const linear = values[0] ?? 0
    const volume = linearToVolume(linear)
    setVolume(volume)
  }

  // Get appropriate volume icon based on volume level and mute state
  const getVolumeIcon = () => {
    if (playback.isMuted || playback.volume === 0) {
      return <IconVolumeOff className="h-5 w-5" />
    } else if (playback.volume < 0.3) {
      return <IconVolumeMute className="h-5 w-5" />
    } else if (playback.volume < 0.7) {
      return <IconVolumeDown className="h-5 w-5" />
    } else {
      return <IconVolumeUp className="h-5 w-5" />
    }
  }

  const currentLinearVolume = volumeToLinear(playback.volume)

  if (!playback.originator) {
    return null
  }

  return (
    <div
      className={cn('contents', playback.maximized && isIdle && 'cursor-none')}
      ref={containerRef}
    >
      {playback.maximized && (
        <div
          className={cn(
            `
              fixed top-0 z-50 flex h-16 w-full flex-row items-center
              justify-between border-b border-border bg-background/70
              transition-opacity duration-300
            `,
            showControls ? '' : 'pointer-events-none opacity-0',
          )}
        >
          <Button
            className="ml-4"
            onClick={toggleMaximize}
            size="icon"
            variant="ghost"
          >
            <IconChevronDown className="h-6 w-6" />
          </Button>
          <Button
            className="mr-4"
            onClick={toggleFullscreen}
            size="icon"
            variant="ghost"
          >
            <IconFullscreen className="h-6 w-6" />
          </Button>
        </div>
      )}
      <button
        aria-label={playback.isPlaying ? 'Pause video' : 'Play video'}
        className={cn(
          playback.maximized
            ? 'flex h-full w-full items-center justify-center'
            : 'absolute bottom-2 left-2 z-1 aspect-video h-20',
        )}
        disabled={!playback.maximized}
        onClick={playback.maximized ? togglePlayPause : undefined}
        type="button"
      >
        <ShakaPlayer
          className={cn(
            'h-full w-full',
            !(playback.maximized && isIdle) && 'cursor-pointer',
          )}
          ref={playerRef}
        />
      </button>
      <div
        className={cn(
          playback.maximized
            ? `
              fixed bottom-0 z-50 w-full bg-background/70 transition-opacity
              duration-300
            `
            : `relative bg-background`,
          `flex h-[100px] flex-col justify-center border-t border-border`,
          playback.maximized &&
            !showControls &&
            'pointer-events-none opacity-0',
        )}
      >
        {/* Progress Bar */}
        <div className="relative -mt-2 w-full py-2">
          <input
            aria-label="Seek"
            className={cn(
              'absolute inset-0 z-10 h-6 w-full cursor-pointer opacity-0',
            )}
            max={safeDuration}
            min={0}
            onBlur={() => {
              setTooltipVisible(false)
              setThumbnail(null)
            }}
            onChange={handleProgressChange}
            onFocus={() => {
              setTooltipVisible(true)
            }}
            onMouseEnter={() => {
              setTooltipVisible(true)
            }}
            onMouseLeave={() => {
              setTooltipVisible(false)
              setThumbnail(null)
            }}
            onMouseMove={handleProgressMouseMove}
            ref={progressBarRef}
            step={1}
            type="range"
            value={safeCurrentTime}
          />
          <div className="relative h-1 bg-stone-900">
            {/* Buffer indicator */}
            <div
              className="absolute h-full bg-primary/20 transition-all"
              style={{
                width: `${String(safeDuration > 0 ? (safeBufferedTime / safeDuration) * 100 : 0)}%`,
              }}
            />
            {/* Current progress */}
            <div
              className="absolute h-full bg-primary transition-all"
              style={{
                width: `${String(safeDuration > 0 ? (safeCurrentTime / safeDuration) * 100 : 0)}%`,
              }}
            />
          </div>
        </div>
        {/* Custom tooltip */}
        {tooltipVisible && (
          <div
            className="pointer-events-none absolute bottom-full z-50 mb-2"
            ref={tooltipRef}
            style={{
              ...getClampedTooltipPosition(),
              width: 'max-content',
            }}
          >
            {/* Thumbnail preview */}
            {thumbnail && (
              <div
                className={`
                  mb-2 aspect-video max-h-40 overflow-hidden border
                  border-primary bg-background shadow-md
                `}
              >
                <img
                  alt=""
                  aria-hidden="true"
                  className={`h-full w-full object-contain`}
                  src={thumbnail.imageUrl}
                />
              </div>
            )}
            {/* Time tooltip */}
            <div
              className={cn(
                thumbnail && 'absolute bottom-4 left-1/2 -translate-x-1/2',
                `
                  rounded-md bg-primary px-3 py-1.5 text-xs
                  text-primary-foreground shadow-md
                `,
              )}
            >
              {formatDuration(tooltipPosition.time)}
            </div>
          </div>
        )}

        <div className="flex h-full flex-row items-center justify-between">
          <div className="flex grow items-center">
            {!playback.maximized && (
              <div className="group relative mb-2.5 ml-2 h-20">
                <div className="aspect-video h-full" />
                <Button
                  aria-label="Maximize player"
                  className={`
                    absolute top-0 z-2 h-full w-full rounded-none opacity-0
                    transition-opacity duration-200 ease-in-out
                    group-hover:opacity-100
                  `}
                  onClick={toggleMaximize}
                  size="icon"
                  variant="ghost"
                >
                  <IconChevronUp />
                </Button>
              </div>
            )}
            {/* Media Info */}
            <div className="mb-2.5 ml-3 text-nowrap">
              <div className="truncate text-sm font-medium">
                {playback.originator.title}
              </div>
              <div className="text-sm text-muted-foreground">
                <span>{formatDuration(safeCurrentTime)}</span>
                <span className="mx-1">/</span>
                <span>{formatDuration(safeDuration)}</span>
              </div>
            </div>
          </div>
          {/* Player Controls */}
          <div className="mx-5 flex shrink items-center justify-center pr-9">
            <Button
              aria-label="Previous"
              // onClick={handlePrevious}
              size="icon"
              variant="ghost"
            >
              <IconPrevious />
            </Button>
            <Button
              aria-label="Rewind 10 seconds"
              onClick={handleRewind10}
              size="icon"
              variant="ghost"
            >
              <IconReplay10 />
            </Button>
            <Button
              aria-label="Play/Pause"
              onClick={togglePlayPause}
              size="icon"
              variant="ghost"
            >
              {playback.isPlaying ? <IconPause /> : <IconPlay />}
            </Button>
            <Button
              aria-label="Fast Forward 10 seconds"
              onClick={handleFastForward10}
              size="icon"
              variant="ghost"
            >
              <IconForward10 />
            </Button>
            <Button
              aria-label="Next"
              // onClick={handleNext}
              size="icon"
              variant="ghost"
            >
              <IconNext />
            </Button>
            <Button
              aria-label="Stop"
              onClick={stopPlayback}
              size="icon"
              variant="ghost"
            >
              <IconStop />
            </Button>
          </div>
          <div className="mb-2.5 flex grow items-center justify-end pr-5">
            {/* Volume Controls */}
            <Button
              aria-label="Mute/Unmute"
              onClick={toggleMute}
              size="icon"
              variant="ghost"
            >
              {getVolumeIcon()}
            </Button>
            <Slider
              aria-label="Volume"
              className="data-[orientation=vertical]:min-h-20"
              max={1}
              min={0}
              onValueChange={handleVolumeChange}
              orientation="vertical"
              step={0.01}
              value={[currentLinearVolume]}
            />
          </div>
        </div>
      </div>
    </div>
  )
}

/**
 * Formats milliseconds to a human-readable duration string (H:MM:SS or M:SS)
 */
function formatDuration(ms: number): string {
  if (!Number.isFinite(ms) || ms < 0) {
    return '--:--'
  }

  const duration = Duration.fromMillis(ms)
  const hours = Math.floor(duration.as('hours'))

  if (hours > 0) {
    return duration.toFormat('h:mm:ss')
  }
  return duration.toFormat('m:ss')
}

function usePlaybackHeartbeat(
  playback: PlaybackState,
  apolloClient: ReturnType<typeof useApolloClient>,
  updatePlaybackState: (updates: Partial<PlaybackState>) => void,
): void {
  const throttledPosition = useThrottle(playback.currentTime, 5000)

  useEffect(() => {
    if (!playback.playbackSessionId) return

    const playheadMs = Math.round(
      Number.isFinite(throttledPosition)
        ? throttledPosition
        : playback.currentTime,
    )

    const input = {
      capability: playback.capabilityVersionMismatch
        ? buildPlaybackCapabilityInput(playback.capabilityProfileVersion)
        : undefined,
      capabilityProfileVersion: playback.capabilityProfileVersion ?? undefined,
      playbackSessionId: playback.playbackSessionId,
      playheadMs,
      state: playback.isPlaying ? 'playing' : 'paused',
    }

    void apolloClient
      .mutate<PlaybackHeartbeatResult>({
        mutation: PlaybackHeartbeatMutation,
        variables: { input },
      })
      .then((response) => {
        const payload = response.data?.playbackHeartbeat
        if (!payload) return

        updatePlaybackState({
          capabilityProfileVersion: payload.capabilityProfileVersion,
          capabilityVersionMismatch: payload.capabilityVersionMismatch,
        })
      })
      .catch((error: unknown) => {
        handleErrorStandalone(error, {
          context: 'PlayerContainer.heartbeat',
          notify: false,
        })
      })
  }, [
    apolloClient,
    playback.capabilityProfileVersion,
    playback.capabilityVersionMismatch,
    playback.currentTime,
    playback.isPlaying,
    playback.playbackSessionId,
    throttledPosition,
    updatePlaybackState,
  ])
}
