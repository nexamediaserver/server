import { useCallback, useMemo } from 'react'
import { toast } from 'sonner'

import {
  type ErrorHandlingOptions,
  type ErrorSeverity,
  getSeverity,
  getUserMessage,
  isAuthenticationError,
  normalizeError,
} from '@/shared/lib/errors'

/**
 * Type representing known error categories for programmatic handling.
 */
export type { ErrorCategory, ErrorSeverity } from '@/shared/lib/errors'

const isDev = import.meta.env.DEV

/**
 * Default options for error handling.
 */
const DEFAULT_OPTIONS: Required<ErrorHandlingOptions> = {
  log: isDev,
  notify: true,
  rethrow: false,
  toastDuration: 5000,
}

/**
 * Options for the useErrorHandler hook.
 */
export interface UseErrorHandlerOptions {
  /** Context identifier for logging (e.g., component/hook name) */
  context?: string
  /** Default options applied to all handled errors */
  defaultOptions?: ErrorHandlingOptions
  /** Callback when an authentication error occurs */
  onAuthError?: () => void
}

/**
 * Result of the useErrorHandler hook.
 */
export interface UseErrorHandlerResult {
  /** Create an error handler callback */
  createErrorHandler: (
    options?: ErrorHandlingOptions,
  ) => (error: unknown) => void
  /** Handle an error with optional overrides */
  handleError: (error: unknown, options?: ErrorHandlingOptions) => void
  /** Report an error without throwing */
  reportError: (error: unknown, options?: ErrorHandlingOptions) => void
  /** Show an error toast manually */
  showErrorToast: (
    message: string,
    options?: { duration?: number; severity?: ErrorSeverity },
  ) => void
  /** Show an info toast */
  showInfoToast: (message: string, options?: { duration?: number }) => void
  /** Show a success toast */
  showSuccessToast: (message: string, options?: { duration?: number }) => void
  /** Show a warning toast */
  showWarningToast: (message: string, options?: { duration?: number }) => void
}

/**
 * Standalone error handler for use outside of React components.
 * Uses the same error normalization and categorization logic.
 *
 * @example
 * ```ts
 * import { handleErrorStandalone } from '@/shared/hooks/useErrorHandler'
 *
 * try {
 *   await someAsyncOperation()
 * } catch (error) {
 *   handleErrorStandalone(error, { context: 'SomeModule' })
 * }
 * ```
 */
export function handleErrorStandalone(
  error: unknown,
  options: {
    context?: string
    log?: boolean
    notify?: boolean
  } = {},
): void {
  const { context, log = isDev, notify = true } = options
  const normalized = normalizeError(error)
  const severity = getSeverity(error)
  const userMessage = getUserMessage(error)

  if (log) {
    const logPrefix = context ? `[${context}]` : '[Error]'
    if (severity === 'error') {
      console.error(logPrefix, normalized.message, normalized.originalError)
    } else {
      console.warn(logPrefix, normalized.message, normalized.originalError)
    }
  }

  if (notify) {
    showToast(userMessage, severity)
  }
}

/**
 * Shows a toast notification based on severity level.
 *
 * @param message - The message to display
 * @param severity - Error severity ('error' | 'warning' | 'info')
 * @param duration - Optional duration in milliseconds
 */
export function showToast(
  message: string,
  severity: ErrorSeverity,
  duration?: number,
): void {
  const toastOptions = { duration: duration ?? 5000 }

  switch (severity) {
    case 'error':
      toast.error(message, toastOptions)
      break
    case 'info':
      toast.info(message, toastOptions)
      break
    case 'warning':
      toast.warning(message, toastOptions)
      break
  }
}

/**
 * Hook providing unified error handling across the application.
 *
 * Features:
 * - Consistent error normalization and categorization
 * - Toast notifications with appropriate severity
 * - Optional console logging (enabled by default in dev)
 * - Authentication error callbacks
 * - Configurable per-call options
 *
 * @example
 * ```tsx
 * function MyComponent() {
 *   const { handleError, showSuccessToast } = useErrorHandler({
 *     context: 'MyComponent',
 *   });
 *
 *   const handleSave = async () => {
 *     try {
 *       await saveData();
 *       showSuccessToast('Saved successfully!');
 *     } catch (error) {
 *       handleError(error);
 *     }
 *   };
 * }
 * ```
 */
export function useErrorHandler(
  options: UseErrorHandlerOptions = {},
): UseErrorHandlerResult {
  const { context, defaultOptions, onAuthError } = options

  const mergedDefaults = useMemo(
    () => ({ ...DEFAULT_OPTIONS, ...defaultOptions }),
    [defaultOptions],
  )

  const handleError = useCallback(
    (error: unknown, callOptions?: ErrorHandlingOptions) => {
      const opts = { ...mergedDefaults, ...callOptions }
      const normalized = normalizeError(error)
      const severity = getSeverity(error)
      const userMessage = getUserMessage(error)

      // Log to console
      if (opts.log) {
        const logPrefix = context ? `[${context}]` : '[Error]'
        if (severity === 'error') {
          console.error(logPrefix, normalized.message, normalized.originalError)
        } else {
          console.warn(logPrefix, normalized.message, normalized.originalError)
        }
      }

      // Show toast notification
      if (opts.notify) {
        showToast(userMessage, severity, opts.toastDuration)
      }

      // Handle authentication errors
      if (isAuthenticationError(error)) {
        onAuthError?.()
      }

      // Optionally rethrow
      if (opts.rethrow) {
        throw normalized
      }
    },
    [context, mergedDefaults, onAuthError],
  )

  const reportError = useCallback(
    (error: unknown, callOptions?: ErrorHandlingOptions) => {
      handleError(error, { ...callOptions, rethrow: false })
    },
    [handleError],
  )

  const createErrorHandler = useCallback(
    (callOptions?: ErrorHandlingOptions) => (error: unknown) => {
      handleError(error, callOptions)
    },
    [handleError],
  )

  const showErrorToast = useCallback(
    (
      message: string,
      toastOptions?: { duration?: number; severity?: ErrorSeverity },
    ) => {
      showToast(
        message,
        toastOptions?.severity ?? 'error',
        toastOptions?.duration,
      )
    },
    [],
  )

  const showInfoToast = useCallback(
    (message: string, toastOptions?: { duration?: number }) => {
      toast.info(message, { duration: toastOptions?.duration ?? 4000 })
    },
    [],
  )

  const showSuccessToast = useCallback(
    (message: string, toastOptions?: { duration?: number }) => {
      toast.success(message, { duration: toastOptions?.duration ?? 3000 })
    },
    [],
  )

  const showWarningToast = useCallback(
    (message: string, toastOptions?: { duration?: number }) => {
      toast.warning(message, { duration: toastOptions?.duration ?? 4000 })
    },
    [],
  )

  return {
    createErrorHandler,
    handleError,
    reportError,
    showErrorToast,
    showInfoToast,
    showSuccessToast,
    showWarningToast,
  }
}
