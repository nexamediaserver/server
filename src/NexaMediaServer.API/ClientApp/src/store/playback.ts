import { atom } from 'jotai'

import type { Item } from '@/shared/api/graphql/graphql'

export interface PlaybackState {
  bufferedTime: number
  currentTime: number
  directPlayUrl?: string
  duration: number
  isMuted: boolean
  isPlaying: boolean
  maximized: boolean
  originator?: Pick<
    Item,
    'id' | 'metadataType' | 'thumbUri' | 'title' | 'trickplayUrl'
  >
  volume: number
}

export const playbackStateAtom = atom<PlaybackState>({
  bufferedTime: 0,
  currentTime: 0,
  duration: 0,
  isMuted: false,
  isPlaying: false,
  maximized: false,
  volume: 1,
})

// Store the last maximized preference to restore it when starting new playback
export const lastMaximizedPreferenceAtom = atom<boolean>(false)

// Derived atom to check if playback is active
export const isPlaybackActiveAtom = atom((get) => {
  const playback = get(playbackStateAtom)
  return playback.isPlaying && playback.originator?.id !== undefined
})
