/**
 * @deprecated Import from '@/domain/constants' and '@/domain/utils' instead.
 *
 * This module is kept for backward compatibility.
 *
 * @example Migration:
 * ```tsx
 * // Old imports
 * import { ITEM_CARD_WIDTH_MIN_TOKEN, clampItemCardWidthToken } from '@/features/content-sources/lib/itemCardSizing'
 *
 * // New imports
 * import { ITEM_CARD, ITEM_CARD_WIDTH_MARKS } from '@/domain/constants'
 * import { clampItemCardWidthToken, getItemCardWidthPx } from '@/domain/utils'
 *
 * // Constant mapping:
 * // ITEM_CARD_WIDTH_MIN_TOKEN -> ITEM_CARD.WIDTH_MIN_TOKEN
 * // ITEM_CARD_WIDTH_MAX_TOKEN -> ITEM_CARD.WIDTH_MAX_TOKEN
 * // ITEM_CARD_WIDTH_STEP -> ITEM_CARD.WIDTH_STEP
 * // ITEM_CARD_PX_PER_TOKEN -> ITEM_CARD.PX_PER_TOKEN
 * // ITEM_CARD_MAX_WIDTH_PX -> ITEM_CARD.MAX_WIDTH_PX
 * ```
 */

import { ITEM_CARD, ITEM_CARD_WIDTH_MARKS as MARKS } from '@/domain/constants'
import {
  clampItemCardWidthToken as clamp,
  getItemCardWidthPx as getWidthPx,
} from '@/domain/utils'

// Re-export constants with old names for backward compatibility
/** @deprecated Use `ITEM_CARD.WIDTH_MIN_TOKEN` from '@/domain/constants' */
export const ITEM_CARD_WIDTH_MIN_TOKEN = ITEM_CARD.WIDTH_MIN_TOKEN

/** @deprecated Use `ITEM_CARD.WIDTH_MAX_TOKEN` from '@/domain/constants' */
export const ITEM_CARD_WIDTH_MAX_TOKEN = ITEM_CARD.WIDTH_MAX_TOKEN

/** @deprecated Use `ITEM_CARD.WIDTH_STEP` from '@/domain/constants' */
export const ITEM_CARD_WIDTH_STEP = ITEM_CARD.WIDTH_STEP

/** @deprecated Use `ITEM_CARD.PX_PER_TOKEN` from '@/domain/constants' */
export const ITEM_CARD_PX_PER_TOKEN = ITEM_CARD.PX_PER_TOKEN

/** @deprecated Use `ITEM_CARD.MAX_WIDTH_PX` from '@/domain/constants' */
export const ITEM_CARD_MAX_WIDTH_PX = ITEM_CARD.MAX_WIDTH_PX

/** @deprecated Use `ITEM_CARD_WIDTH_MARKS` from '@/domain/constants' */
export const ITEM_CARD_WIDTH_MARKS = MARKS

/** @deprecated Not needed with new implementation */
export interface ItemCardWidthOptions {
  maxToken?: number
  maxWidthPx?: number
}

/** @deprecated Use `clampItemCardWidthToken` from '@/domain/utils' */
export function clampItemCardWidthToken(value: number) {
  return clamp(value)
}

/**
 * @deprecated Use `getItemCardWidthPx` from '@/domain/utils'
 *
 * Note: The new implementation doesn't support options parameter.
 * If you need custom scaling, calculate it directly.
 */
export function getItemCardWidthPx(
  widthToken: number,
  options: ItemCardWidthOptions = {},
) {
  const {
    maxToken = ITEM_CARD.WIDTH_MAX_TOKEN,
    maxWidthPx = ITEM_CARD.MAX_WIDTH_PX,
  } = options

  // If using non-default options, use old calculation
  if (
    maxToken !== ITEM_CARD.WIDTH_MAX_TOKEN ||
    maxWidthPx !== ITEM_CARD.MAX_WIDTH_PX
  ) {
    if (maxToken <= 0) {
      return Math.max(0, Math.round(widthToken))
    }
    return Math.max(0, Math.round((maxWidthPx * widthToken) / maxToken))
  }

  // Use new implementation for standard case
  return getWidthPx(widthToken)
}
