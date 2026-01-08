import { MetadataType } from '@/shared/api/graphql/graphql'

/**
 * Returns a human-readable label for a metadata type.
 */
export function getMetadataTypeLabel(type: MetadataType): string {
  const labels: Partial<Record<MetadataType, string>> = {
    [MetadataType.AlbumRelease]: 'Album',
    [MetadataType.AlbumReleaseGroup]: 'Album',
    [MetadataType.Episode]: 'Episode',
    [MetadataType.Movie]: 'Movie',
    [MetadataType.Person]: 'Person',
    [MetadataType.Season]: 'Season',
    [MetadataType.Show]: 'TV Show',
    [MetadataType.Track]: 'Track',
  }
  return labels[type] ?? 'Item'
}

/**
 * Returns true if the metadata type should use poster (2:3) aspect ratio,
 * false for square (1:1) thumbnails.
 */
export function isPosterAspect(type: MetadataType): boolean {
  return [
    MetadataType.Episode,
    MetadataType.Movie,
    MetadataType.Season,
    MetadataType.Show,
  ].includes(type)
}

/** Minimum characters required before search executes */
export const MIN_SEARCH_QUERY_LENGTH = 2
