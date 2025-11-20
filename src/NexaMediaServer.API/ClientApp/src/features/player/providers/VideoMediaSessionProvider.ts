import { MetadataType } from '@/shared/api/graphql/graphql'
import { getImageTranscodeUrlSync } from '@/shared/lib/images'

import type {
  MediaSessionActionHandlers,
  MediaSessionConfig,
  MediaSessionProvider,
} from '../hooks/useMediaSession'
import type { PlaybackState } from '../store'

/**
 * Video player implementation of MediaSessionProvider
 * Handles Media Session API for video playback (movies, TV shows, etc.)
 */
export class VideoMediaSessionProvider implements MediaSessionProvider {
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
      nexttrack: undefined, // Could be implemented for next episode/movie
      pause: () => {
        this.onPause()
      },
      play: () => {
        this.onPlay()
      },
      previoustrack: undefined, // Could be implemented for previous episode
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

    if (!originator?.title || !originator.thumbUri) {
      return {
        title: 'Unknown',
      }
    }

    const src = getImageTranscodeUrlSync(originator.thumbUri, {
      height: 512,
      width: 512,
    })

    if (!src) {
      throw new Error('Failed to get image transcode URL')
    }

    // Build artwork array from thumbnail
    const artwork: MediaImage[] = originator.thumbUri
      ? [
          {
            sizes: '512x512',
            src,
            type: 'image/jpeg',
          },
        ]
      : []

    // For episodes, we might want to include show information
    if (originator.metadataType === MetadataType.Episode) {
      // You could extend the originator type to include show name
      // For now, just use the episode title
      return {
        artist: 'TV Show', // Could be show name
        artwork,
        title: originator.title,
      }
    }

    // For movies
    return {
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
