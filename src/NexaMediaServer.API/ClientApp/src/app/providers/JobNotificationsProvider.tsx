import { useSubscription } from '@apollo/client/react'
import { useEffect, useState } from 'react'

import type { OnJobNotificationSubscription } from '@/shared/api/graphql/graphql'

import { onJobNotification } from '@/app/graphql/subscriptions'
import { useAuth } from '@/features/auth'

import { JobNotificationsContext } from './useJobNotifications'

type JobNotification = OnJobNotificationSubscription['onJobNotification']

/**
 * JobNotificationsProvider manages the global state of active background jobs.
 *
 * Unlike RealtimeSubscriptionsProvider which updates normalized cache entities,
 * this provider manages ephemeral job notification state that drives UI elements
 * like the notification bell.
 *
 * Job notifications are transient events (not cached entities) and have a specific
 * lifecycle (active -> completed), making them better suited for context state
 * rather than the normalized GraphQL cache.
 */
export function JobNotificationsProvider({
  children,
}: {
  children: React.ReactNode
}) {
  const { isAuthenticated } = useAuth()
  const [activeJobs, setActiveJobs] = useState<Map<string, JobNotification>>(
    new Map(),
  )

  const { data: subscriptionData } = useSubscription(onJobNotification, {
    skip: !isAuthenticated,
  })

  useEffect(() => {
    if (subscriptionData?.onJobNotification) {
      const notification = subscriptionData.onJobNotification
      if (import.meta.env.DEV) {
        console.debug('onJobNotification received', notification)
      }
      setActiveJobs((prev) => {
        const next = new Map(prev)
        if (notification.isActive) {
          next.set(notification.id, notification)
        } else {
          // Remove completed jobs after a short delay to show 100% completion
          setTimeout(() => {
            setActiveJobs((current) => {
              const updated = new Map(current)
              updated.delete(notification.id)
              return updated
            })
          }, 2000)
        }
        return next
      })
    }
  }, [subscriptionData])

  const hasActiveJobs = activeJobs.size > 0

  return (
    <JobNotificationsContext.Provider value={{ activeJobs, hasActiveJobs }}>
      {children}
    </JobNotificationsContext.Provider>
  )
}
