import type { Swiper as SwiperInstance, SwiperOptions } from 'swiper/types'

import { useAtomValue } from 'jotai'
import { useRef } from 'react'
import { Swiper, SwiperSlide } from 'swiper/react'
import 'swiper/css'
import MsChevronLeft from '~icons/material-symbols/chevron-left-rounded'
import MsChevronRight from '~icons/material-symbols/chevron-right-rounded'

import type { Item } from '@/shared/api/graphql/graphql'

import { getItemCardWidthPx } from '@/features/content-sources/lib/itemCardSizing'
import { Button } from '@/shared/components/ui/button'
import { cn } from '@/shared/lib/utils'
import { itemCardWidthAtom } from '@/store'

import { ItemCard } from './ItemCard'

type ItemSliderProps = Readonly<{
  breakpoints?: SwiperOptions['breakpoints']
  cardWidthPx?: number
  className?: string
  disableLink?: boolean
  heading?: string
  itemAspect?: 'poster' | 'square' | 'wide'
  items: Pick<
    Item,
    | 'id'
    | 'length'
    | 'librarySectionId'
    | 'metadataType'
    | 'thumbUri'
    | 'title'
    | 'viewOffset'
    | 'year'
  >[]
  slidesPerView?: SwiperOptions['slidesPerView']
  spaceBetween?: number
}>

const defaultSpaceBetween = 16

export function ItemSlider({
  breakpoints,
  cardWidthPx,
  className,
  disableLink,
  heading,
  itemAspect,
  items,
  slidesPerView = 'auto',
  spaceBetween = defaultSpaceBetween,
}: ItemSliderProps) {
  const widthToken = useAtomValue(itemCardWidthAtom)
  const baseCardWidthPx = cardWidthPx ?? getItemCardWidthPx(widthToken)
  const wideWidthMultiplier = 24 / 9
  const resolvedSlideWidth =
    itemAspect === 'wide'
      ? Math.round(baseCardWidthPx * wideWidthMultiplier)
      : baseCardWidthPx
  const swiperRef = useRef<null | SwiperInstance>(null)
  if (items.length === 0) {
    return null
  }

  const hasHeaderContent = Boolean(heading)
  const headingClassName = 'text-lg font-semibold'

  return (
    <div className={cn('w-full min-w-0 space-y-4', className)}>
      {hasHeaderContent && (
        <div className="flex flex-wrap items-center justify-between gap-4">
          <div className="space-y-1">
            {heading && <p className={headingClassName}>{heading}</p>}
          </div>
          <div className="flex items-center gap-2">
            <Button
              aria-label="Previous item"
              className="h-9 w-9 rounded-full"
              onClick={() => swiperRef.current?.slidePrev()}
              size="icon"
              variant="outline"
            >
              <MsChevronLeft className="size-5" />
            </Button>
            <Button
              aria-label="Next item"
              className="h-9 w-9 rounded-full"
              onClick={() => swiperRef.current?.slideNext()}
              size="icon"
              variant="outline"
            >
              <MsChevronRight className="size-5" />
            </Button>
          </div>
        </div>
      )}

      <Swiper
        breakpoints={breakpoints}
        className="w-full overflow-hidden"
        onSwiper={(instance) => {
          swiperRef.current = instance
        }}
        slidesPerView={breakpoints ? undefined : slidesPerView}
        spaceBetween={breakpoints ? undefined : spaceBetween}
      >
        {items.map((item) => (
          <SwiperSlide key={item.id} style={{ width: resolvedSlideWidth }}>
            <div className="pb-4">
              <ItemCard
                aspect={itemAspect}
                cardWidthPx={baseCardWidthPx}
                disableLink={disableLink}
                item={item}
                renderWidthPx={resolvedSlideWidth}
              />
            </div>
          </SwiperSlide>
        ))}
      </Swiper>
    </div>
  )
}
