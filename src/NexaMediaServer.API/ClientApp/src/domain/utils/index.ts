/**
 * @module @/domain/utils
 *
 * Domain-specific utility functions.
 *
 * This module consolidates utilities that are used across multiple features
 * for working with media-server domain concepts.
 *
 * @example
 * ```tsx
 * // Image transcoding
 * import { getImageTranscodeUrl, initImageFormatDetection } from '@/domain/utils'
 *
 * // Formatting
 * import { formatDuration, formatBytes } from '@/domain/utils'
 * ```
 */

export {
  formatBitrate,
  formatBytes,
  formatDuration,
  type FormatDurationOptions,
  formatNumber,
  formatPercent,
} from './formatters'

export {
  getDetectedImageFormat,
  getImageTranscodeUrl,
  getImageTranscodeUrlSync,
  type ImageFormat,
  type ImageTranscodeOptions,
  initImageFormatDetection,
} from './images'

export {
  clampItemCardWidthToken,
  getItemCardTokenFromPx,
  getItemCardWidthPx,
} from './itemCard'
