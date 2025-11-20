import { createContext, useContext } from 'react'

import type { OnJobNotificationSubscription } from '@/shared/api/graphql/graphql'

type JobNotification = OnJobNotificationSubscription['onJobNotification']

interface JobNotificationsContextValue {
  activeJobs: Map<string, JobNotification>
  hasActiveJobs: boolean
}

export const JobNotificationsContext = createContext<
  JobNotificationsContextValue | undefined
>(undefined)

/**
 * Hook to access active job notifications.
 * @throws Error if used outside JobNotificationsProvider
 */
export function useJobNotifications() {
  const context = useContext(JobNotificationsContext)
  if (!context) {
    throw new Error(
      'useJobNotifications must be used within JobNotificationsProvider',
    )
  }
  return context
}
