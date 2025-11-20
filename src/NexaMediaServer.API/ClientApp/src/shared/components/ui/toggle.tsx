import * as TogglePrimitive from '@radix-ui/react-toggle'
import { type VariantProps } from 'class-variance-authority'
import * as React from 'react'

import { cn } from '@/shared/lib/utils'

import { toggleVariants } from './toggle.variants'

function Toggle({
  className,
  size,
  variant,
  ...props
}: React.ComponentProps<typeof TogglePrimitive.Root> &
  VariantProps<typeof toggleVariants>) {
  return (
    <TogglePrimitive.Root
      className={cn(toggleVariants({ className, size, variant }))}
      data-slot="toggle"
      {...props}
    />
  )
}

export { Toggle }
