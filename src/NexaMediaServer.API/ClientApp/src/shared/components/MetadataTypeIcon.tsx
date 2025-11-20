import type { ComponentProps } from 'react'

import IconCamera from '~icons/material-symbols/android-camera'
import IconBook from '~icons/material-symbols/book-2'
import IconComics from '~icons/material-symbols/comic-bubble'
import IconGroup from '~icons/material-symbols/group'
import IconPictures from '~icons/material-symbols/image'
import IconManga from '~icons/material-symbols/manga'
import IconMagazine from '~icons/material-symbols/menu-book'
import IconMovie from '~icons/material-symbols/movie'
import IconMusic from '~icons/material-symbols/music-note'
import IconMusicVideo from '~icons/material-symbols/music-video'
import IconPerson from '~icons/material-symbols/person'
import IconQuestionMark from '~icons/material-symbols/question-mark'
import IconTelevision from '~icons/material-symbols/tv'
import IconHomeVideos from '~icons/material-symbols/videocam'
import IconGames from '~icons/material-symbols/videogame-asset'
import IconAudiobook from '~icons/material-symbols/voice-chat'

import { type Item, MetadataType } from '@/shared/api/graphql/graphql'
import { cn } from '@/shared/lib/utils'

type IconComponent = (props: ComponentProps<'svg'>) => JSX.Element

type MetadataTypeIconMap = Partial<Record<MetadataType, IconComponent>>

const iconByMetadataType: MetadataTypeIconMap = {
  [MetadataType.AlbumRelease]: IconMusic,
  [MetadataType.AlbumReleaseGroup]: IconMusic,
  [MetadataType.AudioWork]: IconAudiobook,
  [MetadataType.BehindTheScenes]: IconMovie,
  [MetadataType.BookSeries]: IconManga,
  [MetadataType.Collection]: IconComics,
  [MetadataType.DeletedScene]: IconMovie,
  [MetadataType.Edition]: IconMagazine,
  [MetadataType.EditionGroup]: IconMagazine,
  [MetadataType.EditionItem]: IconBook,
  [MetadataType.Episode]: IconTelevision,
  [MetadataType.ExtraOther]: IconMovie,
  [MetadataType.Featurette]: IconMovie,
  [MetadataType.Game]: IconGames,
  [MetadataType.GameFranchise]: IconGames,
  [MetadataType.GameRelease]: IconGames,
  [MetadataType.GameSeries]: IconGames,
  [MetadataType.Group]: IconGroup,
  [MetadataType.Interview]: IconMovie,
  [MetadataType.LiteraryWork]: IconBook,
  [MetadataType.LiteraryWorkPart]: IconBook,
  [MetadataType.Movie]: IconMovie,
  [MetadataType.OptimizedVersion]: IconHomeVideos,
  [MetadataType.Person]: IconPerson,
  [MetadataType.Photo]: IconCamera,
  [MetadataType.PhotoAlbum]: IconCamera,
  [MetadataType.Picture]: IconPictures,
  [MetadataType.PictureSet]: IconPictures,
  [MetadataType.Playlist]: IconMusic,
  [MetadataType.PlaylistsFolder]: IconMusic,
  [MetadataType.Recording]: IconAudiobook,
  [MetadataType.Scene]: IconMovie,
  [MetadataType.Season]: IconTelevision,
  [MetadataType.Short]: IconMovie,
  [MetadataType.Show]: IconTelevision,
  [MetadataType.Track]: IconAudiobook,
  [MetadataType.Trailer]: IconMusicVideo,
  [MetadataType.UserPlaylistItem]: IconMusic,
}

export type MetadataTypeIconProps = ComponentProps<'svg'> & {
  item: Pick<Item, 'metadataType'>
}

export function MetadataTypeIcon({
  className,
  item,
  ...props
}: MetadataTypeIconProps) {
  const Icon = iconByMetadataType[item.metadataType] ?? IconQuestionMark

  return (
    <Icon aria-hidden="true" className={cn('text-4xl', className)} {...props} />
  )
}
