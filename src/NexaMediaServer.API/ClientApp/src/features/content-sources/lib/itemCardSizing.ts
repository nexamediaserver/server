export const ITEM_CARD_WIDTH_MIN_TOKEN = 32
export const ITEM_CARD_WIDTH_MAX_TOKEN = 52
export const ITEM_CARD_WIDTH_STEP = 4
export const ITEM_CARD_PX_PER_TOKEN = 4
export const ITEM_CARD_MAX_WIDTH_PX =
  ITEM_CARD_WIDTH_MAX_TOKEN * ITEM_CARD_PX_PER_TOKEN

export const ITEM_CARD_WIDTH_MARKS = Array.from(
  {
    length:
      (ITEM_CARD_WIDTH_MAX_TOKEN - ITEM_CARD_WIDTH_MIN_TOKEN) /
        ITEM_CARD_WIDTH_STEP +
      1,
  },
  (_, index) => ITEM_CARD_WIDTH_MIN_TOKEN + index * ITEM_CARD_WIDTH_STEP,
)

export interface ItemCardWidthOptions {
  maxToken?: number
  maxWidthPx?: number
}

export function clampItemCardWidthToken(value: number) {
  return Math.min(
    ITEM_CARD_WIDTH_MAX_TOKEN,
    Math.max(ITEM_CARD_WIDTH_MIN_TOKEN, Math.round(value)),
  )
}

export function getItemCardWidthPx(
  widthToken: number,
  options: ItemCardWidthOptions = {},
) {
  const {
    maxToken = ITEM_CARD_WIDTH_MAX_TOKEN,
    maxWidthPx = ITEM_CARD_MAX_WIDTH_PX,
  } = options

  if (maxToken <= 0) {
    return Math.max(0, Math.round(widthToken))
  }

  return Math.max(0, Math.round((maxWidthPx * widthToken) / maxToken))
}
