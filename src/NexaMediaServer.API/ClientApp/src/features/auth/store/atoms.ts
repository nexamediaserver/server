import { atom } from 'jotai'
import { atomWithStorage } from 'jotai/utils'

import type { AuthStatus, PublicUser } from '@/features/auth/types'

/** Current authentication status */
export const authStatusAtom = atom<AuthStatus>('idle')

/** Authenticated user info (persisted to localStorage) */
export const authUserAtom = atomWithStorage<null | PublicUser>(
  'auth:user',
  null,
)

/** Access token (persisted to localStorage) */
export const authAccessTokenAtom = atomWithStorage<null | string>(
  'auth:accessToken',
  null,
)

/** Refresh token (persisted to localStorage) */
export const authRefreshTokenAtom = atomWithStorage<null | string>(
  'auth:refreshToken',
  null,
)

/** Access token expiration timestamp (persisted to localStorage) */
export const authAccessTokenExpiresAtAtom = atomWithStorage<null | number>(
  'auth:accessTokenExpiresAt',
  null,
)
