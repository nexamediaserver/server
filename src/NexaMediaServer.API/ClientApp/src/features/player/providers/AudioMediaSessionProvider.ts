import { getImageTranscodeUrlSync } from '@/domain/utils'

import type {
  MediaSessionActionHandlers,
  MediaSessionConfig,
  MediaSessionProvider,
} from '../hooks/useMediaSession'
import type { PlaybackState } from '../store'

/**
 * Audio player implementation of MediaSessionProvider.
 * Handles Media Session API for audio playback (tracks, recordings, etc.)
 * Includes artist and album metadata from the originator.
 */
export class AudioMediaSessionProvider implements MediaSessionProvider {
  private onPause: () => void
  private onPlay: () => void
  private onSeek: (time: number) => void
  private onSeekBackward: () => void
  private onSeekForward: () => void
  private onStop: () => void
  private playbackState: PlaybackState

  constructor(
    playbackState: PlaybackState,
    actions: {
      onPause: () => void
      onPlay: () => void
      onSeek: (time: number) => void
      onSeekBackward: () => void
      onSeekForward: () => void
      onStop: () => void
    },
  ) {
    this.playbackState = playbackState
    this.onPlay = actions.onPlay
    this.onPause = actions.onPause
    this.onStop = actions.onStop
    this.onSeek = actions.onSeek
    this.onSeekForward = actions.onSeekForward
    this.onSeekBackward = actions.onSeekBackward
  }

  getActionHandlers(): MediaSessionActionHandlers {
    return {
      nexttrack: undefined, // Could be implemented for next track in playlist
      pause: () => {
        this.onPause()
      },
      play: () => {
        this.onPlay()
      },
      previoustrack: undefined, // Could be implemented for previous track
      seekbackward: () => {
        this.onSeekBackward()
      },
      seekforward: () => {
        this.onSeekForward()
      },
      seekto: (details: MediaSessionActionDetails) => {
        if (details.seekTime !== undefined) {
          this.onSeek(details.seekTime)
        }
      },
      stop: () => {
        this.onStop()
      },
    }
  }

  getMetadata(): MediaSessionConfig {
    const { originator } = this.playbackState

    if (!originator?.title) {
      return {
        title: 'Unknown',
      }
    }

    const thumbUri = resolveAudioThumbUri(this.playbackState)

    // Build artwork array from thumbnail (track or album)
    let artwork: MediaImage[] = []
    if (thumbUri) {
      const src = getImageTranscodeUrlSync(thumbUri, {
        height: 512,
        width: 512,
      })

      if (src) {
        artwork = [
          {
            sizes: '512x512',
            src,
            type: 'image/jpeg',
          },
        ]
      }
    }

    return {
      album: originator.parentTitle,
      artist: originator.originalTitle,
      artwork,
      title: originator.title,
    }
  }

  updatePositionState(state: PlaybackState): void {
    if (
      'mediaSession' in navigator &&
      'setPositionState' in navigator.mediaSession
    ) {
      navigator.mediaSession.setPositionState({
        duration: state.duration / 1000, // Convert to seconds
        playbackRate: 1.0,
        position: state.currentTime / 1000, // Convert to seconds
      })
    }
  }
}

/**
 * Resolves the best available thumbnail URI for audio playback.
 * Attempts to use track thumb, then falls back to album/parent thumb.
 */
function resolveAudioThumbUri(
  playbackState: PlaybackState,
): string | undefined {
  const originator = playbackState.originator
  if (!originator) return undefined
  return originator.thumbUri ?? originator.parentThumbUri ?? undefined
}
