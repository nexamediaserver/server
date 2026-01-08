import { atom } from 'jotai'
import { atomWithStorage } from 'jotai/utils'

import type { Item } from '@/shared/api/graphql/graphql'

/**
 * Persisted playback session reference used to resume after reloads.
 */
export interface PersistedPlaybackSession {
  currentItemId?: string
  playbackSessionId: string
  playlistGeneratorId?: string
}

/**
 * Extended originator type that includes fields needed for playback.
 * Core fields from Item are required, while additional audio-specific
 * fields (parentTitle, parentThumbUri, originalTitle) may be optionally
 * provided for enhanced display.
 */
export type PlaybackOriginator = Pick<
  Item,
  'id' | 'metadataType' | 'primaryPerson' | 'thumbUri' | 'title'
> & {
  /** Optional artist/original title display */
  originalTitle?: string
  /** Album artwork URL (fallback when track has no thumb) */
  parentThumbUri?: null | string
  /** Optional album title (e.g., parent show/album name) */
  parentTitle?: string
}

export interface PlaybackState {
  bufferedTime: number
  capabilityProfileVersion?: number
  capabilityVersionMismatch?: boolean
  currentTime: number
  duration: number
  isMuted: boolean
  isPlaying: boolean
  maximized: boolean
  mediaId?: string
  mediaTitle?: string
  mediaType?: 'episode' | 'movie' | 'music' | 'photo' | 'unknown'
  originator?: PlaybackOriginator
  playbackSessionId?: string
  playbackUrl?: string
  playlistGeneratorId?: string
  /** Current index within the playlist (0-based). */
  playlistIndex?: number
  /** Whether repeat mode is enabled for the playlist. */
  playlistRepeat?: boolean
  /** Whether shuffle mode is enabled for the playlist. */
  playlistShuffle?: boolean
  /** Total number of items in the playlist. */
  playlistTotalCount?: number
  /**
   * Server-provided duration in milliseconds.
   * This is authoritative and should be preferred over browser-reported duration,
   * which may be incorrect for remuxed or transcoded streams.
   */
  serverDuration?: number
  /**
   * Time offset in milliseconds when the stream is reloaded from a seek position.
   * For remux streams, seeking past the browser-seekable range requires reloading
   * the stream from the new position. The video element's currentTime will be 0,
   * but actual playback position = video.currentTime + streamOffset.
   */
  streamOffset?: number
  streamPlanJson?: string
  trickplayUrl?: null | string
  volume: number
}

/** Main playback state atom */
export const playbackStateAtom = atom<PlaybackState>({
  bufferedTime: 0,
  currentTime: 0,
  duration: 0,
  isMuted: false,
  isPlaying: false,
  maximized: false,
  mediaType: 'unknown',
  playbackUrl: undefined,
  trickplayUrl: undefined,
  volume: 1,
})

/** Persisted playback session identifier for resume-on-load. */
export const persistedPlaybackSessionAtom =
  atomWithStorage<null | PersistedPlaybackSession>(
    'playback:last-session',
    null,
  )

/** Store the last maximized preference to restore it when starting new playback (persisted to localStorage) */
export const lastMaximizedPreferenceAtom = atomWithStorage<boolean>(
  'playback:last-maximized',
  false,
)

/** Derived atom to check if playback is active */
export const isPlaybackActiveAtom = atom((get) => {
  const playback = get(playbackStateAtom)
  return playback.isPlaying && playback.originator?.id !== undefined
})
