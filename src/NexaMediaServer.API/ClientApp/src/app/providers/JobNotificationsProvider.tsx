import { useSubscription } from '@apollo/client/react'
import { useCallback, useEffect, useRef, useState } from 'react'

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
  const removalTimeoutsRef = useRef<Map<string, ReturnType<typeof setTimeout>>>(
    new Map(),
  )

  const handleJobNotification = useCallback((notification: JobNotification) => {
    if (notification.isActive) {
      // Clear any pending removal timeout for this job
      const existingTimeout = removalTimeoutsRef.current.get(notification.id)
      if (existingTimeout !== undefined) {
        clearTimeout(existingTimeout)
        removalTimeoutsRef.current.delete(notification.id)
      }

      setActiveJobs((prev) => {
        const next = new Map(prev)
        next.set(notification.id, notification)
        return next
      })
    } else {
      // Update job to completed state immediately
      setActiveJobs((prev) => {
        const next = new Map(prev)
        next.set(notification.id, notification)
        return next
      })

      // Schedule removal after a short delay to show 100% completion
      const timeout = setTimeout(() => {
        setActiveJobs((current) => {
          const updated = new Map(current)
          updated.delete(notification.id)
          return updated
        })
        removalTimeoutsRef.current.delete(notification.id)
      }, 2000)

      removalTimeoutsRef.current.set(notification.id, timeout)
    }
  }, [])

  useSubscription(onJobNotification, {
    onData: ({ data }) => {
      if (data.data?.onJobNotification) {
        handleJobNotification(data.data.onJobNotification)
      }
    },
    skip: !isAuthenticated,
  })

  // Cleanup timeouts on unmount
  useEffect(() => {
    const timeouts = removalTimeoutsRef.current
    return () => {
      timeouts.forEach((timeout) => {
        clearTimeout(timeout)
      })
      timeouts.clear()
    }
  }, [])

  const hasActiveJobs = activeJobs.size > 0

  return (
    <JobNotificationsContext.Provider value={{ activeJobs, hasActiveJobs }}>
      {children}
    </JobNotificationsContext.Provider>
  )
}
