import type { ReactNode } from 'react'

export function PageSubHeader({ custom }: { custom?: ReactNode }) {
  return (
    <header
      className={`
        sticky top-0 z-30 flex h-16 items-center justify-between gap-3 border-b
        border-border px-3
        md:px-4
        dark:bg-stone-900
      `}
    >
      {custom}
    </header>
  )
}
