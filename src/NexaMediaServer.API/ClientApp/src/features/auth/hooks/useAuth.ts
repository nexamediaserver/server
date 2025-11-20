import { useAtom } from 'jotai'
import { useCallback, useEffect } from 'react'

import {
  login as apiLogin,
  logout as apiLogout,
  me as apiMe,
  refresh as apiRefresh,
  parseUserFromToken,
} from '@/features/auth/api/client'
import {
  authAccessTokenAtom,
  authAccessTokenExpiresAtAtom,
  authRefreshTokenAtom,
  authStatusAtom,
  authUserAtom,
} from '@/store'

export function useAuth() {
  const [status, setStatus] = useAtom(authStatusAtom)
  const [user, setUser] = useAtom(authUserAtom)
  const [accessToken, setAccessToken] = useAtom(authAccessTokenAtom)
  const [, setRefreshToken] = useAtom(authRefreshTokenAtom)
  const [, setExpiresAt] = useAtom(authAccessTokenExpiresAtAtom)
  const hasToken = Boolean(accessToken)
  const isAuthenticated = status === 'authenticated'

  // On initial load or when idle, detect session using token or cookie-based auth
  useEffect(() => {
    if (status !== 'idle' && status !== 'unauthenticated') return
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
        setUser(null)
        setStatus('unauthenticated')
      })
  }, [hasToken, setStatus, setUser, status])

  const login = useCallback(
    async (username: string, password: string) => {
      setStatus('loading')
      const res = await apiLogin(username, password)
      // If tokens are returned, use JWT flow
      const access = (res as unknown as { accessToken?: string }).accessToken
      if (typeof access === 'string' && access.length > 0) {
        setAccessToken(res.accessToken)
        setRefreshToken(res.refreshToken)
        setExpiresAt(Date.now() + res.expiresIn * 1000)
        const user = parseUserFromToken(res.accessToken)
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
      } catch {
        setUser(null)
        setStatus('unauthenticated')
      }
    },
    [setStatus, setAccessToken, setUser],
  )

  const logout = useCallback(async () => {
    try {
      await apiLogout()
    } finally {
      setStatus('unauthenticated')
      setAccessToken(null)
      setRefreshToken(null)
      setExpiresAt(null)
      setUser(null)
    }
  }, [setStatus, setAccessToken, setUser])

  const refresh = useCallback(async () => {
    try {
      const currentRefresh = localStorage.getItem('auth:refreshToken')
      if (!currentRefresh) throw new Error('Missing refresh token')
      const res = await apiRefresh(currentRefresh)
      setAccessToken(res.accessToken)
      setRefreshToken(res.refreshToken)
      setExpiresAt(Date.now() + res.expiresIn * 1000)
      // Refresh doesn't return user; re-derive
      const user = parseUserFromToken(res.accessToken)
      setUser(user)
    } catch {
      setStatus('unauthenticated')
      setAccessToken(null)
      setRefreshToken(null)
      setExpiresAt(null)
      setUser(null)
    }
  }, [setStatus, setAccessToken, setUser])

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
