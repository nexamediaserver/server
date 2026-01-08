import { Link, useRouterState } from '@tanstack/react-router'
import { useMemo } from 'react'
import IconArrowBack from '~icons/material-symbols/arrow-back'
import IconDns from '~icons/material-symbols/dns'
import IconLabel from '~icons/material-symbols/label-outline'
import IconListAlt from '~icons/material-symbols/list-alt'
import IconMovie from '~icons/material-symbols/movie-outline'
import IconTune from '~icons/material-symbols/tune'
import IconViewAgenda from '~icons/material-symbols/view-agenda'
import IconViewCarousel from '~icons/material-symbols/view-carousel'

import {
  SidebarGroup,
  SidebarGroupLabel,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
} from '@/shared/components/ui/sidebar'
import { useLayoutSlot } from '@/shared/hooks'

export function SettingsSidebar() {
  const routerState = useRouterState()
  const pathname = routerState.location.pathname

  const sidebarContent = useMemo(
    () => (
      <>
        <div className="flex flex-row items-center gap-2 px-3 py-2">
          <Link
            className={`
              flex items-center text-muted-foreground
              hover:text-foreground
            `}
            title="Back to app"
            to="/"
          >
            <IconArrowBack className="size-5" />
          </Link>
          <span className="text-lg font-semibold">Server Settings</span>
        </div>

        {/* Server Configuration */}
        <SidebarGroup>
          <SidebarGroupLabel>Server</SidebarGroupLabel>
          <SidebarMenu>
            <SidebarMenuItem>
              <SidebarMenuButton
                asChild
                isActive={pathname.includes('/settings/general')}
                tooltip="General"
              >
                <Link to="/settings/general">
                  <IconDns />
                  General
                </Link>
              </SidebarMenuButton>
            </SidebarMenuItem>
          </SidebarMenu>
        </SidebarGroup>

        {/* Media & Playback */}
        <SidebarGroup>
          <SidebarGroupLabel>Media</SidebarGroupLabel>
          <SidebarMenu>
            <SidebarMenuItem>
              <SidebarMenuButton
                asChild
                isActive={pathname.includes('/settings/transcoding')}
                tooltip="Transcoding"
              >
                <Link to="/settings/transcoding">
                  <IconTune />
                  Transcoding
                </Link>
              </SidebarMenuButton>
            </SidebarMenuItem>
          </SidebarMenu>
        </SidebarGroup>

        {/* Content Customization */}
        <SidebarGroup>
          <SidebarGroupLabel>Content</SidebarGroupLabel>
          <SidebarMenu>
            <SidebarMenuItem>
              <SidebarMenuButton
                asChild
                isActive={pathname.includes('/settings/fields')}
                tooltip="Custom Fields"
              >
                <Link to="/settings/fields">
                  <IconListAlt />
                  Custom Fields
                </Link>
              </SidebarMenuButton>
            </SidebarMenuItem>
            <SidebarMenuItem>
              <SidebarMenuButton
                asChild
                isActive={pathname.includes('/settings/hubs')}
                tooltip="Hub Configuration"
              >
                <Link to="/settings/hubs">
                  <IconViewCarousel />
                  Hubs
                </Link>
              </SidebarMenuButton>
            </SidebarMenuItem>
            <SidebarMenuItem>
              <SidebarMenuButton
                asChild
                isActive={pathname.includes('/settings/field-layouts')}
                tooltip="Detail Field Layout"
              >
                <Link to="/settings/field-layouts">
                  <IconViewAgenda />
                  Detail Fields
                </Link>
              </SidebarMenuButton>
            </SidebarMenuItem>
            <SidebarMenuItem>
              <SidebarMenuButton
                asChild
                isActive={pathname.includes('/settings/tags')}
                tooltip="Tag Moderation"
              >
                <Link to="/settings/tags">
                  <IconLabel />
                  Tag Moderation
                </Link>
              </SidebarMenuButton>
            </SidebarMenuItem>
            <SidebarMenuItem>
              <SidebarMenuButton
                asChild
                isActive={pathname.includes('/settings/genres')}
                tooltip="Genre Mappings"
              >
                <Link to="/settings/genres">
                  <IconMovie />
                  Genre Mappings
                </Link>
              </SidebarMenuButton>
            </SidebarMenuItem>
          </SidebarMenu>
        </SidebarGroup>
      </>
    ),
    [pathname],
  )

  useLayoutSlot('sidebar', sidebarContent, [pathname])

  return null
}
