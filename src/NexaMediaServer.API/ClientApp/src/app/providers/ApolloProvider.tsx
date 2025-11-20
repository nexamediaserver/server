import {
  ApolloClient,
  ApolloLink,
  HttpLink,
  InMemoryCache,
} from '@apollo/client'
import { SetContextLink } from '@apollo/client/link/context'
import { ErrorLink } from '@apollo/client/link/error'
import { GraphQLWsLink } from '@apollo/client/link/subscriptions'
import { ApolloProvider as ApolloProviderBase } from '@apollo/client/react'
import { getMainDefinition } from '@apollo/client/utilities'
import { Kind, OperationTypeNode } from 'graphql'
import { createClient } from 'graphql-ws'
import {
  type PropsWithChildren,
  useCallback,
  useEffect,
  useMemo,
  useState,
} from 'react'
import { map } from 'rxjs/operators'

import { useAuth } from '@/features/auth'
import {
  refresh as apiRefresh,
  decodeJwt,
  parseUserFromToken,
} from '@/features/auth/api/client'

const ACCESS_TOKEN_STORAGE_KEY = 'auth:accessToken'
const REFRESH_TOKEN_STORAGE_KEY = 'auth:refreshToken'
const TOKEN_EXPIRY_SKEW_SECONDS = 30
type GraphQLResponse = ApolloLink.Result<Record<string, unknown>>

interface NodeConnection {
  [key: string]: unknown
  nodes?: unknown[]
}

interface OffsetCollection {
  [key: string]: unknown
  items?: unknown[]
  pageInfo?: { hasNextPage: boolean; hasPreviousPage: boolean }
  totalCount?: number
}

const mergeNodeConnections = (
  existing: NodeConnection | undefined,
  incoming: NodeConnection,
): NodeConnection | undefined => {
  if (!incoming.nodes || incoming.nodes.length === 0) {
    return existing
  }

  if (!existing?.nodes || existing.nodes.length === 0) {
    return incoming
  }

  return {
    ...incoming,
    nodes: [...existing.nodes, ...incoming.nodes],
  }
}

// For offset pagination, maintain a sparse array where items are placed at their correct indices
// This allows non-contiguous loading (A → Z → M) without losing previously loaded data
const mergeOffsetCollection = (
  existing: OffsetCollection | undefined,
  incoming: OffsetCollection,
  { args }: { args: null | Record<string, unknown> },
): OffsetCollection | undefined => {
  if (!incoming.items || incoming.items.length === 0) {
    return existing
  }

  const incomingSkip = (args?.skip as number | undefined) ?? 0
  const totalCount = incoming.totalCount ?? existing?.totalCount ?? 0

  // If no existing data, create array with null placeholders
  // We use null (not undefined or sparse) because Apollo filters out non-readable items
  // See: https://github.com/apollographql/apollo-client/issues/6628
  if (!existing?.items || existing.items.length === 0) {
    const items: unknown[] = new Array<null>(totalCount).fill(null)
    for (let i = 0; i < incoming.items.length; i++) {
      if (incomingSkip + i < totalCount) {
        items[incomingSkip + i] = incoming.items[i]
      }
    }
    return {
      ...incoming,
      items,
    }
  }

  // Merge into existing array
  // Ensure array is large enough (totalCount might have changed)
  const newLength = Math.max(existing.items.length, totalCount)
  const items: unknown[] = [...existing.items]

  // Extend array if needed with null placeholders
  while (items.length < newLength) {
    items.push(null)
  }

  // Insert incoming items at their correct positions
  for (let i = 0; i < incoming.items.length; i++) {
    const targetIndex = incomingSkip + i
    if (targetIndex < newLength) {
      items[targetIndex] = incoming.items[i]
    }
  }

  return {
    ...incoming,
    items,
  }
}

// Read function that preserves null placeholders in the array
// Apollo by default filters out unreadable items; we need to keep nulls for sparse arrays
const readOffsetCollection = (
  existing: OffsetCollection | undefined,
  // eslint-disable-next-line @typescript-eslint/no-explicit-any -- Apollo's FieldReadFunction uses any
  options: { canRead: (value: any) => boolean },
): OffsetCollection | undefined => {
  if (!existing?.items) return existing

  // Replace unreadable items with null to preserve array indices
  // This is critical for offset pagination to work correctly
  const items: unknown[] = []
  for (let i = 0; i < existing.items.length; i++) {
    const item = existing.items[i]
    items[i] = item !== null && options.canRead(item) ? item : null
  }

  return {
    ...existing,
    items,
  }
}

const isJwtToken = (token: null | string): token is string =>
  Boolean(token && token.includes('.') && token.split('.').length === 3)

const readAccessToken = () => localStorage.getItem(ACCESS_TOKEN_STORAGE_KEY)
const readRefreshToken = () => localStorage.getItem(REFRESH_TOKEN_STORAGE_KEY)

export function ApolloProvider({
  children,
}: Readonly<PropsWithChildren<React.ReactNode>>) {
  const { accessToken, setAccessToken, setStatus, setUser } = useAuth()
  const [wsEpoch, setWsEpoch] = useState(0)

  const restartWsConnection = useCallback(() => {
    setWsEpoch((value) => value + 1)
  }, [setWsEpoch])

  const markUnauthenticated = useCallback(() => {
    setStatus('unauthenticated')
    setAccessToken(null)
    setUser(null)
    restartWsConnection()
  }, [restartWsConnection, setAccessToken, setStatus, setUser])

  // Create WebSocket client for subscriptions
  const wsClient = useMemo(() => {
    const proto = globalThis.location.protocol === 'https:' ? 'wss:' : 'ws:'
    const url = `${proto}//${globalThis.location.host}/graphql`
    return createClient({
      connectionParams: () => {
        // Get the latest token from localStorage to ensure fresh auth on reconnect
        const token = readAccessToken()
        // Only send Authorization header for real JWTs; cookie auth will flow via credentials
        return isJwtToken(token) ? { Authorization: `Bearer ${token}` } : {}
      },
      // Disable lazy connection to ensure WebSocket is immediately ready
      lazy: false,
      retryAttempts: Infinity,
      shouldRetry: () => true,
      url,
    })
    // Only recreate on mount/unmount, not on token changes
  }, [wsEpoch])

  // Dispose WS client only on unmount
  useEffect(() => {
    return () => {
      try {
        void wsClient.dispose()
      } catch {
        // ignore
      }
    }
  }, [wsClient])

  const client = useMemo(() => {
    // Capture token within the initializer's closure
    let token = accessToken ?? readAccessToken()

    // HTTP link for queries and mutations
    const httpLink = new HttpLink({
      credentials: 'include', // Ensure cookies flow to GraphQL
      uri: '/graphql',
    })

    // WebSocket link for subscriptions
    const wsLink = new GraphQLWsLink(wsClient)

    // Auth link to add Authorization header
    const authLink = new SetContextLink((prevContext) => {
      if (!token || !isJwtToken(token)) {
        return prevContext
      }
      const existingHeaders =
        typeof prevContext.headers === 'object' && prevContext.headers !== null
          ? (prevContext.headers as Record<string, unknown>)
          : {}
      return {
        ...prevContext,
        headers: {
          ...existingHeaders,
          authorization: `Bearer ${token}`,
        },
      }
    })

    // Error link to handle authentication errors
    const errorLink = new ErrorLink(({ forward, operation }) => {
      // Get errors from response (handled by Apollo)
      return forward(operation).pipe(
        map((response: GraphQLResponse) => {
          const hasAuthError =
            response.errors?.some(
              (err) =>
                err.message.includes('UNAUTHENTICATED') ||
                err.message.includes('Unauthorized'),
            ) ?? false

          if (hasAuthError) {
            const hasJwt = Boolean(token && isJwtToken(token))
            const shouldRefresh = (() => {
              if (!hasJwt || !token) return true
              const { exp } = decodeJwt(token)
              if (!exp) return true
              const now = Math.floor(Date.now() / 1000)
              return exp - TOKEN_EXPIRY_SKEW_SECONDS <= now
            })()

            if (shouldRefresh) {
              if (hasJwt) {
                const rt = readRefreshToken()
                if (rt) {
                  apiRefresh(rt)
                    .then((res) => {
                      token = res.accessToken
                      setAccessToken(res.accessToken)
                      setUser(parseUserFromToken(res.accessToken))
                      restartWsConnection()
                    })
                    .catch(() => {
                      markUnauthenticated()
                    })
                } else {
                  markUnauthenticated()
                }
              } else {
                apiRefresh()
                  .then(() => {
                    restartWsConnection()
                  })
                  .catch(() => {
                    markUnauthenticated()
                  })
              }
            }
          }

          return response
        }),
      )
    })

    // Split link to route queries/mutations through HTTP and subscriptions through WebSocket
    const splitLink = ApolloLink.split(
      ({ query }) => {
        const definition = getMainDefinition(query)
        return (
          definition.kind === Kind.OPERATION_DEFINITION &&
          definition.operation === OperationTypeNode.SUBSCRIPTION
        )
      },
      wsLink,
      ApolloLink.from([errorLink, authLink, httpLink]),
    )

    return new ApolloClient({
      cache: new InMemoryCache({
        typePolicies: {
          LibrarySection: {
            fields: {
              children: {
                // Offset-based pagination with items array
                keyArgs: ['metadataType', 'order', 'where'],
                merge: mergeOffsetCollection,
                read: readOffsetCollection,
              },
            },
          },
          Query: {
            fields: {
              librarySections: {
                // Connection-style pagination with nodes array
                keyArgs: false,
                merge: mergeNodeConnections,
              },
            },
          },
          // Disable caching for SearchResult to prevent ID conflicts with Item types
          SearchResult: {
            keyFields: false,
          },
        },
      }),
      link: splitLink,
    })
  }, [
    accessToken,
    markUnauthenticated,
    restartWsConnection,
    setAccessToken,
    setUser,
    wsClient,
  ])

  return <ApolloProviderBase client={client}>{children}</ApolloProviderBase>
}
