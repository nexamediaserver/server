/**
 * @module MetadataTypeIcon
 *
 * Renders an appropriate icon based on a media item's metadata type.
 *
 * This component provides visual identification for different types of
 * media content (movies, shows, music, etc.) across the application:
 * - Search results
 * - Item headers
 * - Library section views
 * - Navigation breadcrumbs
 *
 * @example Basic usage
 * ```tsx
 * <MetadataTypeIcon item={item} />
 * ```
 *
 * @example With custom size and styling
 * ```tsx
 * <MetadataTypeIcon
 *   item={item}
 *   className="text-2xl text-muted-foreground"
 * />
 * ```
 */
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

/** Component type for icon elements */
type IconComponent = (props: ComponentProps<'svg'>) => JSX.Element

/** Mapping from metadata type to corresponding icon component */
type MetadataTypeIconMap = Partial<Record<MetadataType, IconComponent>>

/**
 * Maps each MetadataType to its representative icon.
 *
 * The mapping groups related content types to similar icons:
 * - Video content → Movie/TV icons
 * - Music content → Music note icon
 * - Book content → Book/manga icons
 * - Photo content → Camera/picture icons
 * - Game content → Gamepad icon
 * - People → Person/group icons
 */
const iconByMetadataType: MetadataTypeIconMap = {
  // Music
  [MetadataType.AlbumRelease]: IconMusic,
  [MetadataType.AlbumReleaseGroup]: IconMusic,
  // Audio - Books and Recordings
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

  // Games
  [MetadataType.Game]: IconGames,
  [MetadataType.GameFranchise]: IconGames,
  [MetadataType.GameRelease]: IconGames,
  [MetadataType.GameSeries]: IconGames,
  [MetadataType.Group]: IconGroup,

  [MetadataType.Interview]: IconMovie,
  // Books and Literature
  [MetadataType.LiteraryWork]: IconBook,
  [MetadataType.LiteraryWorkPart]: IconBook,

  // Video - Movies and related
  [MetadataType.Movie]: IconMovie,
  [MetadataType.OptimizedVersion]: IconHomeVideos,
  // People
  [MetadataType.Person]: IconPerson,
  // Photos and Pictures
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
  // Video - TV and Series
  [MetadataType.Show]: IconTelevision,
  [MetadataType.Track]: IconAudiobook,

  // Video - Other
  [MetadataType.Trailer]: IconMusicVideo,
  [MetadataType.UserPlaylistItem]: IconMusic,
}

/**
 * Props for the MetadataTypeIcon component.
 */
export type MetadataTypeIconProps = ComponentProps<'svg'> & {
  /**
   * The item containing the metadataType to render an icon for.
   * Only the `metadataType` property is required.
   */
  item: Pick<Item, 'metadataType'>
}

/**
 * Renders an icon representing the given item's metadata type.
 *
 * Uses Material Symbols icons imported via unplugin-icons.
 * Falls back to a question mark icon for unknown metadata types.
 *
 * @param props - Component props including the item and optional SVG props
 * @returns An SVG icon element
 */
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
