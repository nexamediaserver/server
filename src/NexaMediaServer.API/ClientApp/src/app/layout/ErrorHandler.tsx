import type { PropsWithChildren } from 'react'

import { ErrorBoundary } from 'react-error-boundary'
import { toast } from 'sonner'

/**
 * A simple error boundary that catches errors in the component tree and displays a toast notification.
 */
export function ErrorHandler({ children }: PropsWithChildren) {
  return (
    <ErrorBoundary
      FallbackComponent={() => (
        <div>An error occurred. Please try again later.</div>
      )}
      onError={(error) => {
        console.error('Uncaught error in component tree:', error)
        toast.error(error.message)
      }}
    >
      {children}
    </ErrorBoundary>
  )
}
