import { useState } from 'react'
import IconMenu from '~icons/material-symbols/menu'
import IconSearch from '~icons/material-symbols/search'

import { NotificationButton } from '@/app/layout/NotificationButton'
import { UserMenuButton } from '@/app/layout/UserMenuButton'
import { MobileSearchSheet } from '@/features/search/components/MobileSearchSheet'
import { Button } from '@/shared/components/ui/button'
import { useSidebar } from '@/shared/hooks'
import { cn } from '@/shared/lib/utils'

/**
 * A fixed bottom navigation bar for mobile screens (< md breakpoint).
 * Provides access to sidebar navigation, search, notifications, and user menu.
 * Hidden when playback is in fullscreen mode.
 */
export function MobileNavBar({ hidden }: { hidden?: boolean }) {
  const { toggleSidebar } = useSidebar()
  const [isSearchOpen, setIsSearchOpen] = useState(false)

  if (hidden) {
    return null
  }

  return (
    <>
      <nav
        className={`
          fixed inset-x-0 bottom-0 z-40 flex h-16 items-center justify-around
          border-t border-border bg-background/95
          pb-[env(safe-area-inset-bottom)] backdrop-blur-lg
          md:hidden
        `}
      >
        {/* Sidebar trigger */}
        <Button
          className="h-11 w-11"
          onClick={toggleSidebar}
          size="icon"
          variant="ghost"
        >
          <IconMenu className="size-6" />
          <span className="sr-only">Open menu</span>
        </Button>

        {/* Search button */}
        <Button
          className={cn(
            'h-11 w-11',
            isSearchOpen && 'bg-accent text-accent-foreground',
          )}
          onClick={() => {
            setIsSearchOpen(true)
          }}
          size="icon"
          variant="ghost"
        >
          <IconSearch className="size-6" />
          <span className="sr-only">Search</span>
        </Button>

        {/* Notifications */}
        <NotificationButton />

        {/* User menu */}
        <UserMenuButton />
      </nav>

      <MobileSearchSheet onOpenChange={setIsSearchOpen} open={isSearchOpen} />
    </>
  )
}
