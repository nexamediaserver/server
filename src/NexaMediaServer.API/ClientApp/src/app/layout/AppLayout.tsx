import type { ReactNode } from 'react'

import { AppShell } from '@/app/layout'
import {
  JobNotificationsProvider,
  LayoutProvider,
  RealtimeSubscriptionsProvider,
} from '@/app/providers'
import { SidebarProvider } from '@/shared/components/ui/sidebar'

// Refactored application layout using shadcn/ui sidebar.
// Navbar spans top; sidebar sits below it (desktop) and becomes a sheet on mobile.
// Main content scrolls independently.

export function AppLayout(): ReactNode {
  return (
    <LayoutProvider>
      <JobNotificationsProvider>
        <SidebarProvider>
          <AppShell />
        </SidebarProvider>

        <RealtimeSubscriptionsProvider />
      </JobNotificationsProvider>
    </LayoutProvider>
  )
}
