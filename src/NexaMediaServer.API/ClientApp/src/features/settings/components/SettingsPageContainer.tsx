import type { ReactNode } from 'react'

import { cn } from '@/shared/lib/utils'

interface SettingsPageContainerProps {
  children: ReactNode
  /**
   * Additional class names to apply to the container
   */
  className?: string
  /**
   * Maximum width variant for the container content.
   * - 'sm': max-w-lg (512px) - for simple forms with few fields
   * - 'md': max-w-2xl (672px) - for medium complexity forms
   * - 'lg': max-w-4xl (896px) - for complex layouts with multiple columns
   * - 'full': max-w-5xl (1024px) - for full-width layouts like tables
   * @default 'md'
   */
  maxWidth?: 'full' | 'lg' | 'md' | 'sm'
}

const maxWidthClasses = {
  full: 'max-w-5xl',
  lg: 'max-w-4xl',
  md: 'max-w-2xl',
  sm: 'max-w-lg',
}

/**
 * Unified container component for settings pages.
 * Provides consistent spacing and max-width constraints.
 */
export function SettingsPageContainer({
  children,
  className,
  maxWidth = 'md',
}: SettingsPageContainerProps) {
  return (
    <div
      className={cn(
        'mx-auto flex w-full flex-col gap-6',
        maxWidthClasses[maxWidth],
        className,
      )}
    >
      {children}
    </div>
  )
}
