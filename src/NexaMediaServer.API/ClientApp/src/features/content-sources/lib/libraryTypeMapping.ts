import { LibraryType, MetadataType } from '@/shared/api/graphql/graphql'

/**
 * Maps a library type to the appropriate root metadata type that should be
 * displayed when browsing that library.
 *
 * @param libraryType - The type of library section
 * @returns The metadata type to use for fetching root items
 */
export function getRootMetadataType(libraryType: LibraryType): MetadataType {
  switch (libraryType) {
    case LibraryType.Audiobooks:
      return MetadataType.EditionGroup
    case LibraryType.Books:
      return MetadataType.EditionGroup
    case LibraryType.Comics:
      return MetadataType.BookSeries
    case LibraryType.Games:
      return MetadataType.Game
    case LibraryType.HomeVideos:
      return MetadataType.Movie
    case LibraryType.Magazines:
      return MetadataType.BookSeries
    case LibraryType.Manga:
      return MetadataType.BookSeries
    case LibraryType.Movies:
      return MetadataType.Movie
    case LibraryType.Music:
      return MetadataType.AlbumReleaseGroup
    case LibraryType.MusicVideos:
      return MetadataType.Movie
    case LibraryType.Photos:
      return MetadataType.PhotoAlbum
    case LibraryType.Pictures:
      return MetadataType.PictureSet
    case LibraryType.Podcasts:
      return MetadataType.Show
    case LibraryType.TvShows:
      return MetadataType.Show
    default:
      return MetadataType.Movie
  }
}
