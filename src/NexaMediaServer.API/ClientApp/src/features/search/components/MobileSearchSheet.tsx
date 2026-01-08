import { Link } from '@tanstack/react-router'
import { useCallback, useEffect, useRef, useState } from 'react'
import IconArrowRight from '~icons/material-symbols/arrow-right-alt'
import IconSearch from '~icons/material-symbols/search'

import { useSearch } from '@/features/search/hooks/useSearch'
import { Button } from '@/shared/components/ui/button'
import { Input } from '@/shared/components/ui/input'
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
} from '@/shared/components/ui/sheet'

import { SearchResultsList } from './SearchResultsList'

export interface MobileSearchSheetProps {
  /** Callback when the sheet is closed */
  onOpenChange: (open: boolean) => void
  /** Whether the sheet is open */
  open: boolean
}

/**
 * A mobile-optimized bottom sheet for search with drag-to-dismiss,
 * native feel styling, and safe area support.
 */
export function MobileSearchSheet({
  onOpenChange,
  open,
}: MobileSearchSheetProps) {
  const [query, setQuery] = useState('')
  const inputRef = useRef<HTMLInputElement>(null)

  const { isLoading, isTyping, results } = useSearch(query, setQuery)

  // Focus input when sheet opens, clear query when sheet closes
  useEffect(() => {
    if (open) {
      // Small delay to allow sheet animation to start
      const timer = setTimeout(() => {
        inputRef.current?.focus()
      }, 100)
      return () => {
        clearTimeout(timer)
      }
    }
    return undefined
  }, [open])

  // Reset query when sheet closes
  const handleOpenChange = useCallback(
    (isOpen: boolean) => {
      if (!isOpen) {
        setQuery('')
      }
      onOpenChange(isOpen)
    },
    [onOpenChange],
  )

  const handleClose = useCallback(() => {
    handleOpenChange(false)
  }, [handleOpenChange])

  const handleNavigate = useCallback(() => {
    handleClose()
  }, [handleClose])

  return (
    <Sheet modal onOpenChange={handleOpenChange} open={open}>
      <SheetContent
        className={`
          flex max-h-[85vh] flex-col rounded-t-2xl
          pb-[max(1rem,env(safe-area-inset-bottom))]
        `}
        side="bottom"
      >
        {/* Drag handle indicator */}
        <div className="flex justify-center pt-3 pb-2">
          <div className="h-1 w-12 rounded-full bg-muted-foreground/30" />
        </div>

        <SheetHeader className="px-4 pb-4">
          <SheetTitle className="sr-only">Search</SheetTitle>
          <div className="relative">
            <IconSearch
              className={`
                absolute top-1/2 left-3 size-5 -translate-y-1/2
                text-muted-foreground
              `}
            />
            <Input
              className="h-12 pl-10 text-base"
              onChange={(e) => {
                setQuery(e.target.value)
              }}
              placeholder="Search movies, shows, music..."
              ref={inputRef}
              value={query}
            />
          </div>
        </SheetHeader>

        <div className="flex-1 overflow-y-auto px-4">
          <SearchResultsList
            footer={
              results.length > 0 ? (
                <div className="mt-4 border-t border-border pt-4">
                  <Button asChild className="w-full" variant="secondary">
                    <Link
                      onClick={handleNavigate}
                      search={{ q: query }}
                      to="/search"
                    >
                      <IconArrowRight className="mr-2" />
                      View All Results
                    </Link>
                  </Button>
                </div>
              ) : undefined
            }
            isLoading={isLoading || isTyping}
            onNavigate={handleNavigate}
            query={query}
            results={results}
          />
        </div>
      </SheetContent>
    </Sheet>
  )
}
