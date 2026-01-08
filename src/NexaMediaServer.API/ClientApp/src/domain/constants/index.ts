/**
 * @module @/domain/constants
 *
 * Domain-wide constants for the media server client.
 *
 * These constants are:
 * - Used across multiple features
 * - Related to media server domain concepts
 * - Stable and well-defined
 *
 * @example
 * ```tsx
 * import { API_ROUTES, IMAGE_DEFAULTS, ASPECT_RATIOS } from '@/domain/constants'
 *
 * const url = `${API_ROUTES.IMAGES.TRANSCODE}?uri=${encodeURIComponent(uri)}`
 * ```
 */

// =============================================================================
// API Routes
// =============================================================================

/**
 * API endpoint paths.
 *
 * Use these constants instead of hardcoding API paths to ensure consistency
 * and make it easier to update routes if they change.
 */
export const API_ROUTES = {
  /** GraphQL endpoint */
  GRAPHQL: '/graphql',

  /** Image-related endpoints */
  IMAGES: {
    /** Image transcoding endpoint */
    TRANSCODE: '/api/v1/images/transcode',
    /** Trickplay thumbnail base path */
    TRICKPLAY: '/api/v1/images/trickplay',
  },

  /** Management endpoints */
  MANAGE: {
    /** Server info endpoint */
    INFO: '/api/v1/manage/info',
  },

  /** Media playback endpoints */
  MEDIA: {
    /** Direct play base path */
    DIRECT_PLAY: '/api/v1/media',
  },

  /** Playback control endpoints */
  PLAYBACK: {
    /** DASH streaming */
    DASH: '/api/v1/playback/dash',
    /** HLS streaming */
    HLS: '/api/v1/playback/hls',
  },
} as const

// =============================================================================
// Image Constants
// =============================================================================

/**
 * Default values for image transcoding.
 */
export const IMAGE_DEFAULTS = {
  /**
   * Supported image output formats in order of preference.
   * AVIF > WebP > JPEG (fallback)
   */
  FORMATS: ['avif', 'webp', 'jpg'] as const,

  /**
   * Default quality for lossy image formats (0-100).
   * 90 provides a good balance between quality and file size.
   */
  QUALITY: 90,
} as const

/**
 * Standard aspect ratios used throughout the application.
 *
 * These match common media aspect ratios:
 * - Poster: 2:3 (movie posters, book covers)
 * - Square: 1:1 (album art, profile pictures)
 * - Wide/Video: 16:9 (video thumbnails, backdrops)
 */
export const ASPECT_RATIOS = {
  /** 2:3 aspect ratio for posters (width / height = 0.667) */
  POSTER: 2 / 3,
  /** 1:1 aspect ratio for square images */
  SQUARE: 1,
  /** 16:9 aspect ratio for video thumbnails */
  WIDE: 16 / 9,
} as const

/**
 * Tailwind CSS aspect ratio classes.
 *
 * Use these when applying aspect ratios via className.
 */
export const ASPECT_RATIO_CLASSES = {
  POSTER: 'aspect-[2/3]',
  SQUARE: 'aspect-square',
  WIDE: 'aspect-video',
} as const

/**
 * Common image dimension presets.
 *
 * These provide standard sizes for different use cases,
 * organized by aspect ratio type.
 */
export const IMAGE_SIZES = {
  /**
   * Poster image sizes (2:3 aspect ratio).
   * Used for movie posters, show art, book covers.
   */
  POSTER: {
    /** Large: 240x360 - larger cards */
    LG: { height: 360, width: 240 },
    /** Medium: 160x240 - card thumbnails */
    MD: { height: 240, width: 160 },
    /** Small: 120x180 - list items */
    SM: { height: 180, width: 120 },
    /** Extra large: 320x480 - detail views */
    XL: { height: 480, width: 320 },
    /** Extra small: 80x120 - tiny thumbnails */
    XS: { height: 120, width: 80 },
    /** 2X large: 400x600 - high-res displays */
    XXL: { height: 600, width: 400 },
  },

  /**
   * Square image sizes (1:1 aspect ratio).
   * Used for album art, avatars, profile pictures.
   */
  SQUARE: {
    /** Large: 200x200 - larger thumbnails */
    LG: { height: 200, width: 200 },
    /** Medium: 120x120 - standard thumbnails */
    MD: { height: 120, width: 120 },
    /** Small: 80x80 - small thumbnails */
    SM: { height: 80, width: 80 },
    /** Extra large: 300x300 - detail views */
    XL: { height: 300, width: 300 },
    /** Extra small: 48x48 - tiny avatars */
    XS: { height: 48, width: 48 },
    /** 2X large: 400x400 - high-res displays */
    XXL: { height: 400, width: 400 },
  },

  /**
   * Wide/video image sizes (16:9 aspect ratio).
   * Used for video thumbnails, backdrops, episode stills.
   */
  WIDE: {
    /** Large: 640x360 - larger thumbnails */
    LG: { height: 360, width: 640 },
    /** Medium: 480x270 - standard thumbnails */
    MD: { height: 270, width: 480 },
    /** Small: 320x180 - small thumbnails */
    SM: { height: 180, width: 320 },
    /** Extra large: 960x540 - half HD */
    XL: { height: 540, width: 960 },
    /** Extra small: 160x90 - tiny thumbnails */
    XS: { height: 90, width: 160 },
    /** 2X large: 1280x720 - 720p */
    XXL: { height: 720, width: 1280 },
  },
} as const

// =============================================================================
// Item Card Constants
// =============================================================================

/**
 * Item card sizing constants.
 *
 * These control the scalable item card widths used in grid and slider views.
 * The width is controlled by a "token" value that maps to pixels.
 */
export const ITEM_CARD = {
  /** Maximum width in pixels (calculated: 52 * 4 = 208) */
  MAX_WIDTH_PX: 208,
  /** Pixels per token unit */
  PX_PER_TOKEN: 4,
  /** Maximum width token value */
  WIDTH_MAX_TOKEN: 52,
  /** Minimum width token value */
  WIDTH_MIN_TOKEN: 32,
  /** Step size for width adjustments */
  WIDTH_STEP: 4,
} as const

/**
 * Pre-calculated width marks for slider controls.
 * Array of valid width token values from min to max by step.
 */
export const ITEM_CARD_WIDTH_MARKS = Array.from(
  {
    length:
      (ITEM_CARD.WIDTH_MAX_TOKEN - ITEM_CARD.WIDTH_MIN_TOKEN) /
        ITEM_CARD.WIDTH_STEP +
      1,
  },
  (_, index) => ITEM_CARD.WIDTH_MIN_TOKEN + index * ITEM_CARD.WIDTH_STEP,
)

// =============================================================================
// Media Session Constants
// =============================================================================

/**
 * Media session thumbnail size for OS media controls.
 */
export const MEDIA_SESSION = {
  /** Thumbnail dimensions for media session artwork */
  THUMBNAIL: { height: 96, width: 96 },
} as const

// =============================================================================
// Animation Constants
// =============================================================================

/**
 * Animation duration constants in milliseconds.
 */
export const ANIMATION_DURATIONS = {
  /** Image crossfade duration */
  CROSSFADE: 260,
  /** Quick transitions (hover, focus) */
  FAST: 150,
  /** Standard transitions (panels, modals) */
  NORMAL: 250,
  /** Image rotation transition */
  ROTATE: 440,
  /** Slower transitions (page transitions, complex animations) */
  SLOW: 400,
} as const
