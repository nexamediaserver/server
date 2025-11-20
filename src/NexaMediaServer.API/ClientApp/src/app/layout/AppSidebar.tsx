import { Link, useRouterState } from '@tanstack/react-router'
import { type ReactNode, useCallback, useRef, useState } from 'react'
import IconClose from '~icons/material-symbols/close'
import IconHome from '~icons/material-symbols/home'
import IconSearch from '~icons/material-symbols/search'

import { SearchSidebarResults } from '@/features/search'
import { Button } from '@/shared/components/ui/button'
import { Input } from '@/shared/components/ui/input'
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupLabel,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
} from '@/shared/components/ui/sidebar'
import { cn } from '@/shared/lib/utils'

import { ContentSourcesSection } from './ContentSourcesSection'
import { NotificationButton } from './NotificationButton'
import { UserMenuButton } from './UserMenuButton'

export function AppSidebar({
  custom,
  footer,
}: Readonly<{
  custom?: ReactNode
  footer?: ReactNode
}>) {
  const routerState = useRouterState()
  const pathname = routerState.location.pathname
  const [isSearchOpen, setIsSearchOpen] = useState(false)
  const [searchQuery, setSearchQuery] = useState('')
  const inputRef = useRef<HTMLInputElement>(null)

  const handleSearchToggle = useCallback(() => {
    if (isSearchOpen) {
      setIsSearchOpen(false)
      setSearchQuery('')
    } else {
      setIsSearchOpen(true)
      // Focus input after state update
      setTimeout(() => inputRef.current?.focus(), 0)
    }
  }, [isSearchOpen])

  const handleClearSearch = useCallback(() => {
    setIsSearchOpen(false)
    setSearchQuery('')
  }, [])

  const isSearchActive = isSearchOpen || searchQuery.length > 0

  return (
    <Sidebar
      className="relative h-full border-r border-border"
      collapsible="icon"
      variant="sidebar"
    >
      <SidebarHeader>
        <div className="flex flex-row items-center justify-between">
          <Link className="px-1 pb-1 text-3xl font-bold" to="/">
            ne<span className="font-black text-purple-500">x</span>a
          </Link>
          <div className="mt-1.5 flex flex-row items-center">
            <Button
              onClick={handleSearchToggle}
              size="icon"
              variant={isSearchActive ? 'secondary' : 'ghost'}
            >
              {isSearchActive ? <IconClose /> : <IconSearch />}
            </Button>
            <NotificationButton />
            <UserMenuButton />
          </div>
        </div>
        <div
          className={cn(
            'grid transition-all duration-200',
            isSearchActive
              ? 'grid-rows-[1fr] opacity-100'
              : 'grid-rows-[0fr] opacity-0',
          )}
        >
          <div className="overflow-hidden">
            <Input
              onChange={(e) => {
                setSearchQuery(e.target.value)
              }}
              placeholder="Search..."
              ref={inputRef}
              value={searchQuery}
            />
          </div>
        </div>
      </SidebarHeader>
      <SidebarContent>
        {isSearchActive ? (
          <SearchSidebarResults
            onClearSearch={handleClearSearch}
            query={searchQuery}
          />
        ) : (
          (custom ?? (
            <SidebarGroup>
              <SidebarGroupLabel className="justify-between pr-0">
                General
              </SidebarGroupLabel>
              <SidebarMenu>
                <SidebarMenuItem>
                  <SidebarMenuButton
                    asChild
                    isActive={pathname === '/'}
                    tooltip="Home"
                  >
                    <Link to="/">
                      <IconHome />
                      Home
                    </Link>
                  </SidebarMenuButton>
                </SidebarMenuItem>
              </SidebarMenu>

              <ContentSourcesSection />
            </SidebarGroup>
          ))
        )}
      </SidebarContent>
      <SidebarFooter className="mt-auto border-t border-border">
        {footer}
      </SidebarFooter>
    </Sidebar>
  )
}
