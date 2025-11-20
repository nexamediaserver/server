import { atom } from 'jotai'

import type { Item } from '@/shared/api/graphql/graphql'

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
  mediaType?: 'episode' | 'movie' | 'music' | 'unknown'
  originator?: Pick<Item, 'id' | 'metadataType' | 'thumbUri' | 'title'>
  playbackSessionId?: string
  playbackUrl?: string
  playlistGeneratorId?: string
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

/** Store the last maximized preference to restore it when starting new playback */
export const lastMaximizedPreferenceAtom = atom<boolean>(false)

/** Derived atom to check if playback is active */
export const isPlaybackActiveAtom = atom((get) => {
  const playback = get(playbackStateAtom)
  return playback.isPlaying && playback.originator?.id !== undefined
})
