/**
 * Image utility module for handling image transcoding URLs with browser capability detection.
 * Detects browser support for AVIF, WebP, and falls back to JPEG.
 */

type ImageFormat = 'avif' | 'jpg' | 'webp'

/**
 * Browser capability cache for image format support.
 * Initialized once on module load to avoid repeated checks.
 */
let detectedFormat: ImageFormat | null = null

/**
 * Options for image transcode URL generation
 */
export interface ImageTranscodeOptions {
  /** Image format override (defaults to auto-detected best format) */
  format?: ImageFormat
  /** Height in pixels */
  height: number
  /** Quality for lossy formats (0-100), defaults to 90 */
  quality?: number
  /** Width in pixels */
  width: number
}

/**
 * Gets the currently detected best image format.
 * Returns null if detection hasn't run yet.
 *
 * @returns The detected format or null
 */
export function getDetectedImageFormat(): ImageFormat | null {
  return detectedFormat
}

/**
 * Generates a URL for transcoding an image through the server's image API.
 * Automatically selects the best image format based on browser capabilities
 * (AVIF > WebP > JPEG) unless a specific format is provided.
 *
 * @param uri - The source image URI (e.g., metadata://...)
 * @param options - Transcode options including width, height, quality, and optional format
 * @returns Promise that resolves to the transcode URL
 *
 * @example
 * ```tsx
 * const imageUrl = await getImageTranscodeUrl('metadata://posters/abc123', {
 *   width: 256,
 *   height: 384,
 *   quality: 90
 * })
 * ```
 */
export async function getImageTranscodeUrl(
  uri: string,
  options: ImageTranscodeOptions,
): Promise<string> {
  const format = options.format ?? (await getBestImageFormat())
  const quality = options.quality ?? 90

  const url = new URL('/api/v1/images/transcode', window.location.origin)
  url.searchParams.append('uri', uri)
  url.searchParams.append('width', options.width.toString())
  url.searchParams.append('height', options.height.toString())
  url.searchParams.append('quality', quality.toString())
  url.searchParams.append('format', format)

  return url.toString()
}

/**
 * Synchronous version that uses cached format detection.
 * Returns null if format detection hasn't completed yet.
 * Use this only if you've already called getImageTranscodeUrl() or getBestImageFormat() earlier.
 *
 * @param uri - The source image URI
 * @param options - Transcode options
 * @returns The transcode URL or null if format not yet detected
 */
export function getImageTranscodeUrlSync(
  uri: string,
  options: ImageTranscodeOptions,
): null | string {
  if (detectedFormat === null && !options.format) {
    return null
  }

  const format = options.format ?? detectedFormat ?? 'jpg'
  const quality = options.quality ?? 90

  const url = new URL('/api/v1/images/transcode', window.location.origin)
  url.searchParams.append('uri', uri)
  url.searchParams.append('width', options.width.toString())
  url.searchParams.append('height', options.height.toString())
  url.searchParams.append('quality', quality.toString())
  url.searchParams.append('format', format)

  return url.toString()
}

/**
 * Pre-initializes the image format detection.
 * Call this early in your app lifecycle to ensure format detection is complete
 * before components need to generate image URLs.
 *
 * @returns Promise that resolves when detection is complete
 */
export async function initImageFormatDetection(): Promise<void> {
  await getBestImageFormat()
}

/**
 * Checks if the browser supports AVIF format.
 * Uses a minimal valid AVIF image for detection.
 *
 * @returns Promise that resolves to true if AVIF is supported
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
 * Checks if the browser supports WebP format using Google's official detection algorithm.
 * Tests for lossy WebP support which is the most commonly used variant.
 *
 * @returns Promise that resolves to true if WebP is supported
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
 *
 * @returns Promise that resolves to the best supported format
 */
async function detectBestImageFormat(): Promise<ImageFormat> {
  // Check AVIF support
  if (await canDecodeAVIF()) {
    return 'avif'
  }

  // Check WebP support
  if (await canDecodeWebP()) {
    return 'webp'
  }

  // Default to JPEG (universally supported)
  return 'jpg'
}

/**
 * Gets or initializes the cached browser image format.
 * Only runs detection once, then caches the result.
 *
 * @returns Promise that resolves to the best supported format
 */
async function getBestImageFormat(): Promise<ImageFormat> {
  detectedFormat ??= await detectBestImageFormat()
  return detectedFormat
}
