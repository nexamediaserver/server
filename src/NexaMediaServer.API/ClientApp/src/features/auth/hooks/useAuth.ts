import { useAtom } from 'jotai'
import { useCallback, useEffect } from 'react'

import {
  login as apiLogin,
  logout as apiLogout,
  me as apiMe,
  refresh as apiRefresh,
  parseUserFromToken,
} from '@/features/auth/api/client'
import { handleErrorStandalone } from '@/shared/hooks/useErrorHandler'
import { clearStoredDeviceIdentifier } from '@/shared/lib/deviceIdentity'

import {
  authAccessTokenAtom,
  authAccessTokenExpiresAtAtom,
  authRefreshTokenAtom,
  authStatusAtom,
  authUserAtom,
} from '../store'

export function useAuth() {
  const [status, setStatus] = useAtom(authStatusAtom)
  const [user, setUser] = useAtom(authUserAtom)
  const [accessToken, setAccessToken] = useAtom(authAccessTokenAtom)
  const [, setRefreshToken] = useAtom(authRefreshTokenAtom)
  const [, setExpiresAt] = useAtom(authAccessTokenExpiresAtAtom)
  const hasToken = Boolean(accessToken)
  const isAuthenticated = status === 'authenticated'

  // On initial load detect session using token or cookie-based auth
  useEffect(() => {
    if (status !== 'idle') return
    // If we have a token from storage, consider authenticated (and keep deriving user lazily elsewhere)
    if (hasToken) {
      setStatus('authenticated')
      return
    }
    // Probe server for current user via cookie session
    setStatus('loading')
    void apiMe()
      .then((info) => {
        const email = info.email
        const username = email
        const id = email
        setUser({ email, id, username })
        setStatus('authenticated')
      })
      .catch(() => {
        clearStoredDeviceIdentifier()
        setUser(null)
        setStatus('unauthenticated')
      })
  }, [hasToken, setStatus, setUser, status])

  const login = useCallback(
    async (
      username: string,
      password: string,
      options?: { clientVersion?: null | string },
    ) => {
      setStatus('loading')
      const res = await apiLogin(
        username,
        password,
        true,
        options?.clientVersion ?? null,
      )
      // If tokens are returned, use JWT flow
      const access = res.accessToken?.trim() ?? ''
      if (access.length > 0) {
        setAccessToken(access)
        setRefreshToken(res.refreshToken)
        setExpiresAt(Date.now() + res.expiresIn * 1000)
        const user = parseUserFromToken(access)
        setUser(user)
        setStatus('authenticated')
        return
      }
      // Cookie-based only: fetch user info to confirm auth
      try {
        const info = await apiMe()
        const email = info.email
        const username = email
        const id = email
        setUser({ email, id, username })
        setStatus('authenticated')
      } catch (error: unknown) {
        handleErrorStandalone(error, { context: 'useAuth.login' })
        setUser(null)
        setStatus('unauthenticated')
      }
    },
    [setStatus, setAccessToken, setExpiresAt, setRefreshToken, setUser],
  )

  const logout = useCallback(async () => {
    try {
      await apiLogout()
    } finally {
      clearStoredDeviceIdentifier()
      setStatus('unauthenticated')
      setAccessToken(null)
      setRefreshToken(null)
      setExpiresAt(null)
      setUser(null)
    }
  }, [setStatus, setAccessToken, setExpiresAt, setRefreshToken, setUser])

  const refresh = useCallback(async () => {
    try {
      const currentRefresh = localStorage.getItem('auth:refreshToken')
      const res = await apiRefresh(currentRefresh)
      const nextAccess = res.accessToken

      if (typeof nextAccess === 'string' && nextAccess.length > 0) {
        setAccessToken(nextAccess)
        setRefreshToken(res.refreshToken)
        setExpiresAt(Date.now() + res.expiresIn * 1000)
        // Refresh doesn't return user; re-derive
        const user = parseUserFromToken(nextAccess)
        setUser(user)
      } else {
        // Cookie-only session: probe /manage/info to sync user context
        const info = await apiMe()
        const email = info.email
        const username = email
        const id = email
        setUser({ email, id, username })
        setAccessToken(null)
        setRefreshToken(null)
        setExpiresAt(null)
        setStatus('authenticated')
      }
    } catch {
      clearStoredDeviceIdentifier()
      setStatus('unauthenticated')
      setAccessToken(null)
      setRefreshToken(null)
      setExpiresAt(null)
      setUser(null)
    }
  }, [setStatus, setAccessToken, setExpiresAt, setRefreshToken, setUser])

  return {
    accessToken,
    isAuthenticated,
    login,
    logout,
    refresh,
    setAccessToken,
    setStatus,
    setUser,
    status,
    user,
  }
}
