import { memo, useCallback, useMemo } from 'react'

import { cn } from '@/shared/lib/utils'

const LETTERS = ['#', ...Array.from('ABCDEFGHIJKLMNOPQRSTUVWXYZ')] as const

export interface JumpBarProps {
  /** Current letter that is active/visible */
  activeLetter?: string
  /** Letter index data from the API */
  letterIndex: LetterIndexEntry[]
  /** Callback when a letter is clicked */
  onLetterSelect: (letter: string, offset: number) => void
}

export interface LetterIndexEntry {
  count: number
  firstItemOffset: number
  letter: string
}

export const JumpBar = memo(function JumpBar({
  activeLetter,
  letterIndex,
  onLetterSelect,
}: JumpBarProps) {
  // Create a map for quick lookup of letter data
  const letterMap = useMemo(
    () => new Map(letterIndex.map((entry) => [entry.letter, entry])),
    [letterIndex],
  )

  const handleLetterClick = useCallback(
    (letter: string) => {
      const entry = letterMap.get(letter)
      if (entry) {
        onLetterSelect(letter, entry.firstItemOffset)
      }
    },
    [letterMap, onLetterSelect],
  )

  return (
    <div
      className={`
        fixed top-1/2 right-2 z-50 flex -translate-y-1/2 flex-col items-center
        gap-0.5
      `}
    >
      {LETTERS.map((letter) => {
        const entry = letterMap.get(letter)
        const hasItems = entry && entry.count > 0
        const isActive = activeLetter === letter

        return (
          <button
            aria-label={`Jump to ${letter === '#' ? 'numbers and symbols' : letter}`}
            className={cn(
              `
                flex h-5 w-5 items-center justify-center rounded text-xs
                font-medium transition-colors
              `,
              hasItems
                ? `
                  cursor-pointer
                  hover:bg-accent hover:text-accent-foreground
                `
                : 'cursor-default text-muted-foreground/40',
              isActive && hasItems && 'bg-primary text-primary-foreground',
            )}
            disabled={!hasItems}
            key={letter}
            onClick={() => {
              handleLetterClick(letter)
            }}
            title={
              hasItems
                ? `${entry.count.toString()} item${entry.count === 1 ? '' : 's'}`
                : 'No items'
            }
            type="button"
          >
            {letter}
          </button>
        )
      })}
    </div>
  )
})
