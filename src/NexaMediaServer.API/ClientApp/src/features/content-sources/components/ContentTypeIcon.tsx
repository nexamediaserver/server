import type { ComponentProps } from 'react'

import IconCamera from '~icons/material-symbols/android-camera'
import IconBook from '~icons/material-symbols/book-2'
import IconComics from '~icons/material-symbols/comic-bubble'
import IconPictures from '~icons/material-symbols/image'
import IconManga from '~icons/material-symbols/manga'
import IconMagazine from '~icons/material-symbols/menu-book'
import IconMovie from '~icons/material-symbols/movie'
import IconMusic from '~icons/material-symbols/music-note'
import IconMusicVideo from '~icons/material-symbols/music-video'
import IconPodcasts from '~icons/material-symbols/podcasts'
import IconQuestionMark from '~icons/material-symbols/question-mark'
import IconTelevision from '~icons/material-symbols/tv'
import IconHomeVideos from '~icons/material-symbols/videocam'
import IconGames from '~icons/material-symbols/videogame-asset'
import IconAudiobook from '~icons/material-symbols/voice-chat'

import { LibraryType } from '@/shared/api/graphql/graphql'
import { cn } from '@/shared/lib/utils'

export type ContentTypeIconProps = ComponentProps<'svg'> & {
  contentType: LibraryType
}

export function ContentTypeIcon({
  className,
  contentType,
  ...props
}: ContentTypeIconProps) {
  switch (contentType) {
    case LibraryType.Audiobooks:
      return (
        <IconAudiobook
          aria-hidden="true"
          className={cn('text-4xl', className)}
          {...props}
        />
      )
    case LibraryType.Books:
      return (
        <IconBook
          aria-hidden="true"
          className={cn('text-4xl', className)}
          {...props}
        />
      )
    case LibraryType.Comics:
      return (
        <IconComics
          aria-hidden="true"
          className={cn('text-4xl', className)}
          {...props}
        />
      )
    case LibraryType.Games:
      return (
        <IconGames
          aria-hidden="true"
          className={cn('text-4xl', className)}
          {...props}
        />
      )
    case LibraryType.HomeVideos:
      return (
        <IconHomeVideos
          aria-hidden="true"
          className={cn('text-4xl', className)}
          {...props}
        />
      )
    case LibraryType.Magazines:
      return (
        <IconMagazine
          aria-hidden="true"
          className={cn('text-4xl', className)}
          {...props}
        />
      )
    case LibraryType.Manga:
      return (
        <IconManga
          aria-hidden="true"
          className={cn('text-4xl', className)}
          {...props}
        />
      )
    case LibraryType.Movies:
      return (
        <IconMovie
          aria-hidden="true"
          className={cn('text-4xl', className)}
          {...props}
        />
      )
    case LibraryType.Music:
      return (
        <IconMusic
          aria-hidden="true"
          className={cn('text-4xl', className)}
          {...props}
        />
      )
    case LibraryType.MusicVideos:
      return (
        <IconMusicVideo
          aria-hidden="true"
          className={cn('text-4xl', className)}
          {...props}
        />
      )
    case LibraryType.Photos:
      return (
        <IconCamera
          aria-hidden="true"
          className={cn('text-4xl', className)}
          {...props}
        />
      )
    case LibraryType.Pictures:
      return (
        <IconPictures
          aria-hidden="true"
          className={cn('text-4xl', className)}
          {...props}
        />
      )
    case LibraryType.Podcasts:
      return (
        <IconPodcasts
          aria-hidden="true"
          className={cn('text-4xl', className)}
          {...props}
        />
      )
    case LibraryType.TvShows:
      return (
        <IconTelevision
          aria-hidden="true"
          className={cn('text-4xl', className)}
          {...props}
        />
      )
    default:
      return (
        <IconQuestionMark
          aria-hidden="true"
          className={cn('text-4xl', className)}
          {...props}
        />
      )
  }
}
