import { Outlet } from '@tanstack/react-router'
import { Suspense } from 'react'

import { PageHeader } from '@/app/layout'
import { AppSidebar } from '@/app/layout/AppSidebar'
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
        <div className="flex flex-1 overflow-x-hidden overflow-y-auto">
          <AppSidebar custom={slots.sidebar} footer={slots.footer} />
          <SidebarRail />
          <SidebarInset>
            {slots.header ? <PageHeader custom={slots.header} /> : null}
            <div className="flex-1 overflow-hidden" tabIndex={-1}>
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
