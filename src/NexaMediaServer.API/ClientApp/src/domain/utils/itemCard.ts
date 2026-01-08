/**
 * @module @/domain/utils/itemCard
 *
 * Utility functions for item card sizing calculations.
 */

import { ITEM_CARD } from '@/domain/constants'

/**
 * Clamps an item card width token to valid bounds.
 *
 * Ensures the token value stays within the allowed range
 * (32-52 with step increments of 4).
 *
 * @param token - The token value to clamp
 * @returns Clamped token value within valid range
 *
 * @example
 * ```tsx
 * clampItemCardWidthToken(28) // Returns 32 (min)
 * clampItemCardWidthToken(60) // Returns 52 (max)
 * clampItemCardWidthToken(40) // Returns 40 (unchanged)
 * ```
 */
export function clampItemCardWidthToken(token: number): number {
  return Math.min(
    Math.max(token, ITEM_CARD.WIDTH_MIN_TOKEN),
    ITEM_CARD.WIDTH_MAX_TOKEN,
  )
}

/**
 * Converts pixels to the nearest valid item card width token.
 *
 * @param px - Width in pixels
 * @returns Nearest valid token value
 *
 * @example
 * ```tsx
 * getItemCardTokenFromPx(150) // Returns 36 or 40 (nearest step)
 * getItemCardTokenFromPx(200) // Returns 52
 * ```
 */
export function getItemCardTokenFromPx(px: number): number {
  const rawToken = px / ITEM_CARD.PX_PER_TOKEN
  const snappedToken =
    Math.round((rawToken - ITEM_CARD.WIDTH_MIN_TOKEN) / ITEM_CARD.WIDTH_STEP) *
      ITEM_CARD.WIDTH_STEP +
    ITEM_CARD.WIDTH_MIN_TOKEN
  return clampItemCardWidthToken(snappedToken)
}

/**
 * Converts an item card width token to pixels.
 *
 * @param token - The token value (will be clamped to valid range)
 * @returns Width in pixels
 *
 * @example
 * ```tsx
 * getItemCardWidthPx(32) // Returns 128
 * getItemCardWidthPx(40) // Returns 160
 * getItemCardWidthPx(52) // Returns 208
 * ```
 */
export function getItemCardWidthPx(token: number): number {
  return clampItemCardWidthToken(token) * ITEM_CARD.PX_PER_TOKEN
}
