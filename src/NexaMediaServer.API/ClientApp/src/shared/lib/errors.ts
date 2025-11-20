import type { GraphQLError } from 'graphql'

// ============================================================================
// Exports
// ============================================================================

/**
 * Shape of an Apollo-like error.
 * @internal
 */
export interface ApolloLikeError {
  graphQLErrors?: readonly GraphQLError[]
  networkError?: null | {
    statusCode?: number
  }
}

/**
 * Error categories for consistent handling across the application.
 */
export type ErrorCategory =
  | 'authentication'
  | 'authorization'
  | 'network'
  | 'not_found'
  | 'playback'
  | 'server'
  | 'unknown'
  | 'validation'

/**
 * Options for error handling behavior.
 */
export interface ErrorHandlingOptions {
  /** Log error to console (default: true in development) */
  log?: boolean
  /** Show toast notification to user (default: true) */
  notify?: boolean
  /** Rethrow the error after handling (default: false) */
  rethrow?: boolean
  /** Custom toast duration in milliseconds */
  toastDuration?: number
}

/**
 * Severity levels for error handling decisions.
 */
export type ErrorSeverity = 'error' | 'info' | 'warning'

/**
 * Structured application error with metadata.
 */
export class AppError extends Error {
  readonly category: ErrorCategory
  readonly code?: string
  readonly originalError?: unknown
  readonly severity: ErrorSeverity
  readonly userMessage: string

  constructor(
    message: string,
    options: {
      category?: ErrorCategory
      code?: string
      originalError?: unknown
      severity?: ErrorSeverity
      userMessage?: string
    } = {},
  ) {
    super(message)
    this.name = 'AppError'
    this.category = options.category ?? 'unknown'
    this.severity = options.severity ?? 'error'
    this.userMessage = options.userMessage ?? message
    this.code = options.code
    this.originalError = options.originalError
  }
}

/**
 * Determines the error category from an error object.
 */
export function categorizeError(error: unknown): ErrorCategory {
  if (error instanceof AppError) {
    return error.category
  }

  // Check for Apollo/GraphQL errors
  if (isApolloLikeError(error)) {
    return categorizeApolloError(error)
  }

  // Check for standard fetch/network errors
  if (error instanceof TypeError && error.message.includes('fetch')) {
    return 'network'
  }

  // Check for DOMException (often browser API errors)
  if (error instanceof DOMException) {
    if (error.name === 'NotAllowedError') return 'authorization'
    if (error.name === 'AbortError') return 'network'
  }

  return 'unknown'
}

/**
 * Creates a playback-specific error.
 */
export function createPlaybackError(
  message: string,
  originalError?: unknown,
): AppError {
  return new AppError(message, {
    category: 'playback',
    originalError,
    userMessage: 'There was a problem playing the media. Please try again.',
  })
}

/**
 * Creates a validation error.
 */
export function createValidationError(message: string): AppError {
  return new AppError(message, {
    category: 'validation',
    severity: 'warning',
    userMessage: message,
  })
}

/**
 * Determines the severity based on error category.
 */
export function getSeverity(error: unknown): ErrorSeverity {
  if (error instanceof AppError) {
    return error.severity
  }

  const category = categorizeError(error)

  switch (category) {
    case 'authentication':
    case 'authorization':
    case 'network':
    case 'validation':
      return 'warning'
    default:
      return 'error'
  }
}

/**
 * Extracts a user-friendly message from any error.
 */
export function getUserMessage(error: unknown): string {
  // AppError already has a user message
  if (error instanceof AppError) {
    return error.userMessage
  }

  // Apollo errors with GraphQL errors
  if (isApolloLikeError(error)) {
    return getApolloErrorMessage(error)
  }

  // Standard Error with safe message
  if (error instanceof Error) {
    // Don't expose potentially sensitive error messages
    const category = categorizeError(error)
    // Only return the original message for known safe categories
    if (category === 'validation' || category === 'not_found') {
      return error.message
    }
    return CATEGORY_MESSAGES[category]
  }

  return CATEGORY_MESSAGES.unknown
}

/**
 * Checks if an error is an authentication error that should trigger a redirect.
 */
export function isAuthenticationError(error: unknown): boolean {
  const category = categorizeError(error)
  return category === 'authentication'
}

/**
 * Checks if an error is a network error that might be transient.
 */
export function isNetworkError(error: unknown): boolean {
  const category = categorizeError(error)
  return category === 'network'
}

/**
 * Normalizes any thrown value into an AppError.
 */
export function normalizeError(error: unknown): AppError {
  if (error instanceof AppError) {
    return error
  }

  const category = categorizeError(error)
  const userMessage = getUserMessage(error)
  const message = error instanceof Error ? error.message : String(error)

  return new AppError(message, {
    category,
    originalError: error,
    userMessage,
  })
}

// ============================================================================
// Internal constants and helpers (non-exported)
// ============================================================================

/**
 * User-friendly messages for common error categories.
 */
const CATEGORY_MESSAGES: Record<ErrorCategory, string> = {
  authentication: 'Please sign in to continue.',
  authorization: "You don't have permission to perform this action.",
  network: 'Unable to connect. Please check your internet connection.',
  not_found: 'The requested resource was not found.',
  playback: 'There was a problem with media playback.',
  server: 'Something went wrong on our end. Please try again later.',
  unknown: 'An unexpected error occurred.',
  validation: 'Please check your input and try again.',
}

/**
 * Categorizes Apollo-like errors.
 */
function categorizeApolloError(error: ApolloLikeError): ErrorCategory {
  // Network errors take precedence
  if (error.networkError) {
    return categorizeNetworkError(error.networkError)
  }

  // GraphQL errors
  const gqlErrors = error.graphQLErrors
  if (gqlErrors && gqlErrors.length > 0) {
    return categorizeGraphQLError(gqlErrors[0])
  }

  return 'server'
}

/**
 * Categorizes a GraphQL error based on its code or message.
 */
function categorizeGraphQLError(error: GraphQLError): ErrorCategory {
  const code = error.extensions.code as string | undefined
  const message = error.message.toLowerCase()

  // Check error codes first
  if (code === 'UNAUTHENTICATED' || code === 'AUTH_NOT_AUTHENTICATED') {
    return 'authentication'
  }
  if (code === 'FORBIDDEN' || code === 'AUTH_NOT_AUTHORIZED') {
    return 'authorization'
  }
  if (code === 'BAD_USER_INPUT' || code === 'GRAPHQL_VALIDATION_FAILED') {
    return 'validation'
  }
  if (code === 'INTERNAL_SERVER_ERROR') {
    return 'server'
  }

  // Check message content as fallback
  if (message.includes('unauthenticated') || message.includes('unauthorized')) {
    return 'authentication'
  }
  if (message.includes('forbidden') || message.includes('permission')) {
    return 'authorization'
  }
  if (message.includes('not found')) {
    return 'not_found'
  }
  if (message.includes('validation') || message.includes('invalid')) {
    return 'validation'
  }

  return 'server'
}

/**
 * Categorizes network errors from Apollo-like errors.
 */
function categorizeNetworkError(
  networkError: ApolloLikeError['networkError'],
): ErrorCategory {
  if (!networkError) {
    return 'network'
  }

  const status = networkError.statusCode
  if (status === 401) return 'authentication'
  if (status === 403) return 'authorization'
  if (status === 404) return 'not_found'
  if (status && status >= 500) return 'server'

  return 'network'
}

/**
 * Extracts a user-friendly message from Apollo-like errors.
 */
function getApolloErrorMessage(error: ApolloLikeError): string {
  const gqlErrors = error.graphQLErrors
  if (gqlErrors && gqlErrors.length > 0) {
    const gqlError = gqlErrors[0]
    // Don't expose technical GraphQL messages
    if (gqlError.message.includes('UNAUTHENTICATED')) {
      return CATEGORY_MESSAGES.authentication
    }
    // For validation errors, the message is usually useful
    const category = categorizeGraphQLError(gqlError)
    if (category === 'validation') {
      return gqlError.message
    }
    return CATEGORY_MESSAGES[category]
  }

  // Network errors
  if (error.networkError) {
    return CATEGORY_MESSAGES.network
  }

  return CATEGORY_MESSAGES.server
}

/**
 * Type guard for Apollo-like errors.
 */
function isApolloLikeError(error: unknown): error is ApolloLikeError {
  return (
    error !== null &&
    typeof error === 'object' &&
    'graphQLErrors' in error &&
    'networkError' in error
  )
}
