import { Outlet } from '@tanstack/react-router'
import { Suspense, useEffect, useState } from 'react'

import { PageHeader, PageSubHeader } from '@/app/layout'
import { AppSidebar } from '@/app/layout/AppSidebar'
import { APP_SHELL_SCROLL_REGION_ID } from '@/app/layout/constants'
import { MobileNavBar } from '@/app/layout/MobileNavBar'
import {
  PlayerContainer,
  usePlayback,
  useResumePlaybackOnLoad,
} from '@/features/player'
import { SidebarInset, SidebarRail } from '@/shared/components/ui/sidebar'
import { useLayout } from '@/shared/hooks'

export function AppShell() {
  const { slots } = useLayout()
  const { playback } = usePlayback()
  const isFullscreen = useIsFullscreen()
  useResumePlaybackOnLoad()

  return (
    <div
      className={`
        flex max-h-svh min-h-svh w-svw flex-col bg-background
        pt-[env(safe-area-inset-top)] text-foreground
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
              className={`
                flex-1 overflow-x-hidden overflow-y-auto pb-16
                md:pb-0
              `}
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
      <MobileNavBar hidden={isFullscreen} />
    </div>
  )
}

/**
 * Hook to track document fullscreen state for hiding mobile nav bar.
 */
function useIsFullscreen() {
  const [isFullscreen, setIsFullscreen] = useState(false)

  useEffect(() => {
    const handleFullscreenChange = () => {
      setIsFullscreen(!!document.fullscreenElement)
    }

    document.addEventListener('fullscreenchange', handleFullscreenChange)
    return () => {
      document.removeEventListener('fullscreenchange', handleFullscreenChange)
    }
  }, [])

  return isFullscreen
}
