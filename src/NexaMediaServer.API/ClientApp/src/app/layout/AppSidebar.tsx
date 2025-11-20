import type { ReactNode } from 'react'

import { Link, useRouterState } from '@tanstack/react-router'
import IconHome from '~icons/material-symbols/home'

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

import { ContentSourcesSection } from './ContentSourcesSection'
import { NotificationButton } from './NotificationButton'

export function AppSidebar({
  custom,
  footer,
}: {
  custom?: ReactNode
  footer?: ReactNode
}) {
  const routerState = useRouterState()
  const pathname = routerState.location.pathname

  return (
    <Sidebar
      className="relative h-full border-r border-border"
      collapsible="icon"
      variant="sidebar"
    >
      <SidebarHeader className="flex flex-row items-center justify-between">
        <Link className="px-1 pb-1 text-3xl font-bold" to="/">
          ne<span className="font-black text-purple-500">x</span>a
        </Link>
        <div className="mt-1.5">
          <NotificationButton />
        </div>
      </SidebarHeader>
      <SidebarContent>
        {custom ?? (
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
        )}
      </SidebarContent>
      <SidebarFooter className="mt-auto border-t border-border">
        {footer}
      </SidebarFooter>
    </Sidebar>
  )
}
