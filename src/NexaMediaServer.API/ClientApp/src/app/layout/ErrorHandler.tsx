import type { PropsWithChildren, ReactNode } from 'react'

import { ErrorBoundary, type FallbackProps } from 'react-error-boundary'

import { handleErrorStandalone } from '@/shared/hooks/useErrorHandler'
import { getUserMessage } from '@/shared/lib/errors'

import { Button } from '../../shared/components/ui/button'

/**
 * Error boundary that catches errors in the component tree.
 * Displays a fallback UI and shows a toast notification.
 */
export function ErrorHandler({
  children,
}: Readonly<PropsWithChildren>): ReactNode {
  return (
    <ErrorBoundary
      FallbackComponent={ErrorFallback}
      onError={(error) => {
        handleErrorStandalone(error, {
          context: 'ErrorBoundary',
        })
      }}
      onReset={() => {
        // Reset application state here if needed
      }}
    >
      {children}
    </ErrorBoundary>
  )
}

/**
 * Fallback component displayed when an error occurs.
 * Provides user-friendly message and recovery options.
 */
function ErrorFallback({
  error,
  resetErrorBoundary,
}: Readonly<FallbackProps>): ReactNode {
  const userMessage = getUserMessage(error)

  return (
    <div
      className={`
        flex min-h-[50vh] flex-col items-center justify-center gap-4 p-8
      `}
    >
      <div className="flex flex-col items-center gap-2 text-center">
        <h2 className="text-xl font-semibold text-destructive">
          Something went wrong
        </h2>
        <p className="max-w-md text-muted-foreground">{userMessage}</p>
      </div>
      <div className="flex gap-2">
        <Button onClick={resetErrorBoundary} variant="outline">
          Try again
        </Button>
        <Button
          onClick={() => {
            globalThis.location.href = '/'
          }}
          variant="default"
        >
          Go home
        </Button>
      </div>
    </div>
  )
}
