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
import { useCallback, useEffect, useMemo, useState } from 'react'
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

const isJwtToken = (token: null | string): token is string =>
  Boolean(token && token.includes('.') && token.split('.').length === 3)

const readAccessToken = () => localStorage.getItem(ACCESS_TOKEN_STORAGE_KEY)
const readRefreshToken = () => localStorage.getItem(REFRESH_TOKEN_STORAGE_KEY)

export function ApolloProvider({ children }: { children: React.ReactNode }) {
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
    const proto = window.location.protocol === 'https:' ? 'wss:' : 'ws:'
    const url = `${proto}//${window.location.host}/graphql`
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
            // Check if token is expired or near expiry
            const shouldRefresh = (() => {
              if (!token || !isJwtToken(token)) return true
              const { exp } = decodeJwt(token)
              if (!exp) return true
              const now = Math.floor(Date.now() / 1000)
              return exp - TOKEN_EXPIRY_SKEW_SECONDS <= now
            })()

            if (shouldRefresh) {
              // Attempt to refresh the token asynchronously
              const rt = readRefreshToken()
              if (!rt) {
                markUnauthenticated()
              } else {
                apiRefresh(rt)
                  .then((res) => {
                    token = res.accessToken
                    setAccessToken(res.accessToken)
                    setUser(parseUserFromToken(res.accessToken))
                    // Force a new websocket connection so subscriptions pick up the fresh token
                    restartWsConnection()
                  })
                  .catch(() => {
                    markUnauthenticated()
                  })
              }
            }

            // Log the error
            response.errors?.forEach((err) => {
              console.error(`[Apollo Client Error]: ${err.message}`)
            })
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
                // Connection-style pagination with nodes array
                keyArgs: false,
                merge: mergeNodeConnections,
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
