import type { ReactNode } from 'react'
import type shaka from 'shaka-player'

import { forwardRef, useImperativeHandle, useRef } from 'react'
import IconMusicNote from '~icons/material-symbols/music-note'

import { Image } from '@/shared/components/Image'
import { cn } from '@/shared/lib/utils'

import type { PlaybackOriginator, PlaybackState } from '../store'

import { ShakaPlayer, type ShakaPlayerHandle } from './ShakaPlayer'

export interface AudioPlayerHandle {
  getMediaElement: () => HTMLAudioElement | HTMLVideoElement | null
  getPlayer: () => null | shaka.Player
  seek: (time: number) => void
}

interface AudioPlayerProps {
  /** Current playback state */
  playback: PlaybackState
}

/**
 * Resolves the best available thumbnail URI for audio playback.
 * Attempts to use track thumb, then falls back to album/parent thumb.
 */
function resolveAudioThumbUri(
  originator?: PlaybackOriginator,
): string | undefined {
  if (!originator) return undefined
  return originator.thumbUri ?? originator.parentThumbUri ?? undefined
}

/**
 * Audio player component with album artwork display.
 * - Minimized: Square album thumb with track title
 * - Maximized: Large centered album art with track info below
 */
export const AudioPlayer = forwardRef<AudioPlayerHandle, AudioPlayerProps>(
  ({ playback }, ref) => {
    const shakaRef = useRef<ShakaPlayerHandle>(null)

    // Forward the ref to the underlying ShakaPlayer
    useImperativeHandle(
      ref,
      () => ({
        getMediaElement: () => shakaRef.current?.getMediaElement() ?? null,
        getPlayer: () => shakaRef.current?.getPlayer() ?? null,
        seek: (time: number) => shakaRef.current?.seek(time),
      }),
      [],
    )

    const thumbUri = resolveAudioThumbUri(playback.originator)
    const title = playback.originator?.title ?? 'Unknown Track'
    const artist = playback.originator?.primaryPerson?.title
    const album = playback.originator?.parentTitle

    return (
      <>
        {/* Hidden audio player - ShakaPlayer handles all playback */}
        <ShakaPlayer className="hidden" mediaType="audio" ref={shakaRef} />

        {/* Audio visualization area */}
        {playback.maximized ? (
          <MaximizedAudioView
            album={album}
            artist={artist}
            thumbUri={thumbUri}
            title={title}
          />
        ) : (
          <MinimizedAudioView thumbUri={thumbUri} />
        )}
      </>
    )
  },
)

AudioPlayer.displayName = 'AudioPlayer'

interface MaximizedAudioViewProps {
  album?: string
  artist?: string
  thumbUri?: string
  title: string
}

interface MinimizedAudioViewProps {
  thumbUri?: string
}

function MaximizedAudioView({
  album,
  artist,
  thumbUri,
  title,
}: MaximizedAudioViewProps): ReactNode {
  return (
    <div
      className={`flex h-full w-full flex-col items-center justify-center pb-32`}
    >
      {/* Album artwork */}
      <div
        className={`
          relative aspect-square w-full max-w-md overflow-hidden rounded-lg
          shadow-2xl
        `}
      >
        {thumbUri ? (
          <Image
            className="h-full w-full"
            height={400}
            imageUri={thumbUri}
            objectFit="cover"
            width={400}
          />
        ) : (
          <div
            className={`flex h-full w-full items-center justify-center bg-muted`}
          >
            <IconMusicNote className="h-32 w-32 text-muted-foreground" />
          </div>
        )}
      </div>

      {/* Track info */}
      <div className="mt-8 flex flex-col items-center text-center">
        <h2 className="max-w-lg truncate text-2xl font-bold">{title}</h2>
        {(artist || album) && (
          <p className={`mt-2 max-w-lg truncate text-lg text-muted-foreground`}>
            {artist && album ? `${artist} — ${album}` : artist || album}
          </p>
        )}
      </div>
    </div>
  )
}

function MinimizedAudioView({ thumbUri }: MinimizedAudioViewProps): ReactNode {
  return (
    <div
      className={cn(
        'absolute bottom-2 left-2 z-1 aspect-square h-20',
        'overflow-hidden rounded-md shadow-lg',
      )}
    >
      {thumbUri ? (
        <Image
          className="h-full w-full"
          height={80}
          imageUri={thumbUri}
          objectFit="cover"
          width={80}
        />
      ) : (
        <div className="flex h-full w-full items-center justify-center bg-muted">
          <IconMusicNote className="h-8 w-8 text-muted-foreground" />
        </div>
      )}
    </div>
  )
}
