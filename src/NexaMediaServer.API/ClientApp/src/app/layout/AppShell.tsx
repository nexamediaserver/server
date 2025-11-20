import { Outlet } from '@tanstack/react-router'
import { Suspense } from 'react'

import { PageHeader, PageSubHeader } from '@/app/layout'
import { AppSidebar } from '@/app/layout/AppSidebar'
import { APP_SHELL_SCROLL_REGION_ID } from '@/app/layout/constants'
import { PlayerContainer, usePlayback } from '@/features/player'
import { SidebarInset, SidebarRail } from '@/shared/components/ui/sidebar'
import { useLayout } from '@/shared/hooks'

export function AppShell() {
  const { slots } = useLayout()
  const { playback } = usePlayback()
  return (
    <div
      className={`
        flex max-h-svh min-h-svh w-svw flex-col bg-background text-foreground
      `}
    >
      {!playback.maximized && (
        <div
          className="flex flex-1 overflow-x-hidden overflow-y-auto"
          data-scroll-restoration-id={APP_SHELL_SCROLL_REGION_ID}
        >
          <AppSidebar custom={slots.sidebar} footer={slots.footer} />
          <SidebarRail />
          <SidebarInset>
            {slots.header ? <PageHeader custom={slots.header} /> : null}
            {slots.subheader ? (
              <PageSubHeader custom={slots.subheader} />
            ) : null}
            <div
              className="flex-1 overflow-x-hidden overflow-y-auto"
              tabIndex={-1}
            >
              <Suspense>
                <Outlet />
              </Suspense>
            </div>
          </SidebarInset>
        </div>
      )}
      <PlayerContainer />
    </div>
  )
}
