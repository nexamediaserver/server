import type { Swiper as SwiperInstance, SwiperOptions } from 'swiper/types'

import { useAtomValue } from 'jotai'
import { useRef } from 'react'
import { Swiper, SwiperSlide } from 'swiper/react'
import 'swiper/css'
import MsChevronLeft from '~icons/material-symbols/chevron-left-rounded'
import MsChevronRight from '~icons/material-symbols/chevron-right-rounded'

import type { Role } from '@/shared/api/graphql/graphql'

import { getItemCardWidthPx } from '@/features/content-sources/lib/itemCardSizing'
import { Button } from '@/shared/components/ui/button'
import { cn } from '@/shared/lib/utils'
import { itemCardWidthAtom } from '@/store'

import { RoleCard } from './RoleCard'

type RoleSwiperProps = Readonly<{
  breakpoints?: SwiperOptions['breakpoints']
  cardWidthPx?: number
  className?: string
  heading?: string
  librarySectionId: string
  roles: Pick<Role, 'name' | 'personId' | 'relationship' | 'thumbUrl'>[]
  slidesPerView?: SwiperOptions['slidesPerView']
  spaceBetween?: number
}>

const defaultBreakpoints: SwiperOptions['breakpoints'] = {
  540: { slidesPerView: 2.2 },
  768: { slidesPerView: 3.1 },
  1024: { slidesPerView: 4 },
}
const defaultSpaceBetween = 16

export function RoleSlider({
  breakpoints = defaultBreakpoints,
  cardWidthPx,
  className,
  heading,
  librarySectionId,
  roles,
  slidesPerView = 'auto',
  spaceBetween = defaultSpaceBetween,
}: RoleSwiperProps) {
  const swiperRef = useRef<null | SwiperInstance>(null)
  const widthToken = useAtomValue(itemCardWidthAtom)
  const baseCardWidthPx = cardWidthPx ?? getItemCardWidthPx(widthToken)
  const resolvedSlideWidth = baseCardWidthPx

  return (
    <div className={cn('space-y-3', className)}>
      <div className="flex flex-wrap items-center justify-between gap-3 px-1">
        {heading ? (
          <div className="space-y-0.5">
            <p className="text-lg font-semibold text-slate-50">{heading}</p>
          </div>
        ) : (
          <div />
        )}
        <div className="flex items-center gap-2">
          <Button
            aria-label="Previous cast member"
            className="h-9 w-9 rounded-full"
            onClick={() => swiperRef.current?.slidePrev()}
            size="icon"
            variant="outline"
          >
            <MsChevronLeft className="size-5" />
          </Button>
          <Button
            aria-label="Next cast member"
            className="h-9 w-9 rounded-full"
            onClick={() => swiperRef.current?.slideNext()}
            size="icon"
            variant="outline"
          >
            <MsChevronRight className="size-5" />
          </Button>
        </div>
      </div>

      <Swiper
        breakpoints={breakpoints}
        className="overflow-visible"
        onSwiper={(instance) => {
          swiperRef.current = instance
        }}
        slidesPerView={breakpoints ? undefined : slidesPerView}
        spaceBetween={breakpoints ? undefined : spaceBetween}
      >
        {roles.map((role) => (
          <SwiperSlide
            key={role.personId}
            style={{ width: resolvedSlideWidth }}
          >
            <div className="pr-1 pb-2">
              <RoleCard
                cardWidthPx={baseCardWidthPx}
                librarySectionId={librarySectionId}
                renderWidthPx={resolvedSlideWidth}
                role={role}
              />
            </div>
          </SwiperSlide>
        ))}
      </Swiper>
    </div>
  )
}
