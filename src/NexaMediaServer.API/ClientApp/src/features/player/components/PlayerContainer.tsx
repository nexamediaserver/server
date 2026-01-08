import type { ReactNode } from 'react'

import { useApolloClient } from '@apollo/client/react'
import { useIdle, useThrottle } from '@uidotdev/usehooks'
import { useAtom } from 'jotai'
import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import IconClose from '~icons/material-symbols/close'
import IconFullscreen from '~icons/material-symbols/fullscreen'
import IconChevronDown from '~icons/material-symbols/keyboard-arrow-down'
import IconChevronUp from '~icons/material-symbols/keyboard-arrow-up'
import IconQueueMusic from '~icons/material-symbols/queue-music'

import { Button } from '@/shared/components/ui/button'
import { type KeyboardShortcut, useKeyboardShortcuts } from '@/shared/hooks'
import { handleErrorStandalone } from '@/shared/hooks/useErrorHandler'
import { buildPlaybackCapabilityInput } from '@/shared/lib/playbackCapabilities'
import { cn } from '@/shared/lib/utils'

import { createPlayerShortcuts } from '../config/keyboardShortcuts'
import { PlaybackHeartbeatDocument } from '../graphql/playbackHeartbeat'
import { useImagePrefetch } from '../hooks/useImagePrefetch'
import {
  type MediaSessionProvider,
  useMediaSession,
} from '../hooks/useMediaSession'
import { usePlayback } from '../hooks/usePlayback'
import { usePlaylist } from '../hooks/usePlaylist'
import { usePlaylistNavigation } from '../hooks/usePlaylistNavigation'
import { useStopPlayback } from '../hooks/useStopPlayback'
import { AudioMediaSessionProvider } from '../providers/AudioMediaSessionProvider'
import { VideoMediaSessionProvider } from '../providers/VideoMediaSessionProvider'
import { type PlaybackState, playbackStateAtom } from '../store'
import { AudioPlayer, type AudioPlayerHandle } from './AudioPlayer'
import {
  ImageControls,
  MediaInfo,
  PlaybackControls,
  PlayerMenu,
  ProgressBar,
  VolumeControls,
} from './controls'
import { ImageViewer, type ImageViewerHandle } from './ImageViewer'
import { PlaylistDrawer } from './playlist/PlaylistDrawer'
import { ShakaPlayer, type ShakaPlayerHandle } from './ShakaPlayer'
import {
  AudioPlayerStatsProvider,
  ImagePlayerStatsProvider,
  PlayerStats,
  type PlayerStatsProvider,
  VideoPlayerStatsProvider,
} from './stats'

interface PlaybackHeartbeatResult {
  playbackHeartbeat?: null | {
    capabilityProfileVersion: number
    capabilityVersionMismatch: boolean
  }
}

/**
 * PlayerContainer component similar to Plex's web client.
 * Shows a fixed player bar at the bottom when playback is active.
 * Supports video, audio, and image playback with type-specific UI.
 */
export function PlayerContainer(): ReactNode {
  const [playback] = useAtom(playbackStateAtom)
  const {
    setVolume,
    toggleMaximize,
    toggleMute,
    togglePlayPause,
    updatePlaybackState,
  } = usePlayback()
  const { stopPlayback } = useStopPlayback()
  const { hasNext, hasPrevious, navigateNext, navigatePrevious } =
    usePlaylistNavigation()
  const {
    isRepeat,
    isShuffle,
    playlistIndex,
    setRepeat,
    setShuffle,
    totalCount: playlistTotalCount,
  } = usePlaylist()
  const apolloClient = useApolloClient()
  const playerRef = useRef<ShakaPlayerHandle>(null)
  const audioPlayerRef = useRef<AudioPlayerHandle>(null)
  const imageViewerRef = useRef<ImageViewerHandle>(null)
  const containerRef = useRef<HTMLDivElement>(null)
  const [tooltipVisible, setTooltipVisible] = useState(false)
  const [isPlaylistOpen, setIsPlaylistOpen] = useState(false)
  const [mediaSessionProvider, setMediaSessionProvider] =
    useState<MediaSessionProvider | null>(null)
  const [playerShortcuts, setPlayerShortcuts] = useState<KeyboardShortcut[]>([])
  const [statsProvider, setStatsProvider] = useState<
    PlayerStatsProvider | undefined
  >(undefined)
  const [statsEnabled, setStatsEnabled] = useState(false)

  // Determine which player type to use based on media type
  const isAudio = playback.mediaType === 'music'
  const isImage = playback.mediaType === 'photo'
  const isVideo = !isAudio && !isImage

  useImagePrefetch({ windowSize: 3 })

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
    // For images, there's no duration/time - return zeros
    if (isImage) {
      return { bufferedTimeMs: 0, currentTimeMs: 0, durationMs: 0 }
    }

    // Prefer server-provided duration as it's authoritative and correct
    // for remuxed/transcoded streams where browser may report incorrect values
    const serverDurationMs =
      playback.serverDuration !== undefined &&
      Number.isFinite(playback.serverDuration) &&
      playback.serverDuration > 0
        ? playback.serverDuration
        : undefined

    const stateDuration =
      Number.isFinite(playback.duration) && playback.duration > 0
        ? playback.duration
        : undefined

    // Priority: server duration > state duration > fallback 0
    const durationMs = serverDurationMs ?? stateDuration ?? 0

    let currentTimeMs = 0
    if (Number.isFinite(playback.currentTime) && playback.currentTime >= 0) {
      currentTimeMs = playback.currentTime
    }

    const bufferedTimeMs =
      Number.isFinite(playback.bufferedTime) && playback.bufferedTime >= 0
        ? playback.bufferedTime
        : 0

    return { bufferedTimeMs, currentTimeMs, durationMs }
  }, [
    isImage,
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

  // Fetch thumbnail based on throttled position to limit API call frequency (video only)
  useEffect(() => {
    if (
      !tooltipVisible ||
      safeDuration === 0 ||
      !playback.originator ||
      !isVideo
    ) {
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
    isVideo,
  ])

  // Get the active player reference for seek operations
  const getActivePlayer = useCallback(() => {
    if (isAudio) return audioPlayerRef.current
    if (isVideo) return playerRef.current
    return null
  }, [isAudio, isVideo])

  const handleProgressChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const activePlayer = getActivePlayer()
    if (!activePlayer || safeDuration === 0) return

    const newTime = event.currentTarget.valueAsNumber

    // Seek to the new time (in seconds)
    activePlayer.seek(newTime / 1000)
  }

  const handleRewind10 = useCallback(() => {
    const activePlayer = getActivePlayer()
    if (!activePlayer) return
    const currentTimeSeconds = playback.currentTime / 1000
    const newTime = Math.max(0, currentTimeSeconds - 10)
    activePlayer.seek(newTime)
  }, [getActivePlayer, playback.currentTime])

  const handleFastForward10 = useCallback(() => {
    const activePlayer = getActivePlayer()
    if (!activePlayer) return
    const currentTimeSeconds = playback.currentTime / 1000
    const durationSeconds = safeDuration / 1000
    const newTime = Math.min(durationSeconds, currentTimeSeconds + 10)
    activePlayer.seek(newTime)
  }, [getActivePlayer, playback.currentTime, safeDuration])

  const handleSkipBack10Minutes = useCallback(() => {
    const activePlayer = getActivePlayer()
    if (!activePlayer) return
    const currentTimeSeconds = playback.currentTime / 1000
    const newTime = Math.max(0, currentTimeSeconds - 600) // 600 seconds = 10 minutes
    activePlayer.seek(newTime)
  }, [getActivePlayer, playback.currentTime])

  const handleJumpForward10Minutes = useCallback(() => {
    const activePlayer = getActivePlayer()
    if (!activePlayer) return
    const currentTimeSeconds = playback.currentTime / 1000
    const durationSeconds = safeDuration / 1000
    const newTime = Math.min(durationSeconds, currentTimeSeconds + 600) // 600 seconds = 10 minutes
    activePlayer.seek(newTime)
  }, [getActivePlayer, playback.currentTime, safeDuration])

  const handleSeek = useCallback(
    (timeInSeconds: number) => {
      const activePlayer = getActivePlayer()
      if (!activePlayer) return
      activePlayer.seek(timeInSeconds)
    },
    [getActivePlayer],
  )

  // Update Media Session provider based on media type
  // Note: This uses useEffect + setState (not useMemo) because the action callbacks
  // access refs (via handleSeek, handleRewind10, etc.), which is only safe in effects.
  useEffect(() => {
    if (!playback.originator || isImage) {
      // eslint-disable-next-line react-hooks/set-state-in-effect -- Deriving state from callbacks that access refs
      setMediaSessionProvider(null)
      return
    }

    const actions = {
      onPause: togglePlayPause,
      onPlay: togglePlayPause,
      onSeek: handleSeek,
      onSeekBackward: handleRewind10,
      onSeekForward: handleFastForward10,
      onStop: stopPlayback,
    }

    if (isAudio) {
      setMediaSessionProvider(new AudioMediaSessionProvider(playback, actions))
    } else {
      setMediaSessionProvider(new VideoMediaSessionProvider(playback, actions))
    }
  }, [
    handleFastForward10,
    handleRewind10,
    handleSeek,
    isAudio,
    isImage,
    playback,
    stopPlayback,
    togglePlayPause,
  ])

  // Integrate Media Session API
  useMediaSession(mediaSessionProvider, playback, !!playback.originator)

  // Update stats provider based on media type
  // Note: This uses useEffect + setState (not useMemo) because the provider callbacks
  // access refs (audioPlayerRef, playerRef), which is only safe in effects.
  useEffect(() => {
    if (!playback.originator) {
      // eslint-disable-next-line react-hooks/set-state-in-effect -- Deriving state from callbacks that access refs
      setStatsProvider(undefined)
      return
    }

    if (isAudio) {
      setStatsProvider(
        new AudioPlayerStatsProvider({
          getMediaElement: () =>
            audioPlayerRef.current?.getMediaElement() ?? null,
          getPlayer: () => audioPlayerRef.current?.getPlayer() ?? null,
          playbackState: playback,
        }),
      )
    } else if (isImage) {
      setStatsProvider(
        new ImagePlayerStatsProvider({
          playbackState: playback,
        }),
      )
    } else {
      setStatsProvider(
        new VideoPlayerStatsProvider({
          getMediaElement: () => playerRef.current?.getMediaElement() ?? null,
          getPlayer: () => playerRef.current?.getPlayer() ?? null,
          playbackState: playback,
        }),
      )
    }
  }, [isAudio, isImage, playback])

  // Toggle player stats visibility
  const toggleStats = useCallback(() => {
    setStatsEnabled((prev) => !prev)
  }, [])

  // Update player-specific keyboard shortcuts
  // Note: This uses useEffect + setState (not useMemo) because the shortcut callbacks
  // access refs (via handleFastForward10, etc.), which is only safe in effects.
  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect -- Deriving state from callbacks that access refs
    setPlayerShortcuts(
      createPlayerShortcuts(
        {
          forward10Seconds: handleFastForward10,
          jumpForward10Minutes: handleJumpForward10Minutes,
          rewind10Seconds: handleRewind10,
          skipBack10Minutes: handleSkipBack10Minutes,
          togglePlayPause,
          toggleStats,
        },
        {
          isPlayerMaximized: () => playback.maximized,
        },
      ),
    )
  }, [
    handleFastForward10,
    handleJumpForward10Minutes,
    handleRewind10,
    handleSkipBack10Minutes,
    playback.maximized,
    togglePlayPause,
    toggleStats,
  ])

  // Register player shortcuts (only active when playback is active and not for images)
  useKeyboardShortcuts(playerShortcuts, !!playback.originator && !isImage)

  const handleProgressMouseMove = (e: React.MouseEvent<HTMLDivElement>) => {
    if (!progressBarRef.current || safeDuration === 0) return

    const rect = progressBarRef.current.getBoundingClientRect()
    const mouseX = e.clientX - rect.left
    const percentage = mouseX / rect.width
    const targetTime = percentage * safeDuration

    // Update tooltip position immediately for smooth UI feedback
    setTooltipPosition({ percentage, time: targetTime })
  }

  // Handlers for tooltip visibility
  const handleTooltipVisibilityChange = useCallback((visible: boolean) => {
    setTooltipVisible(visible)
    if (!visible) {
      setThumbnail(null)
    }
  }, [])

  // Handler for volume change
  const handleVolumeChange = useCallback(
    (values: number[]) => {
      const newVolume = values[0] ?? 0
      setVolume(newVolume)
    },
    [setVolume],
  )

  // Image viewer zoom handlers
  const handleZoomIn = useCallback(() => {
    imageViewerRef.current?.zoomIn()
  }, [])

  const handleZoomOut = useCallback(() => {
    imageViewerRef.current?.zoomOut()
  }, [])

  const handleResetZoom = useCallback(() => {
    imageViewerRef.current?.resetTransform()
  }, [])

  if (!playback.originator) {
    return null
  }

  // Render image viewer with its own control bar
  if (isImage) {
    return (
      <>
        {/* Player Stats Overlay */}
        <PlayerStats
          enabled={statsEnabled}
          onDisable={() => {
            setStatsEnabled(false)
          }}
          provider={statsProvider}
        />
        <div
          className={cn(
            'contents',
            playback.maximized && isIdle && 'cursor-none',
          )}
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
          <div
            className={cn(
              playback.maximized
                ? 'flex h-full w-full items-center justify-center'
                : 'absolute bottom-2 left-2 z-1 aspect-square h-20',
            )}
          >
            <ImageViewer
              className="h-full w-full"
              playback={playback}
              ref={imageViewerRef}
            />
          </div>
          <div
            className={cn(
              playback.maximized
                ? `
                  fixed bottom-0 z-50 w-full bg-background/70 transition-opacity
                  duration-300
                `
                : `relative bg-background`,
              `flex h-25 flex-col justify-center border-t border-border`,
              playback.maximized &&
                !showControls &&
                'pointer-events-none opacity-0',
            )}
          >
            <div className="grid h-full grid-cols-[1fr_auto_1fr] items-center">
              <div className="flex items-center">
                {!playback.maximized && (
                  <div className="group relative mb-2.5 ml-2 h-20">
                    <div className="aspect-square h-full" />
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
                <MediaInfo
                  playlistIndex={playlistIndex}
                  playlistTotalCount={playlistTotalCount}
                  title={playback.originator.title}
                />
              </div>
              <ImageControls
                hasNext={hasNext}
                hasPrevious={hasPrevious}
                isMaximized={playback.maximized}
                isRepeat={isRepeat}
                isShuffle={isShuffle}
                onNext={() => {
                  void navigateNext()
                }}
                onPrevious={() => {
                  void navigatePrevious()
                }}
                onResetZoom={handleResetZoom}
                onToggleRepeat={() => {
                  void setRepeat(!isRepeat)
                }}
                onToggleShuffle={() => {
                  void setShuffle(!isShuffle)
                }}
                onZoomIn={handleZoomIn}
                onZoomOut={handleZoomOut}
              />
              <div className="mb-2.5 flex items-center justify-end gap-2 pr-5">
                <Button
                  aria-label="Toggle playlist"
                  onClick={() => {
                    setIsPlaylistOpen(!isPlaylistOpen)
                  }}
                  size="icon"
                  variant="ghost"
                >
                  <IconQueueMusic className="h-5 w-5" />
                </Button>
                <Button
                  aria-label="Close"
                  onClick={() => {
                    void stopPlayback()
                  }}
                  size="icon"
                  variant="ghost"
                >
                  <IconClose className="h-5 w-5" />
                </Button>
              </div>
            </div>
          </div>
        </div>
        {/* Playlist Drawer */}
        <PlaylistDrawer
          onOpenChange={setIsPlaylistOpen}
          open={isPlaylistOpen}
        />
      </>
    )
  }

  // Render audio player
  if (isAudio) {
    return (
      <>
        {/* Player Stats Overlay */}
        <PlayerStats
          enabled={statsEnabled}
          onDisable={() => {
            setStatsEnabled(false)
          }}
          provider={statsProvider}
        />
        <div
          className={cn(
            'contents',
            playback.maximized && isIdle && 'cursor-none',
          )}
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
          <div
            className={cn(
              playback.maximized
                ? 'flex h-full w-full items-center justify-center'
                : 'contents',
            )}
          >
            <AudioPlayer playback={playback} ref={audioPlayerRef} />
          </div>
          <div
            className={cn(
              playback.maximized
                ? `
                  fixed bottom-0 z-50 w-full bg-background/70 transition-opacity
                  duration-300
                `
                : `relative bg-background`,
              `flex h-25 flex-col justify-center border-t border-border`,
              playback.maximized &&
                !showControls &&
                'pointer-events-none opacity-0',
            )}
          >
            {/* Progress Bar (no thumbnails for audio) */}
            <ProgressBar
              bufferedTime={safeBufferedTime}
              currentTime={safeCurrentTime}
              duration={safeDuration}
              onProgressChange={handleProgressChange}
              onProgressMouseMove={handleProgressMouseMove}
              onTooltipVisibilityChange={handleTooltipVisibilityChange}
              progressBarRef={progressBarRef}
              thumbnail={null}
              tooltipPosition={tooltipPosition}
              tooltipRef={tooltipRef}
              tooltipVisible={tooltipVisible}
            />
            <div className="grid h-full grid-cols-[1fr_auto_1fr] items-center">
              <div className="flex items-center">
                {!playback.maximized && (
                  <div className="group relative mb-2.5 ml-2 h-20">
                    <div className="aspect-square h-full" />
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
                <MediaInfo
                  album={playback.originator.parentTitle}
                  artist={playback.originator.originalTitle}
                  currentTime={safeCurrentTime}
                  duration={safeDuration}
                  title={playback.originator.title}
                />
              </div>
              <PlaybackControls
                isPlaying={playback.isPlaying}
                onFastForward10={handleFastForward10}
                onNext={
                  hasNext
                    ? () => {
                        void navigateNext()
                      }
                    : undefined
                }
                onPrevious={
                  hasPrevious
                    ? () => {
                        void navigatePrevious()
                      }
                    : undefined
                }
                onRewind10={handleRewind10}
                onStop={() => {
                  void stopPlayback()
                }}
                onTogglePlayPause={togglePlayPause}
              />
              <div className="mb-2.5 flex items-center justify-end gap-2 pr-5">
                <Button
                  aria-label="Toggle playlist"
                  onClick={() => {
                    setIsPlaylistOpen(!isPlaylistOpen)
                  }}
                  size="icon"
                  variant="ghost"
                >
                  <IconQueueMusic className="h-5 w-5" />
                </Button>
                <PlayerMenu
                  onToggleStats={toggleStats}
                  statsEnabled={statsEnabled}
                />
                <VolumeControls
                  isMuted={playback.isMuted}
                  onToggleMute={toggleMute}
                  onVolumeChange={handleVolumeChange}
                  volume={playback.volume}
                />
              </div>
            </div>
          </div>
        </div>
        {/* Playlist Drawer */}
        <PlaylistDrawer
          onOpenChange={setIsPlaylistOpen}
          open={isPlaylistOpen}
        />
      </>
    )
  }

  // Render video player (default)
  return (
    <>
      {/* Player Stats Overlay */}
      <PlayerStats
        enabled={statsEnabled}
        onDisable={() => {
          setStatsEnabled(false)
        }}
        provider={statsProvider}
      />
      <div
        className={cn(
          'contents',
          playback.maximized && isIdle && 'cursor-none',
        )}
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
            `flex h-25 flex-col justify-center border-t border-border`,
            playback.maximized &&
              !showControls &&
              'pointer-events-none opacity-0',
          )}
        >
          {/* Progress Bar with thumbnails */}
          <ProgressBar
            bufferedTime={safeBufferedTime}
            currentTime={safeCurrentTime}
            duration={safeDuration}
            onProgressChange={handleProgressChange}
            onProgressMouseMove={handleProgressMouseMove}
            onTooltipVisibilityChange={handleTooltipVisibilityChange}
            progressBarRef={progressBarRef}
            thumbnail={thumbnail}
            tooltipPosition={tooltipPosition}
            tooltipRef={tooltipRef}
            tooltipVisible={tooltipVisible}
          />
          <div className="grid h-full grid-cols-[1fr_auto_1fr] items-center">
            <div className="flex items-center">
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
              <MediaInfo
                currentTime={safeCurrentTime}
                duration={safeDuration}
                title={playback.originator.title}
              />
            </div>
            <PlaybackControls
              isPlaying={playback.isPlaying}
              onFastForward10={handleFastForward10}
              onNext={
                hasNext
                  ? () => {
                      void navigateNext()
                    }
                  : undefined
              }
              onPrevious={
                hasPrevious
                  ? () => {
                      void navigatePrevious()
                    }
                  : undefined
              }
              onRewind10={handleRewind10}
              onStop={() => {
                void stopPlayback()
              }}
              onTogglePlayPause={togglePlayPause}
            />
            <div className="mb-2.5 flex items-center justify-end gap-2 pr-5">
              <Button
                aria-label="Toggle playlist"
                onClick={() => {
                  setIsPlaylistOpen(!isPlaylistOpen)
                }}
                size="icon"
                variant="ghost"
              >
                <IconQueueMusic className="h-5 w-5" />
              </Button>
              <PlayerMenu
                onToggleStats={toggleStats}
                statsEnabled={statsEnabled}
              />
              <VolumeControls
                isMuted={playback.isMuted}
                onToggleMute={toggleMute}
                onVolumeChange={handleVolumeChange}
                volume={playback.volume}
              />
            </div>
          </div>
        </div>
        {/* Playlist Drawer */}
        <PlaylistDrawer
          onOpenChange={setIsPlaylistOpen}
          open={isPlaylistOpen}
        />
      </div>
    </>
  )
}

function usePlaybackHeartbeat(
  playback: PlaybackState,
  apolloClient: ReturnType<typeof useApolloClient>,
  updatePlaybackState: (updates: Partial<PlaybackState>) => void,
): void {
  const playbackRef = useRef(playback)
  const intervalRef = useRef<null | ReturnType<typeof setInterval>>(null)

  useEffect(() => {
    playbackRef.current = playback
  }, [playback])

  useEffect(() => {
    if (!playback.playbackSessionId) {
      if (intervalRef.current) {
        clearInterval(intervalRef.current)
        intervalRef.current = null
      }
      return
    }

    const sendHeartbeat = () => {
      const state = playbackRef.current
      if (!state.playbackSessionId) return

      const streamOffset = state.streamOffset ?? 0
      const currentTime = state.currentTime
      const playheadMs = Math.round(currentTime + streamOffset)

      const input = {
        capability: state.capabilityVersionMismatch
          ? buildPlaybackCapabilityInput(state.capabilityProfileVersion)
          : undefined,
        capabilityProfileVersion: state.capabilityProfileVersion ?? undefined,
        playbackSessionId: state.playbackSessionId,
        playheadMs,
        state: state.isPlaying ? 'playing' : 'paused',
      }

      void apolloClient
        .mutate<PlaybackHeartbeatResult>({
          mutation: PlaybackHeartbeatDocument,
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
    }

    sendHeartbeat()

    if (intervalRef.current) {
      clearInterval(intervalRef.current)
    }
    intervalRef.current = setInterval(sendHeartbeat, 15000)

    return () => {
      if (intervalRef.current) {
        clearInterval(intervalRef.current)
        intervalRef.current = null
      }
    }
  }, [apolloClient, playback.playbackSessionId, updatePlaybackState])
}
