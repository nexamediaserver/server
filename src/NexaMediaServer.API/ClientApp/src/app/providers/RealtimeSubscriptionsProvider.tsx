import { useSubscription } from '@apollo/client/react'
import { useEffect } from 'react'

import { onMetadataItemUpdated } from '@/app/graphql/subscriptions'
import { useAuth } from '@/features/auth'

/**
 * RealtimeSubscriptionsProvider establishes GraphQL subscriptions for normalized cache entities.
 *
 * This provider uses the cache-driven pattern where subscription data is automatically
 * merged into Apollo Client's normalized cache. Any components
 * querying these entities will automatically re-render when updates arrive.
 *
 * **When to use this pattern:**
 * - Subscription returns entities that have an `id` field
 * - Data should be cached globally and shared across multiple components
 * - Updates should automatically reflect in all queries for that entity
 * - No component-specific state management is needed
 *
 * **Contrast with JobNotificationsProvider:**
 * JobNotificationsProvider uses a context-driven pattern for ephemeral events
 * (job notifications) that don't belong in the normalized cache.
 *
 * **Example:**
 * - onMetadataItemUpdated returns `Item` nodes → cache-driven (this provider)
 * - onJobNotification returns transient events → context-driven (JobNotificationsProvider)
 */
export function RealtimeSubscriptionsProvider() {
  const { isAuthenticated } = useAuth()

  const { data: metaResult, error: metaError } = useSubscription(
    onMetadataItemUpdated,
    { skip: !isAuthenticated },
  )

  // Optional: surface subscription errors and data in development
  useEffect(() => {
    if (metaError && import.meta.env.DEV) {
      console.debug('onMetadataItemUpdated error', metaError)
    }
    if (metaResult && import.meta.env.DEV) {
      console.debug('onMetadataItemUpdated received', metaResult)
    }
  }, [metaError, metaResult])

  return null
}
