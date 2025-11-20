import { useMemo } from 'react'

import { Progress } from '@/shared/components/ui/progress'

export interface ItemProgressProps {
  className?: string
  length?: null | number
  viewOffset?: null | number
}

export function ItemProgress({
  className,
  length,
  viewOffset,
}: Readonly<ItemProgressProps>) {
  const value = useMemo(() => {
    const duration = length ?? 0
    const offset = viewOffset ?? 0

    if (duration <= 0 || offset <= 0) {
      return 0
    }

    return Math.min(100, (offset / duration) * 100)
  }, [length, viewOffset])

  if (value <= 0) {
    return null
  }

  return <Progress className={className} value={value} />
}
