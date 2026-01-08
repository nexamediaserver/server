/**
 * @module @/domain/utils/images
 *
 * Image utility functions for generating transcode URLs and handling
 * browser format detection.
 *
 * This module provides:
 * - Automatic image format detection (AVIF > WebP > JPEG)
 * - Transcode URL generation for the media server's image API
 * - Both async and sync URL generation methods
 *
 * @example Basic usage
 * ```tsx
 * import { getImageTranscodeUrl, initImageFormatDetection } from '@/domain/utils/images'
 *
 * // Initialize format detection early in app lifecycle
 * await initImageFormatDetection()
 *
 * // Generate transcode URL
 * const url = await getImageTranscodeUrl('metadata://posters/abc123', {
 *   width: 256,
 *   height: 384,
 * })
 * ```
 *
 * @example Synchronous usage (after initialization)
 * ```tsx
 * import { getImageTranscodeUrlSync } from '@/domain/utils/images'
 *
 * // Only works after initImageFormatDetection() has completed
 * const url = getImageTranscodeUrlSync('metadata://posters/abc123', {
 *   width: 256,
 *   height: 384,
 * })
 * ```
 */

import { API_ROUTES, IMAGE_DEFAULTS } from '@/domain/constants'

/** Supported image output formats */
export type ImageFormat = 'avif' | 'jpg' | 'webp'

/**
 * Browser capability cache for image format support.
 * Initialized once on module load to avoid repeated checks.
 */
let detectedFormat: ImageFormat | null = null

/**
 * Options for image transcode URL generation.
 */
export interface ImageTranscodeOptions {
  /**
   * Image format override.
   * If not specified, automatically selects the best format based on
   * browser capabilities (AVIF > WebP > JPEG).
   */
  format?: ImageFormat

  /**
   * Target height in pixels.
   * The server will resize the image to fit within this height
   * while maintaining aspect ratio.
   */
  height: number

  /**
   * Quality for lossy formats (0-100).
   * Higher values = better quality but larger file size.
   * @default 90
   */
  quality?: number

  /**
   * Target width in pixels.
   * The server will resize the image to fit within this width
   * while maintaining aspect ratio.
   */
  width: number
}

/**
 * Gets the currently detected best image format for the browser.
 *
 * Returns null if detection hasn't run yet. Call {@link initImageFormatDetection}
 * or {@link getImageTranscodeUrl} to trigger detection.
 *
 * @returns The detected format or null if not yet detected
 */
export function getDetectedImageFormat(): ImageFormat | null {
  return detectedFormat
}

/**
 * Generates a URL for transcoding an image through the server's image API.
 *
 * Automatically selects the best image format based on browser capabilities
 * (AVIF > WebP > JPEG) unless a specific format is provided.
 *
 * @param uri - The source image URI (e.g., `metadata://posters/abc123`)
 * @param options - Transcode options including dimensions and quality
 * @returns Promise resolving to the transcode URL
 *
 * @example
 * ```tsx
 * const imageUrl = await getImageTranscodeUrl('metadata://posters/abc123', {
 *   width: 256,
 *   height: 384,
 *   quality: 90,
 * })
 * // Returns: /api/v1/images/transcode?uri=...&width=256&height=384&quality=90&format=avif
 * ```
 */
export async function getImageTranscodeUrl(
  uri: string,
  options: ImageTranscodeOptions,
): Promise<string> {
  const format = options.format ?? (await getBestImageFormat())
  const quality = options.quality ?? IMAGE_DEFAULTS.QUALITY

  const url = new URL(API_ROUTES.IMAGES.TRANSCODE, globalThis.location.origin)
  url.searchParams.append('uri', uri)
  url.searchParams.append('width', options.width.toString())
  url.searchParams.append('height', options.height.toString())
  url.searchParams.append('quality', quality.toString())
  url.searchParams.append('format', format)

  return url.toString()
}

/**
 * Synchronous version of {@link getImageTranscodeUrl}.
 *
 * Uses the cached format detection result. Returns null if format detection
 * hasn't completed yet and no explicit format is provided.
 *
 * **Important:** Call {@link initImageFormatDetection} early in your app
 * lifecycle to ensure this function works correctly.
 *
 * @param uri - The source image URI
 * @param options - Transcode options including dimensions and quality
 * @returns The transcode URL, or null if format not yet detected
 *
 * @example
 * ```tsx
 * // In a callback where async isn't available
 * const url = getImageTranscodeUrlSync(thumbUri, { width: 96, height: 96 })
 * if (url) {
 *   img.src = url
 * }
 * ```
 */
export function getImageTranscodeUrlSync(
  uri: string,
  options: ImageTranscodeOptions,
): null | string {
  if (detectedFormat === null && !options.format) {
    return null
  }

  const format = options.format ?? detectedFormat ?? 'jpg'
  const quality = options.quality ?? IMAGE_DEFAULTS.QUALITY

  const url = new URL(API_ROUTES.IMAGES.TRANSCODE, globalThis.location.origin)
  url.searchParams.append('uri', uri)
  url.searchParams.append('width', options.width.toString())
  url.searchParams.append('height', options.height.toString())
  url.searchParams.append('quality', quality.toString())
  url.searchParams.append('format', format)

  return url.toString()
}

/**
 * Pre-initializes the image format detection.
 *
 * Call this early in your app lifecycle (e.g., in a root provider or
 * before rendering) to ensure format detection is complete before
 * components need to generate image URLs.
 *
 * @returns Promise that resolves when detection is complete
 *
 * @example
 * ```tsx
 * // In your app's initialization
 * useEffect(() => {
 *   void initImageFormatDetection()
 * }, [])
 * ```
 */
export async function initImageFormatDetection(): Promise<void> {
  await getBestImageFormat()
}

/**
 * Checks if the browser supports AVIF format.
 * Uses a minimal valid AVIF image for detection.
 */
async function canDecodeAVIF(): Promise<boolean> {
  // Minimal valid AVIF image (1x1 pixel)
  const testImage =
    'data:image/avif;base64,AAAAIGZ0eXBhdmlmAAAAAGF2aWZtaWYxbWlhZk1BMUIAAADybWV0YQAAAAAAAAAoaGRscgAAAAAAAAAAcGljdAAAAAAAAAAAAAAAAGxpYmF2aWYAAAAADnBpdG0AAAAAAAEAAAAeaWxvYwAAAABEAAABAAEAAAABAAABGgAAAB0AAAAoaWluZgAAAAAAAQAAABppbmZlAgAAAAABAABhdjAxQ29sb3IAAAAAamlwcnAAAABLaXBjbwAAABRpc3BlAAAAAAAAAAIAAAACAAAAEHBpeGkAAAAAAwgICAAAAAxhdjFDgQ0MAAAAABNjb2xybmNseAACAAIAAYAAAAAXaXBtYQAAAAAAAAABAAEEAQKDBAAAACVtZGF0EgAKCBgANogQEAwgMg8f8D///8WfhwB8+ErK42A='

  return new Promise((resolve) => {
    const img = new Image()
    img.onload = () => {
      const result = img.width > 0 && img.height > 0
      resolve(result)
    }
    img.onerror = () => {
      resolve(false)
    }
    img.src = testImage
  })
}

/**
 * Checks if the browser supports WebP format.
 * Uses Google's official detection algorithm.
 */
async function canDecodeWebP(): Promise<boolean> {
  // Google's official WebP detection test image (lossy variant)
  const testImage =
    'data:image/webp;base64,UklGRiIAAABXRUJQVlA4IBYAAAAwAQCdASoBAAEADsD+JaQAA3AAAAAA'

  return new Promise((resolve) => {
    const img = new Image()
    img.onload = () => {
      const result = img.width > 0 && img.height > 0
      resolve(result)
    }
    img.onerror = () => {
      resolve(false)
    }
    img.src = testImage
  })
}

/**
 * Detects the best supported image format for the current browser.
 * Checks in order of preference: AVIF > WebP > JPEG
 */
async function detectBestImageFormat(): Promise<ImageFormat> {
  // Check AVIF support (best compression)
  if (await canDecodeAVIF()) {
    return 'avif'
  }

  // Check WebP support (good compression, wide support)
  if (await canDecodeWebP()) {
    return 'webp'
  }

  // Default to JPEG (universally supported)
  return 'jpg'
}

/**
 * Gets or initializes the cached browser image format.
 * Only runs detection once, then caches the result.
 */
async function getBestImageFormat(): Promise<ImageFormat> {
  detectedFormat ??= await detectBestImageFormat()
  return detectedFormat
}
