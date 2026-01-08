/**
 * Hardware acceleration detection result
 */
export interface HardwareAccelerationInfo {
  /** Decoder type if available */
  decoderType?: string
  /** Whether hardware decoding is likely being used */
  hardwareDecoding: 'Likely' | 'No' | 'Unknown' | 'Yes'
}

/**
 * Queries the Media Capabilities API to check hardware decode support for a codec.
 * @param codec - The video codec string (e.g., 'avc1.42E01E', 'hev1.1.6.L93.B0')
 * @param width - Video width
 * @param height - Video height
 * @param framerate - Video framerate
 * @param bitrate - Video bitrate in bits per second
 * @returns Promise resolving to hardware decode support info
 */
export async function checkHardwareDecodeSupport(
  codec: string,
  width: number,
  height: number,
  framerate: number,
  bitrate: number,
): Promise<{ powerEfficient: boolean; smooth: boolean; supported: boolean }> {
  if (!('mediaCapabilities' in navigator)) {
    return { powerEfficient: false, smooth: false, supported: false }
  }

  try {
    const result = await navigator.mediaCapabilities.decodingInfo({
      type: 'media-source',
      video: {
        bitrate,
        contentType: `video/mp4; codecs="${codec}"`,
        framerate,
        height,
        width,
      },
    })

    return {
      powerEfficient: result.powerEfficient,
      smooth: result.smooth,
      supported: result.supported,
    }
  } catch {
    return { powerEfficient: false, smooth: false, supported: false }
  }
}

/**
 * Detects if hardware acceleration is being used for video playback.
 * Uses VideoPlaybackQuality API and heuristics based on decoded frame counts.
 * @param video - The HTML video element
 * @returns Hardware acceleration info
 */
export function detectHardwareAcceleration(
  video: HTMLVideoElement,
): HardwareAccelerationInfo {
  const quality = video.getVideoPlaybackQuality()

  const { droppedVideoFrames, totalVideoFrames } = quality

  // If we have decoded frames but very few dropped, hardware acceleration is likely
  if (totalVideoFrames > 0) {
    const dropRate = droppedVideoFrames / totalVideoFrames

    // Very low drop rate with significant frames suggests hardware decoding
    // Hardware decoders typically have near-zero drop rates under normal conditions
    if (totalVideoFrames > 100 && dropRate < 0.01) {
      return { decoderType: 'Hardware (inferred)', hardwareDecoding: 'Likely' }
    }

    // High drop rate might indicate software decoding struggling
    if (dropRate > 0.1) {
      return { decoderType: 'Software (inferred)', hardwareDecoding: 'No' }
    }
  }

  return { hardwareDecoding: 'Unknown' }
}

/**
 * Formats bitrate in a human-readable format
 * @param bitrate - Bitrate in bits per second
 * @returns Formatted bitrate string (e.g., "5.2 Mbps", "120 Kbps")
 */
export function formatBitrate(bitrate: number): string {
  if (bitrate > 1_000_000) {
    return `${(bitrate / 1_000_000).toFixed(1)} Mbps`
  }
  return `${(bitrate / 1000).toFixed(0)} Kbps`
}

/**
 * Formats a time duration in milliseconds to H:MM:SS or M:SS
 * @param durationMs - Duration in milliseconds
 * @returns Formatted time string
 */
export function formatDuration(durationMs: number): string {
  const totalSeconds = Math.floor(durationMs / 1000)
  const hours = Math.floor(totalSeconds / 3600)
  const minutes = Math.floor((totalSeconds % 3600) / 60)
  const seconds = totalSeconds % 60

  if (hours > 0) {
    return `${String(hours)}:${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`
  }
  return `${String(minutes)}:${seconds.toString().padStart(2, '0')}`
}

/**
 * Formats file size in a human-readable format
 * @param bytes - Size in bytes
 * @returns Formatted size string (e.g., "1.5 GB", "250 MB")
 */
export function formatFileSize(bytes: number): string {
  const units = ['B', 'KB', 'MB', 'GB', 'TB']
  let size = bytes
  let unitIndex = 0

  while (size >= 1024 && unitIndex < units.length - 1) {
    size /= 1024
    unitIndex++
  }

  const decimals = unitIndex === 0 ? 0 : 1
  return `${size.toFixed(decimals)} ${units[unitIndex]}`
}

/**
 * Formats a framerate value
 * @param fps - Framerate in frames per second
 * @returns Formatted framerate string (e.g., "23.976 fps", "60 fps")
 */
export function formatFramerate(fps: number): string {
  return `${fps.toFixed(fps % 1 === 0 ? 0 : 3)} fps`
}

/**
 * Gets a human-readable play method name
 * @param mode - Play mode (DirectPlay=0, DirectStream=1, Transcode=2)
 * @returns Formatted play method string
 */
export function formatPlayMethod(mode?: number | string): string {
  if (mode === 0 || mode === 'DirectPlay') return 'Direct Play'
  if (mode === 1 || mode === 'DirectStream') return 'Direct Stream'
  if (mode === 2 || mode === 'Transcode') return 'Transcode'
  return 'Unknown'
}

/**
 * Formats a resolution
 * @param width - Width in pixels
 * @param height - Height in pixels
 * @returns Formatted resolution string (e.g., "1920×1080")
 */
export function formatResolution(width: number, height: number): string {
  return `${String(width)}×${String(height)}`
}

/**
 * Transcode reason flags matching the server's TranscodeReason enum.
 */
const TRANSCODE_REASONS: Record<number, string> = {
  1: 'Container',
  2: 'Video Codec',
  4: 'Audio Codec',
  8: 'Subtitle Codec',
  16: 'Video Bitrate',
  32: 'Audio Bitrate',
  64: 'Resolution',
  128: 'Video Level',
  256: 'Video Profile',
  512: 'Ref Frames',
  1024: 'Bit Depth',
  2048: 'Audio Channels',
  4096: 'Sample Rate',
}

/**
 * Formats transcode reason flags into a human-readable string.
 * @param reasons - Bitfield of TranscodeReason flags from the server
 * @returns Comma-separated list of transcode reasons, or null if none
 */
export function formatTranscodeReasons(reasons: number): null | string {
  if (reasons === 0) return null

  const activeReasons: string[] = []

  for (const [flag, label] of Object.entries(TRANSCODE_REASONS)) {
    if (reasons & Number(flag)) {
      activeReasons.push(label)
    }
  }

  return activeReasons.length > 0 ? activeReasons.join(', ') : null
}
