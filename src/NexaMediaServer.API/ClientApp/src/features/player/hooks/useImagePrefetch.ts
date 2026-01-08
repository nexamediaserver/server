import { useAtomValue } from 'jotai'
import { useEffect, useMemo } from 'react'

import { playbackStateAtom } from '../store'
import { usePlaylist } from './usePlaylist'

interface UseImagePrefetchOptions {
  /** Number of items to preload on each side of the current image. */
  windowSize?: number
}

/**
 * Preload nearby images in the playlist to make next/previous navigation instant.
 */
export function useImagePrefetch(options: UseImagePrefetchOptions = {}): void {
  const { windowSize = 3 } = options
  const playback = useAtomValue(playbackStateAtom)

  // Only prefetch when an image playlist is active
  const isImage = playback.mediaType === 'photo'
  const chunkSize = Math.max(1, windowSize * 2 + 1)
  const { chunk } = usePlaylist({ chunkSize })

  const targetIndices = useMemo(() => {
    if (!isImage) return []

    const center = playback.playlistIndex ?? 0
    const indices: number[] = []

    for (let offset = -windowSize; offset <= windowSize; offset += 1) {
      indices.push(center + offset)
    }

    return indices
  }, [isImage, playback.playlistIndex, windowSize])

  useEffect(() => {
    if (!isImage || !chunk) return

    const preloaded: HTMLImageElement[] = []

    for (const index of targetIndices) {
      const item = chunk.items.find((i) => i?.index === index)
      const url = item?.playbackUrl

      if (!url) continue

      const img = new Image()
      img.decoding = 'async'
      img.loading = 'eager'
      img.src = url
      preloaded.push(img)
    }

    return () => {
      preloaded.forEach((img) => {
        // Drop references to allow GC
        img.src = ''
      })
    }
  }, [chunk, isImage, targetIndices])
}
