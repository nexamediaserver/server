/**
 * @deprecated This module has been moved to `@/domain/utils/images`.
 * Please update your imports:
 *
 * ```tsx
 * // Old (deprecated)
 * import { getImageTranscodeUrl } from '@/shared/lib/images'
 *
 * // New
 * import { getImageTranscodeUrl } from '@/domain/utils'
 * ```
 *
 * This re-export is kept for backward compatibility but will be removed
 * in a future version.
 */

/* eslint-disable import-x/no-restricted-paths -- Deprecated re-export for backward compatibility */

export {
  getDetectedImageFormat,
  getImageTranscodeUrl,
  getImageTranscodeUrlSync,
  type ImageFormat,
  type ImageTranscodeOptions,
  initImageFormatDetection,
} from '@/domain/utils/images'
