import type { ReactNode } from 'react'

import { getUserMessage } from '@/shared/lib/errors'

import { Button } from './ui/button'

/**
 * Props for QueryErrorDisplay component.
 */
export interface QueryErrorDisplayProps {
  /** The error to display */
  error: Error | undefined
  /** Optional message override */
  message?: string
  /** Optional retry callback */
  onRetry?: () => void
  /** Title to display (default: "Something went wrong") */
  title?: string
}

/**
 * A consistent error display component for GraphQL query errors.
 * Use this in pages/components to show errors from useQuery hooks.
 *
 * @example
 * ```tsx
 * const { data, error, loading, refetch } = useQuery(MyQuery)
 *
 * if (error) {
 *   return <QueryErrorDisplay error={error} onRetry={() => refetch()} />
 * }
 * ```
 */
export function QueryErrorDisplay({
  error,
  message,
  onRetry,
  title = 'Something went wrong',
}: Readonly<QueryErrorDisplayProps>): ReactNode {
  const displayMessage = message ?? (error ? getUserMessage(error) : undefined)

  return (
    <div
      className={`
        flex min-h-[50vh] flex-col items-center justify-center gap-4 p-8
      `}
    >
      <div className="flex flex-col items-center gap-2 text-center">
        <h2 className="text-xl font-semibold text-destructive">{title}</h2>
        {displayMessage ? (
          <p className="max-w-md text-muted-foreground">{displayMessage}</p>
        ) : null}
      </div>
      {onRetry ? (
        <Button onClick={onRetry} variant="outline">
          Try again
        </Button>
      ) : null}
    </div>
  )
}
