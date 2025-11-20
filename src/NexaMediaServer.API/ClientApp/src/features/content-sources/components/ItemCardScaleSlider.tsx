import { useAtom } from 'jotai'
import { useMemo } from 'react'

import {
  clampItemCardWidthToken,
  ITEM_CARD_WIDTH_MARKS,
  ITEM_CARD_WIDTH_MAX_TOKEN,
  ITEM_CARD_WIDTH_MIN_TOKEN,
  ITEM_CARD_WIDTH_STEP,
} from '@/features/content-sources/lib/itemCardSizing'
import { Slider } from '@/shared/components/ui/slider'
import { cn } from '@/shared/lib/utils'
import { itemCardWidthAtom } from '@/store'

/**
 * ItemCardScaleSlider
 *
 * A compact control that globally adjusts ItemCard width using Tailwind v4 spacing tokens.
 * - Range: w-32 to w-52
 * - Step: matches Tailwind spacing steps in this range (4)
 * - Uses Jotai for global state so all ItemCards react instantly
 */
export function ItemCardScaleSlider({ className }: { className?: string }) {
  const [widthToken, setWidthToken] = useAtom(itemCardWidthAtom)

  const valueArray = useMemo(() => [widthToken], [widthToken])

  return (
    <Slider
      className={cn('w-16', className)}
      max={ITEM_CARD_WIDTH_MAX_TOKEN}
      min={ITEM_CARD_WIDTH_MIN_TOKEN}
      onValueChange={(vals) => {
        const v = clampItemCardWidthToken(vals[0] ?? widthToken)
        // Snap to nearest mark in case of any drift
        const snapped = ITEM_CARD_WIDTH_MARKS.reduce((prev, cur) =>
          Math.abs(cur - v) < Math.abs(prev - v) ? cur : prev,
        )
        setWidthToken(snapped)
      }}
      step={ITEM_CARD_WIDTH_STEP}
      value={valueArray}
    />
  )
}
