/**
 * @module @/domain/utils/formatters
 *
 * Common formatting utilities for displaying media-related values.
 *
 * These utilities consolidate duplicate formatting logic that was
 * previously scattered across multiple feature components.
 *
 * @example
 * ```tsx
 * import { formatDuration, formatBitrate } from '@/domain/utils/formatters'
 *
 * // Format playback time
 * <span>{formatDuration(currentTime)}</span>
 *
 * // Format with hours
 * <span>{formatDuration(movie.duration, { alwaysShowHours: true })}</span>
 * ```
 */

/**
 * Options for duration formatting.
 */
export interface FormatDurationOptions {
  /**
   * Always include hours in the output, even if duration is less than 1 hour.
   * @default false - hours are only shown when duration >= 1 hour
   */
  alwaysShowHours?: boolean
}

/**
 * Formats a bitrate value to a human-readable string with appropriate units.
 *
 * Automatically selects the best unit (bps, Kbps, Mbps, Gbps) based on
 * the magnitude of the value.
 *
 * @param bitsPerSecond - Bitrate in bits per second
 * @param decimals - Number of decimal places (default: 1)
 * @returns Formatted bitrate string with unit
 *
 * @example
 * ```tsx
 * formatBitrate(1500)      // "1.5 Kbps"
 * formatBitrate(5000000)   // "5.0 Mbps"
 * formatBitrate(800)       // "800 bps"
 * ```
 */
export function formatBitrate(bitsPerSecond: number, decimals = 1): string {
  if (bitsPerSecond === 0) return '0 bps'

  const units = ['bps', 'Kbps', 'Mbps', 'Gbps', 'Tbps']
  const k = 1000
  const i = Math.floor(Math.log(Math.abs(bitsPerSecond)) / Math.log(k))
  const unitIndex = Math.min(i, units.length - 1)

  const value = bitsPerSecond / Math.pow(k, unitIndex)
  return `${value.toFixed(decimals)} ${units[unitIndex]}`
}

/**
 * Formats a byte size to a human-readable string with appropriate units.
 *
 * Uses binary units (1024-based) by default, matching how most systems
 * report file sizes.
 *
 * @param bytes - Size in bytes
 * @param decimals - Number of decimal places (default: 1)
 * @returns Formatted size string with unit
 *
 * @example
 * ```tsx
 * formatBytes(1024)        // "1.0 KB"
 * formatBytes(1536000)     // "1.5 MB"
 * formatBytes(1073741824)  // "1.0 GB"
 * ```
 */
export function formatBytes(bytes: number, decimals = 1): string {
  if (bytes === 0) return '0 B'

  const units = ['B', 'KB', 'MB', 'GB', 'TB', 'PB']
  const k = 1024
  const i = Math.floor(Math.log(Math.abs(bytes)) / Math.log(k))
  const unitIndex = Math.min(i, units.length - 1)

  const value = bytes / Math.pow(k, unitIndex)
  return `${value.toFixed(decimals)} ${units[unitIndex]}`
}

/**
 * Formats a duration in milliseconds to a human-readable string.
 *
 * Output format depends on duration length:
 * - Under 1 hour: `MM:SS` (e.g., "45:30")
 * - 1 hour or more: `H:MM:SS` (e.g., "2:15:30")
 *
 * Use `alwaysShowHours: true` to force `H:MM:SS` format regardless of duration.
 *
 * @param durationMs - Duration in milliseconds
 * @param options - Formatting options
 * @returns Formatted duration string
 *
 * @example
 * ```tsx
 * formatDuration(90000)                    // "1:30"
 * formatDuration(3661000)                  // "1:01:01"
 * formatDuration(90000, { alwaysShowHours: true }) // "0:01:30"
 * ```
 */
export function formatDuration(
  durationMs: number,
  options: FormatDurationOptions = {},
): string {
  const { alwaysShowHours = false } = options

  const totalSeconds = Math.floor(durationMs / 1000)
  const hours = Math.floor(totalSeconds / 3600)
  const minutes = Math.floor((totalSeconds % 3600) / 60)
  const seconds = totalSeconds % 60

  const paddedMinutes =
    hours > 0 || alwaysShowHours
      ? minutes.toString().padStart(2, '0')
      : minutes.toString()
  const paddedSeconds = seconds.toString().padStart(2, '0')

  if (hours > 0 || alwaysShowHours) {
    return `${hours.toString()}:${paddedMinutes}:${paddedSeconds}`
  }

  return `${paddedMinutes}:${paddedSeconds}`
}

/**
 * Formats a number with thousands separators.
 *
 * Uses the user's locale for proper formatting.
 *
 * @param value - Number to format
 * @returns Formatted number string
 *
 * @example
 * ```tsx
 * formatNumber(1234567)  // "1,234,567" (en-US locale)
 * ```
 */
export function formatNumber(value: number): string {
  return new Intl.NumberFormat().format(value)
}

/**
 * Formats a percentage value.
 *
 * @param value - Value between 0 and 1 (or 0 and 100 if already percentage)
 * @param decimals - Number of decimal places (default: 0)
 * @returns Formatted percentage string
 *
 * @example
 * ```tsx
 * formatPercent(0.75)      // "75%"
 * formatPercent(0.333, 1)  // "33.3%"
 * ```
 */
export function formatPercent(value: number, decimals = 0): string {
  // Handle both 0-1 and 0-100 ranges
  const percentage = value > 1 ? value : value * 100
  return `${percentage.toFixed(decimals)}%`
}
